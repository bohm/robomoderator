using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator
{
    public partial class MessageManager
    {
        private BotProperties _prop;
        private MessageManagerData _data = null;
        private bool _ready = false;

        public MessageManager(BotProperties bp, DiscordSocketClient dsc)
        {
            _data = new MessageManagerData();
            _prop = bp;
            dsc.MessageReceived += MessageHandlerAsync;
            _ready = true;
        }

        public bool RecentMessage(SocketUserMessage msg)
        {
            TimeSpan diff = (DateTimeOffset.Now - msg.Timestamp);
            return (diff.Duration() <= Settings.MessageRecentTime);
        }
        /// <summary>
        /// Delete a message performing some last-minute checks to make sure
        /// the whole Discord server is not deleted.
        /// </summary>
        /// <param name="messageToDelete"></param>
        /// <returns></returns>
        public async Task SafeDelete(SocketUserMessage messageToDelete)
        {
            if (!RecentMessage(messageToDelete))
            {
                return;
            }

            // Another possible check: whitelist of users/administrators.

            await LogDeletedMessage(messageToDelete);
            await messageToDelete.Channel.DeleteMessageAsync(messageToDelete.Id);
        }

        protected async Task LogDeletedMessage(SocketUserMessage msg)
        {
            if (!Settings.Logging)
            {
                return;
            }

            var channel = msg.Channel;
            if (channel is SocketTextChannel guildChannel)
            {
                ulong sourceGuild = guildChannel.Guild.Id;
                // Only manage messages from the guilds you were assigned to.
                if (!_prop.Guilds.byId.ContainsKey(sourceGuild))
                {
                    return;
                }

                DiscordGuild g = _prop.Guilds.byId[sourceGuild];
                if (g.Config.loggingChannel == null)
                {
                    return;
                }

                // Grab the logging channel.
                Discord.WebSocket.SocketTextChannel logChan = g._socket.TextChannels.First(x => x.Name == g.Config.loggingChannel);

                if (logChan == null)
                {
                    return;
                }


                string logString = $"{DateTime.Now.ToString("s")}: Deleted message by {msg.Author.Username} in channel {msg.Channel.Name}: '{msg.Content}'.";
                await logChan.SendMessageAsync(logString);
            }
        }
        public async Task TestingDeletionAsync(SocketUserMessage message, SocketTextChannel channel)
        {
            if (message.Content == "delete me")
            {
                await SafeDelete(message);
            }
        }
        public async Task MessageHandlerAsync(SocketMessage rawmsg)
        {
            if(!_ready)
            {
                return;
            }

            SocketUserMessage message = rawmsg as SocketUserMessage;
            if (message is null || message.Author.IsBot)
            {
                return; // Ignore all bot messages and empty messages.
            }
            var contextChannel = message.Channel;
            // The message may be a DM, so we cast it to determine its type and work only with guild chat.
            if (contextChannel is SocketTextChannel guildChannel)
            {
                ulong sourceGuild = guildChannel.Guild.Id;


                // Only manage messages from the guilds you were assigned to.
                if (!_prop.Guilds.byId.ContainsKey(sourceGuild))
                {
                    return;
                }

                await TestingDeletionAsync(message, guildChannel);
                await GifDeletionAsync(message, guildChannel);
            }

        }
    }
}
