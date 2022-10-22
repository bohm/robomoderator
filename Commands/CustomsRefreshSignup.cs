using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Commands
{
    public class CustomsRefreshSignup : AdminCommonBase
    {
        public static readonly bool SlashCommand = true;

        public CustomsRefreshSignup()
        {
            SlashName = "customs-refresh-signup";
            SlashDescription = "The bot refreshes the message with lists of users from its memory. This may help with some issues.";
        }

        public async override Task ProcessCommandAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await Bot.Instance.Orga.LockAndUpdateMessage();
            await command.RespondAsync($"Refreshed!", ephemeral: true);
        }
    }
}
