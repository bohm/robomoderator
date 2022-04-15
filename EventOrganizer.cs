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

    class EventOrganizer
    {
        private BotProperties _p;
        private OrganizerProperties _op;
        private SemaphoreSlim _lock;
        private OrganizerCoreData _data;
        private OrganizerProperties _secondary;
        // private DiscordGuild _targetGuild;
        private SocketTextChannel _targetChannel;
        private SocketTextChannel _backupChannel;

        private BackupSystem<OrganizerCoreData> _coreDataRecovery;
        // Hardcoded for now.
        // private const ulong _targetDiscord = 620608384227606528;
        // private const string _targetChannelName = "🔔oznámení-customek";
        // private const string _targetChannelName = "rank-bot-admin"; // Debug.
        // private const string _backupChannelName = "robomoderator-backups"; // Debug.
        // private const ulong _botId = 747390449366466640;
        private string[] shortDayNames = { "Po", "Ut", "St", "Ct", "Pa", "So", "Ne" };
        private const string _backupFileName = "r6events.json";
        private List<Emote> _emoteDayNames;

        public EventOrganizer(BotProperties props)
        {
            _p = props;
            _lock = new SemaphoreSlim(1, 1);
            // _targetGuild = _p.Guilds.byId[_targetDiscord];
            // _targetChannel = _targetGuild.GetChannel(_targetChannelName);
            // _backupChannel = _targetGuild.GetChannel(_backupChannelName);

            _emoteDayNames = new List<Emote> {
                              Emote.Parse("<:pondeli:854328055799742485>"),
                              Emote.Parse("<:utery:854331942254149673>"),
                              Emote.Parse("<:streda:854331955964411914>"),
                              Emote.Parse("<:ctvrtek:854331971655827456>"),
                              Emote.Parse("<:patek:854331983349284894>"),
                              Emote.Parse("<:sobota:854331997799448626>"),
                              Emote.Parse("<:nedele:854332011807506442>")
            };

            // Recover backup data.

        }
        
        /// <summary>
        /// Initializes the OrganizerProperties structures, which means that it runs the recovery process from the Discord message channels.
        /// In principle, it can be integrated into the constructor, but it requires to be async, which is why we do it separately.
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            _coreDataRecovery = new BackupSystem<OrganizerCoreData>(_p.Primary,
                Settings.EventConfigurationChannel, Settings.EventConfigurationFile);

            OrganizerCoreData cd = await _coreDataRecovery.RecoverAsync();
            foreach (var singleEventData in cd.DataList)
            {
                singleEventData.FillGaps();
            }

            _op = new OrganizerProperties(cd);
        }


        public string BuildMessage(ulong targetGuild)
        {
            // First, recover event data of the target guild.
            SingleGuildEventData data = _op.PrimaryById[targetGuild];
            // Also, recover the Discord API for the target guild.
            DiscordGuild targetAPI = _p.Guilds.byId[targetGuild];

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append("Rozpis: \n");
            int day = 0;
            int dayOfTheWeek = ((int)DateTime.Today.DayOfWeek + 6) % 7;

            foreach (var weeklyList in data.SignUpLists)
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

                    string nextName = targetAPI.GetNicknameOrName(userId);
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
            // First, find the target guild.
            // We only parse buttons from guild messages currently, so this should be fine.
            SocketGuildChannel contextChannel = component.Channel as SocketGuildChannel;
            ulong contextGuildId = contextChannel.Guild.Id;
            DiscordGuild targetAPI = _p.Guilds.byId[contextGuildId];
            SingleGuildEventData data = _op.PrimaryById[contextGuildId];
            SingleGuildSecondaryData secondary = _op.SecondaryById[contextGuildId];

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

            if (secondary.UserQuery[numericalDay].Contains(userId))
            {
                // Removal.
                addition = false;
                data.SignUpLists[numericalDay].Remove(userId); // Technically, this is slow, but who cares.
                secondary.UserQuery[numericalDay].Remove(userId);
            }
            else
            {
                addition = true;
                // Addition.
                data.SignUpLists[numericalDay].Add(component.User.Id);
                secondary.UserQuery[numericalDay].Add(component.User.Id);

            }
            // Backup the data.
            await BackupDataAsync();

            // Update the sign sheet message.
            await UpdateSignupMessageAsync(contextGuildId);

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

        public async Task UpdateSignupMessageAsync(ulong targetGuild)
        {
            // First, recover event data of the target guild.
            SingleGuildEventData data = _op.PrimaryById[targetGuild];
            // Also, recover the Discord API for the target guild.
            DiscordGuild targetAPI = _p.Guilds.byId[targetGuild];

            SocketTextChannel targetChannel = targetAPI.GetChannel(data.GuildAnnounceChannel);
            if (data.MessageWithSignups == 0)
            {
                return;
            }

            string newText = BuildMessage(targetGuild);
            var msg = await targetChannel.GetMessageAsync(data.MessageWithSignups);
            var oldMessage = msg as IUserMessage;
            await oldMessage.ModifyAsync(x => x.Content = newText);
        }

        public async Task BackupDataAsync()
        {
            await _coreDataRecovery.BackupAsync(_data);
        }

        public async Task SendNewMessagesAsync(ulong targetGuild)
        {
            // First, recover event data of the target guild.
            SingleGuildEventData data = _op.PrimaryById[targetGuild];
            SingleGuildSecondaryData secondary = _op.SecondaryById[targetGuild];

            // Also, recover the Discord API for the target guild.
            DiscordGuild targetAPI = _p.Guilds.byId[targetGuild];
            SocketTextChannel targetChannel = targetAPI.GetChannel(data.GuildAnnounceChannel);

            await _lock.WaitAsync();
            data.Clear();
            secondary.Clear();
            await BuildButtonMessageAsync();
            string signups = BuildMessage(targetGuild);
            var signupsSent = await targetChannel.SendMessageAsync(signups);
            data.MessageWithSignups = signupsSent.Id;

            await BackupDataAsync();
            _lock.Release();
        }

        public async Task GenerateGuildCommandsAsync(DiscordSocketClient client)
        {

            foreach (var kvp in _op.PrimaryById)
            {
                ulong guildId = kvp.Key;


                SlashCommandBuilder guildCommand = new SlashCommandBuilder();
                guildCommand.WithName("customs-new-week");
                guildCommand.WithDescription("Create custom signups for a new week. Only the admin can do this.");
                try
                {
                    await client.Rest.CreateGuildCommand(guildCommand.Build(), guildId);
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
                    await client.Rest.CreateGuildCommand(refreshSignupCommand.Build(), guildId);
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
                    await client.Rest.CreateGuildCommand(removePersonCammand.Build(), guildId);
                }
                catch (HttpException e)
                {
                    Console.WriteLine($"RoboModerator: Remove person build error {e.HttpCode}:  {e.Message}");
                }
            }
        }

        public async Task CustomsNewWeekAsync(SocketSlashCommand command)
        {
            // This command should only work as a discord guild command, so getting a discord ID should be
            // possible.

            SocketGuildChannel contextChannel = command.Channel as SocketGuildChannel;
            if (contextChannel == null)
            {
                await command.RespondAsync($"Command {command.CommandName} internally failed: conversion error.");
            }

            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await SendNewMessagesAsync(contextChannel.Guild.Id);
            await command.RespondAsync("Created!", ephemeral: true);
        }

        public async Task CustomsRemovePersonAsync(SocketSlashCommand command)
        {
            // This command should only work as a discord guild command, so getting a discord ID should be
            // possible.

            SocketGuildChannel contextChannel = command.Channel as SocketGuildChannel;
            if (contextChannel == null)
            {
                await command.RespondAsync($"Command {command.CommandName} internally failed: conversion error.");
            }


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