using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboModerator
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
    }
}
