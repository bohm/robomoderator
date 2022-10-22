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

    public class OlderCommands : ModuleBase<SocketCommandContext>
    {
        [Command("domovoj")]
        public async Task Info()
        {
            await ReplyAsync(@"Jsem bot pomahajici s udrzbou tady na serveru.
Jmenuji se podle slovanske mytologicke bytosti Domovoje, ktery strazi domacnost:
https://cs.wikipedia.org/wiki/Domovoj. Zatim nepodporuji zadne prikazy, ale budu
v budoucnosti.");
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

        // -- The commands below are primarily administrative and testing in nature. --

        [Command("manualclear")]
        public async Task ManualClear()
        {
            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("ResidentGuild is not set, cannot continue for now.");
                return;
            }

            if (!Settings.Operators.Contains(Context.Message.Author.Id))
            {
                await ReplyAsync("This command requires admin/operator privileges.");
                return;
            }

            _ = Bot.Instance.ManualClear(Context.Channel);

        }

        [Command("delayeduserping")]
        public async Task DelayedUserPing(string discordNick)
        {
            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("ResidentGuild is not set, cannot continue for now.");
                return;
            }

            if (!Settings.Operators.Contains(Context.Message.Author.Id))
            {
                await ReplyAsync("This command requires admin/operator privileges.");
                return;
            }

            var target = Bot.Instance.UserByName(discordNick);
            if (target == null)
            {
                await ReplyAsync(Context.Message.Author.Username + ": Nenasli jsme cloveka ani podle prezdivky, ani podle Discord jmena.");
                return;
            }

            _ = Bot.Instance.DelayedUserPing(Context.Message.Channel, target);

        }

        [Command("delayedroleping")]
        public async Task DelayedRolePing(string roleName)
        {
            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("ResidentGuild is not set, cannot continue for now.");
                return;
            }

            if (!Settings.Operators.Contains(Context.Message.Author.Id))
            {
                await ReplyAsync("This command requires admin/operator privileges.");
                return;
            }

            var target = Bot.Instance.RoleByName(roleName);
            if (target == null)
            {
                await ReplyAsync(Context.Message.Author.Mention + ": Nenasli jsme roli.");
                return;
            }

            _ = Bot.Instance.DelayedRolePing(Context.Message.Channel, target);

        }

    }
}
