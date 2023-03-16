using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Commands
{
    public class MakeNewTournament : UserCommonBase
    {
        public static readonly bool SlashCommand = true;

        public MakeNewTournament()
        {
            SlashName = "make-new-tournament";
            SlashDescription = "Create a new signup for the tournament. Only the admin can run this.";
        }

        public async override Task ProcessCommandAsync(SocketSlashCommand command)
        {
            if (!Settings.Operators.Contains(command.User.Id))
            {
                await command.RespondAsync("Only the admins can run this.", ephemeral: true);
                return;
            }

            await Bot.Instance.Tourney.NewTournamentAsync(command);
            await command.RespondAsync($"New tournament event initialized!", ephemeral: true);
        }
    }
}

