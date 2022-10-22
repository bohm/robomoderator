using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Commands
{
    public class NewRankedRoom : UserCommonBase
    {
        public static readonly bool SlashCommand = true;

        public NewRankedRoom()
        {
            SlashName = "new-ranked-room";
            SlashDescription = "Bot vytvori novou mistnost v kategorii ranked s Vami zvolenym jmenem.";
            ParameterList.Add(new CommandParameter("roomname", Discord.ApplicationCommandOptionType.String, "Jmeno mistnosti", true));
        }

        public async override Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            var author = (SocketGuildUser)command.User;
            DiscordGuild contextGuild = Bot.Instance.BotProps.Guilds.byId[author.Guild.Id];
            await command.DeferAsync(ephemeral: true);

            var roomname = (string)command.Data.Options.First(x => x.Name == "roomname").Value;

            if (roomname == null)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = "Chybi parametr  'Jmeno mistnosti', nemuzeme pokrcovat.");
                return;
            }

            await LogCommand(contextGuild, author, "/new-ranked-room", $"/new-ranked-room {roomname}");

            try
            {
                await Bot.Instance.RoomReg.CreateNewRoom(contextGuild, author, Settings.RankedRoomCategory, roomname);
            }
            catch (Exception e)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Nepodarilo se vytvorit novou mistnost, duvod: {e.Message}");
                await LogError(contextGuild, author, "/new-ranked-room", e.Message);
                return;
            }

            await command.ModifyOriginalResponseAsync(
                resp => resp.Content = $"Vytvorili jsme pro vas novou ranked mistnost {roomname}. Mistnost bude existovat do zitrku 04:00 hodin.");
        }
    }
}
