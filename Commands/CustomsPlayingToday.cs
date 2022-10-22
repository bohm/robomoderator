using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Commands
{
    public class CustomsPlayingToday : UserCommonBase
    {
        public static readonly bool SlashCommand = true;

        public CustomsPlayingToday()
        {
            SlashName = "customs-playing-today";
            SlashDescription = "The bot lists who plays today in numeric form. This might be most useful to another bot.";
        }

        public async override Task ProcessCommandAsync(SocketSlashCommand command)
        {
            int today = ((int)DateTime.Today.DayOfWeek + 6) % 7;
            string todayPlaying = Bot.Instance.Orga.TopTenPlaying(today);
            await command.RespondAsync($"{todayPlaying}", ephemeral: true);
        }
    }
}