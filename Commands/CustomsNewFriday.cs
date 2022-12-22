
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Commands
{
    public class CustomsNewFriday : UserCommonBase
    {
        public static readonly bool SlashCommand = true;

        public CustomsNewFriday()
        {
            SlashName = "customs-friday";
            SlashDescription = "Create custom signups for Friday only. Only the admin can run this.";
        }

        public async override Task ProcessCommandAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await Bot.Instance.Orga.CustomFridayAsync(command);
            await command.RespondAsync($"New friday event initialized!", ephemeral: true);
        }
    }
}
