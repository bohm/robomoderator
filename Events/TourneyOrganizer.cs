
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

namespace RoboModerator.Events
{
    class TourneyOrganizer
    {
        private BotProperties _p;
        private SemaphoreSlim _lock;
        private DiscordGuild _targetGuild;
        private TourneyData _data;
        private BackupSystem<TourneyData> _backupSystem;
        // Hardcoded for now.
        private const ulong _targetDiscord = 620608384227606528;
        private const string _targetChannelName = "🏁turnaj-oznámení";
        private const string _backupChannelName = "robomod-tourney-backup";
        private const ulong _botId = 747390449366466640;
        private const string _backupFileName = "r6tourney.json";


        public TourneyOrganizer(BotProperties props)
        {
            _p = props;
            _lock = new SemaphoreSlim(1, 1);
            _targetGuild = _p.Guilds.byId[_targetDiscord];
        }

        public async Task DelayedInitAsync()
        {
            _data = new TourneyData();
            if (!_p.Primary._socket.TextChannels.Any(x => x.Name == _backupChannelName))
            {
                _backupSystem = await BackupSystem<TourneyData>.NewBackupSystemAsync(_backupChannelName, _backupFileName, _data, _p.Primary);
            }
            else
            {
                _backupSystem = new BackupSystem<TourneyData>(_p.Primary, _backupChannelName, _backupFileName);
                _data = await _backupSystem.RecoverAsync();
            }
        }

        public void RemoveUsersWhoLeaveDiscord()
        {
            _data.SignUpList.RemoveAll(x => !_targetGuild.IsGuildMember(x));
        }

        public string BuildMessage()
        {
            RemoveUsersWhoLeaveDiscord();

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append("Rozpis: \n");

            StringBuilder listBuilder = new StringBuilder();

            // Write a parenthesis on how many people are signed up
            listBuilder.Append($" ({_data.SignUpList.Count}) ");
            bool first = true;
            int userOrder = 0;
           foreach (ulong userId in _data.SignUpList)
           {
                if (userOrder == 16)
                {
                    listBuilder.Append(" | N: ");
                    first = true;
                }
                
                if (!first)
                {
                    listBuilder.Append(", ");
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

                            throw new Exception($"Could not find the user with id {userId} that joined for this event.");
                        }

                        listBuilder.Append(Discord.Format.Sanitize(nextName));
                        userOrder++;
           }
            messageBuilder.Append(listBuilder.ToString());
            return messageBuilder.ToString();
        }

        public async Task EventButtonHandlerAsync(SocketMessageComponent component)
        {
            // All buttons contain the word event.
            string customId = component.Data.CustomId;
            Console.WriteLine($"Button triggered, custom id {customId}");

            if (!customId.Contains("tourney"))
            {
                return;
            }

            ulong userId = component.User.Id;
            await _lock.WaitAsync();

            bool addition = true;

            if (_data.SignUpList.Contains(userId))
            {
                // Removal.
                addition = false;
                _data.SignUpList.Remove(userId); // Technically, this is slow, but who cares.
            }
            else
            {
                addition = true;
                // Addition.
                _data.SignUpList.Add(userId);
            }
            // Backup the data.
            await _backupSystem.BackupAsync(_data);

            // Update the sign sheet message.
            await UpdateSignupMessageAsync();

            _lock.Release();

            // Send a response to the user.
            if (addition)
            {
                await component.RespondAsync($"Pridan na turnaj!", ephemeral: true);
            }
            else
            {
                await component.RespondAsync($"Odebran z turnaje!", ephemeral: true);
            }

        }

        public async Task UpdateSignupMessageAsync()
        {
            if (_data.MessageWithSignups == 0)
            {
                return;
            }

            string newText = BuildMessage();
            var targetChannel = _targetGuild.GetChannel(_targetChannelName);
            var msg = await targetChannel.GetMessageAsync(_data.MessageWithSignups);
            var oldMessage = msg as IUserMessage;
            await oldMessage.ModifyAsync(x => x.Content = newText);
        }

        public async Task LockAndUpdateMessage()
        {
            await _lock.WaitAsync();
            await UpdateSignupMessageAsync();
            _lock.Release();
        }

        public async Task SendNewMessagesAsync()
        {
            await _lock.WaitAsync();
            _data.Clear();

            await BuildButtonMessageAsync();
            string signups = BuildMessage();
            var targetChannel = _targetGuild.GetChannel(_targetChannelName);
            var signupsSent = await targetChannel.SendMessageAsync(signups);
            _data.MessageWithSignups = signupsSent.Id;

            await _backupSystem.BackupAsync(_data);
            _lock.Release();
        }

        public async Task NewTournamentAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await SendNewMessagesAsync();
        }

        public async Task BuildButtonMessageAsync()
        {
            var targetChannel = _targetGuild.GetChannel(_targetChannelName);
            var builder = new ComponentBuilder();
            builder.WithButton("Prihlasit/Odhlasit na turnaj", "tourney");
            var sentMessage = await targetChannel.SendMessageAsync("Zapiste se na turnaj tady!",
                    components: builder.Build());
            _data.MessageWithButtons = sentMessage.Id;
        }
    }
}
