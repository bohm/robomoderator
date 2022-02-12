using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace RoboModerator
{

    class OrganizerCoreData
    {
        public ulong MessageWithButtons;
        public ulong MessageWithSignups;
        public List<List<ulong>> SignUpLists;

        public void FillGaps()
        {
            if (SignUpLists == null)
            {
                SignUpLists = new List<List<ulong>>();
            }

            while (SignUpLists.Count < 7)
            {
                 SignUpLists.Add(new List<ulong>());
            }
        }

        public void Clear()
        {
            foreach(var signup in SignUpLists)
            {
                signup.Clear();
            }

            MessageWithButtons = 0;
            MessageWithSignups = 0;
        }
    }

    class OrganizerSecondaryData
    {
        public List<HashSet<ulong>> UserQuery;

        public OrganizerSecondaryData(OrganizerCoreData cd)
        {
            UserQuery = new List<HashSet<ulong>>();
            foreach (var weeklyList in cd.SignUpLists)
            {
                HashSet<ulong> userSet = new HashSet<ulong>();
                foreach (var user in weeklyList)
                {
                    userSet.Add(user);
                }
                UserQuery.Add(userSet);
            }
        }

        public void Clear()
        {
            foreach(var week in UserQuery)
            {
                week.Clear();
            }
        }
    }

    class EventOrganizer
    {
        private BotProperties _p;
        private SemaphoreSlim _lock;
        private OrganizerCoreData _data;
        private OrganizerSecondaryData _secondary;
        private DiscordGuild _targetGuild;
        private SocketTextChannel _targetChannel;
        private SocketTextChannel _backupChannel;

        // Hardcoded for now.
        private const ulong _targetDiscord = 620608384227606528;
        private const string _targetChannelName = "🔔oznámení-customek";
        // private const string _targetChannelName = "rank-bot-admin"; // Debug.
        private const string _backupChannelName = "robomoderator-backups"; // Debug.
        private const ulong _botId = 747390449366466640;
        private string[] shortDayNames = { "Po", "Ut", "St", "Ct", "Pa", "So", "Ne" };
        private const string _backupFileName = "r6events.json";
        private List<Emote> _emoteDayNames;

        public EventOrganizer(BotProperties props)
        {
            _p = props;
            _lock = new SemaphoreSlim(1, 1);
            _targetGuild = _p.Guilds.byId[_targetDiscord];
            _targetChannel = _targetGuild.GetChannel(_targetChannelName);
            _backupChannel = _targetGuild.GetChannel(_backupChannelName);

            _emoteDayNames = new List<Emote> {
                              Emote.Parse("<:pondeli:854328055799742485>"),
                              Emote.Parse("<:utery:854331942254149673>"),
                              Emote.Parse("<:streda:854331955964411914>"),
                              Emote.Parse("<:ctvrtek:854331971655827456>"),
                              Emote.Parse("<:patek:854331983349284894>"),
                              Emote.Parse("<:sobota:854331997799448626>"),
                              Emote.Parse("<:nedele:854332011807506442>")
            };
        }

        public string BuildMessage()
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append("Rozpis: \n");
            int day = 0;
            int dayOfTheWeek = ((int)DateTime.Today.DayOfWeek + 6) % 7;

            foreach (var weeklyList in _data.SignUpLists)
            {
                StringBuilder weekBuilder = new StringBuilder();
                if (dayOfTheWeek > day)
                {
                    weekBuilder.Append("~~");
                }
                weekBuilder.Append($"{_emoteDayNames[day]}");
                // weekBuilder.Append($"{shortDayNames[day]}: ");

                // Write a parenthesis on how many people are signed up
                weekBuilder.Append($" ({Math.Min(10,weeklyList.Count)}");
                if (weeklyList.Count > 10)
                {
                    weekBuilder.Append($" + {weeklyList.Count - 10}");
                }
                weekBuilder.Append("): ");

                bool first = true;
                int userOrder = 0;
                foreach (ulong userId in weeklyList)
                {
                    if (userOrder == 10)
                    {
                        weekBuilder.Append(" | N: ");
                        first = true;
                    }
                    if (!first)
                    {
                        weekBuilder.Append(", ");
                    }
                    else
                    {
                        first = false;
                    }

                    string nextName = _targetGuild.GetNicknameOrName(userId);
                    if (nextName == null)
                    {
                        // This can actually happen if a user signs up, then leaves the server.
                        // Fix it somehow?

                        throw new Exception("Could not find the user that joined for this event.");
                    }

                    weekBuilder.Append(Discord.Format.Sanitize(nextName));
                    userOrder++;
                }

                if (dayOfTheWeek > day)
                {
                    weekBuilder.Append("~~");
                }

                messageBuilder.Append(weekBuilder.ToString());
                messageBuilder.Append("\n");
                day++;
            }

            return messageBuilder.ToString();
        }

        public async Task EventButtonHandlerAsync(SocketMessageComponent component)
        {
            // All buttons contain the word event.
            string customId = component.Data.CustomId;
            Console.WriteLine($"Button triggered, custom id {customId}");

            if (!customId.Contains("event"))
            {
                return;
            }

            // Extract which day of the week it is.
            var match = Regex.Match(customId, @"event-(\d+)");
            if (match.Groups.Count < 2)
            {
                return;
            }

            string matchedNumericalDay = match.Groups[1].Value;

            if (matchedNumericalDay == null)
            {
                return;
            }

            int numericalDay = Int32.Parse(matchedNumericalDay);

            if (numericalDay < 0 || numericalDay >= 7)
            {
                throw new Exception("Day out of bounds!");
            }

            ulong userId = component.User.Id;
            await _lock.WaitAsync();

            bool addition = true;

            if (_secondary.UserQuery[numericalDay].Contains(userId))
            {
                // Removal.
                addition = false;
                _data.SignUpLists[numericalDay].Remove(userId); // Technically, this is slow, but who cares.
                _secondary.UserQuery[numericalDay].Remove(userId);
            }
            else
            {
                addition = true;
                // Addition.
                _data.SignUpLists[numericalDay].Add(component.User.Id);
                _secondary.UserQuery[numericalDay].Add(component.User.Id);

            }
            // Backup the data.
            await BackupDataAsync();

            // Update the sign sheet message.
            await UpdateSignupMessageAsync();

            _lock.Release();

            // Send a response to the user.
            if (addition)
            {
                await component.RespondAsync($"Pridan na {shortDayNames[numericalDay]}!", ephemeral: true);
            }
            else
            {
                await component.RespondAsync($"Odebran z {shortDayNames[numericalDay]}!", ephemeral: true);
            }

        }

        public async Task UpdateSignupMessageAsync()
        {
            if (_data.MessageWithSignups == 0)
            {
                return;
            }

            string newText = BuildMessage();
            var msg = await _targetChannel.GetMessageAsync(_data.MessageWithSignups);
            var oldMessage = msg as IUserMessage;
            await oldMessage.ModifyAsync(x => x.Content = newText);
        }

        public async Task RecoverDataAsync()
        {
            await _lock.WaitAsync();
            bool newBuild = false;
            var amsg = _backupChannel.GetMessagesAsync();
            IAsyncEnumerable<IMessage> messages = null;
            IMessage[] msgarray = null;
            OrganizerCoreData o = null;

            if (amsg == null)
            {
                newBuild = true;
            }
            else
            {
                messages = _backupChannel.GetMessagesAsync().Flatten();
                msgarray = await messages.ToArrayAsync();

                if (msgarray.Count() != 1)
                {
                    newBuild = true;
                }
            }

            if(newBuild)
            {
                Console.WriteLine("Warning: Recovery failed, creating structures anew.");
                o = new OrganizerCoreData();
                o.SignUpLists = new List<List<ulong>>();
                for (int day = 0; day < 7; day++)
                {
                    o.SignUpLists.Add(new List<ulong>());
                }

                o.MessageWithButtons = 0;
                o.MessageWithSignups = 0;
            }
            else
            {

                var client = new HttpClient();
                var dataString = await client.GetStringAsync(msgarray[0].Attachments.First().Url);
                TextReader stringr = new StringReader(dataString);
                JsonSerializer serializer = new JsonSerializer();
                o = (OrganizerCoreData)serializer.Deserialize(stringr, typeof(OrganizerCoreData));

                o.FillGaps();
            }

            _data = o;
            _secondary = new OrganizerSecondaryData(_data); // Initialize secondary structures.
            _lock.Release();
        }

        public async Task BackupDataAsync()
        {
            // First, backup to a file.
            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(_backupFileName))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, _data);
            }

            // With the file ready, back up to the channel.
            // First, delete the previous backup. (This is why we also have a secondary backup.)
            var messages = _backupChannel.GetMessagesAsync().Flatten();
            var msgarray = await messages.ToArrayAsync();
            if (msgarray.Count() > 1)
            {
                Console.WriteLine($"The bot found {msgarray.Count()} messages but can only delete one due to safety. Aborting backup.");
            }

            if (msgarray.Count() == 1)
            {
                await _backupChannel.DeleteMessageAsync(msgarray[0]);
            }

            // Now, upload the new backup.
            await _backupChannel.SendFileAsync(_backupFileName, $"Backup file created at {DateTime.Now.ToShortTimeString()}.");
        }

        public async Task SendNewMessagesAsync()
        {
            await _lock.WaitAsync();
            _data.Clear();
            _secondary.Clear();
            await BuildButtonMessageAsync();
            string signups = BuildMessage();
            var signupsSent = await _targetChannel.SendMessageAsync(signups);
            _data.MessageWithSignups = signupsSent.Id;

            await BackupDataAsync();
            _lock.Release();
        }

        public async Task GenerateSlashCommandAsync(DiscordSocketClient client)
        {
            SlashCommandBuilder guildCommand = new SlashCommandBuilder();
            guildCommand.WithName("customs-new-week");
            guildCommand.WithDescription("Create custom signups for a new week. Only the admin can do this.");
            try
            {
                await client.Rest.CreateGuildCommand(guildCommand.Build(), _targetGuild.Id);
            }
            catch (HttpException e)
            {
                Console.WriteLine($"RoboModerator: Guild command build error {e.HttpCode}:  {e.Message}");
            }

            SlashCommandBuilder refreshSignupCommand = new SlashCommandBuilder();
            refreshSignupCommand.WithName("customs-refresh-signup");
            refreshSignupCommand.WithDescription("Refresh the signup text. Only the admin can do this.");
            try
            {
                await client.Rest.CreateGuildCommand(refreshSignupCommand.Build(), _targetGuild.Id);
            }
            catch (HttpException e)
            {
                Console.WriteLine($"RoboModerator: Refresh signup build error {e.HttpCode}:  {e.Message}");
            }

            SlashCommandBuilder removePersonCammand = new SlashCommandBuilder();
            removePersonCammand.WithName("customs-remove-person");
            removePersonCammand.WithDescription("Removes a person from a particular day. Only the admin can do this.");
            removePersonCammand.AddOption("user", ApplicationCommandOptionType.User, "The user to remove", isRequired: true);
            removePersonCammand.AddOption("day", ApplicationCommandOptionType.Integer,
                "The day of the week, Monday is 0.", isRequired: true);
            try
            {
                await client.Rest.CreateGuildCommand(removePersonCammand.Build(), _targetGuild.Id);
            }
            catch (HttpException e)
            {
                Console.WriteLine($"RoboModerator: Remove person build error {e.HttpCode}:  {e.Message}");
            }
        }

        public async Task CustomsNewWeekAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await SendNewMessagesAsync();
            await command.RespondAsync("Created!", ephemeral: true);
        }

        public async Task CustomsRemovePersonAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            var user = (SocketGuildUser)command.Data.Options.First(x => x.Name == "user").Value;
            Int64 sixtyFourBitDay = (Int64) command.Data.Options.First(x => x.Name == "day").Value;

            if (sixtyFourBitDay >= 7 || sixtyFourBitDay < 0)
            {
                await command.RespondAsync("The parameter day needs to be between 0 and 6.", ephemeral: true);
                return;
            }

            int day = Convert.ToInt32(sixtyFourBitDay);

            await _lock.WaitAsync();

            if (!_secondary.UserQuery[day].Contains(user.Id))
            {
                _lock.Release();
                await command.RespondAsync($"The user {user.Username} is not signed up for {shortDayNames[day]}.",
                    ephemeral: true);
                return;
            } else
            {
                _data.SignUpLists[day].Remove(user.Id); // Technically, this is slow, but who cares.
                _secondary.UserQuery[day].Remove(user.Id);

                await BackupDataAsync();
                await UpdateSignupMessageAsync();
                _lock.Release();
                await command.RespondAsync($"User{user.Username} removed from event {shortDayNames[day]}!", ephemeral: true);
            }
        }

        public async Task RefreshSignupCommandAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await _lock.WaitAsync();

            await UpdateSignupMessageAsync();
            _lock.Release();

            await command.RespondAsync($"Refreshed!", ephemeral: true);
        }

        public async Task BuildButtonMessageAsync()
        {

            var builder = new ComponentBuilder();
            for (int i = 0; i < 7; i++)
            {
                builder.WithButton($"Prihlasit/Odhlasit na {shortDayNames[i]}", $"event-{i}");
            }


            var sentMessage = await _targetChannel.SendMessageAsync("Zapiste se na customku tady!",
                    components: builder.Build());

            _data.MessageWithButtons = sentMessage.Id;
        }
    }
}