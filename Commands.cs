using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace domovoj
{

    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("domovoj")]
        public async Task Info()
        {
            await ReplyAsync(@"Jsem bot pomahajici s udrzbou tady na serveru.
Jmenuji se podle slovanske mytologicke bytosti Domovoje, ktery strazi domacnost:
https://cs.wikipedia.org/wiki/Domovoj. Zatim nepodporuji zadne prikazy, ale budu
v budoucnosti.");
        }


        [Command("domovojresidence")]
        public async Task Residence()
        {
            if (Bot.IsOperator(Context.Message.Author.Id) && Bot.Instance.ResidentGuild == null)
            {
                Bot.Instance.ResidentGuild = Context.Guild;
                await ReplyAsync("Setting up residence in guild " + Context.Guild.Name + ".");
                await Bot.Instance.ResidenceInit();

            }
        }

        [Command("joinedat")]
        public async Task JoinedAt(string discordUsername)
        {
            if(Bot.Instance.ResidentGuild != null)
            {
                var matchedUsers = Bot.Instance.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

                if (matchedUsers.Count() == 0)
                {
                    await ReplyAsync("There is no user matching the Discord nickname " + discordUsername + ".");
                    return;
                }

                if (matchedUsers.Count() > 1)
                {
                    await ReplyAsync("Two or more users have the same matching Discord nickname. This command cannot continue.");
                    return;
                }

                Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();

                await ReplyAsync("The user with the nickname " + discordUsername + " joined at " + rightUser.JoinedAt);

            }


        }

    }
}
