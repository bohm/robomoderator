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
        public async Task GifDeletionAsync(SocketUserMessage message, SocketTextChannel channel)
        {
            if (_data.NoGifChannels.Contains(channel.Id))
            {
                if (message.Content.Contains("giphy.com") || message.Content.Contains("tenor.com"))
                {
                    await SafeDelete(message);
                }
            }
        }
    }
}
