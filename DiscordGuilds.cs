using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator
{
    class DiscordGuilds
    {
        public List<DiscordGuild> guildList;
        public Dictionary<string, DiscordGuild> byName;
        public Dictionary<ulong, DiscordGuild> byId;

        public DiscordGuilds()
        {
            guildList = new List<DiscordGuild>();
            byName = new Dictionary<string, DiscordGuild>();
            byId = new Dictionary<ulong, DiscordGuild>();
        }

        public void Add(DiscordGuild g)
        {
            if(byId.ContainsKey(g.Id) || byName.ContainsKey(g.GetName()))
            {
                throw new Exception("Trying to add this guild twice!");
            }

            guildList.Add(g);
            byName.Add(g.GetName(), g);
            byId.Add(g.Id, g);
        }
    }



    class DiscordGuild
    {
        private SocketGuild _socket;

        public ulong Id;
        public string GetName()
        {
            return _socket.Name;
        }


        public DiscordGuild(SocketGuild socket)
        {
            Id = socket.Id;
            _socket = socket;
        }

        /// <summary>
        /// Queries the Discord API to figure out if a user is a member of a guild.
        /// </summary>
        /// <param name="discordID"></param>
        /// <returns></returns>
        public bool IsGuildMember(ulong discordID)
        {
            if (_socket.Users.FirstOrDefault(x => x.Id == discordID) != null)
            {
                return true;
            }

            return false;
        }

        public SocketGuildUser GetSingleUser(ulong discordId)
        {
            return _socket.Users.FirstOrDefault(x => ((x.Id == discordId)));
        }
        public SocketGuildUser GetSingleUser(string discNameOrNick)
        {
            return _socket.Users.FirstOrDefault(x => ((x.Username == discNameOrNick) || (x.Nickname == discNameOrNick)));
        }

        public IEnumerable<SocketGuildUser> GetAllUsers(string discNameOrNick)
        {
            return _socket.Users.Where(x => ((x.Username == discNameOrNick) || (x.Nickname == discNameOrNick)));

        }

        public string GetNicknameOrName(ulong discordId)
        {
            SocketGuildUser user = GetSingleUser(discordId);
            if (user == null)
            {
                return null;
            }

            if (user.Nickname != null)
            {
                return user.Nickname;
            } else
            {
                return user.Username;
            }
        }

        public SocketTextChannel GetChannel(string name)
        {
            return _socket.Channels.FirstOrDefault(x => x.Name == name) as SocketTextChannel;
        }

        public async Task<bool> AddRoleAsync(string roleName, ulong discordId)
        {
            int roleCount = _socket.Roles.Count(x => x.Name == roleName);
            if (roleCount == 0 || roleCount >= 2)
            {
                Console.WriteLine("There is zero roles or more than one role of the same name, terminating.");
                return false;
            }

            SocketRole role = _socket.Roles.FirstOrDefault(x => x.Name == roleName);
            if (role == null)
            {
                return false;
            }

            SocketGuildUser rightUser = GetSingleUser(discordId);

            if (rightUser.Roles.Contains(role))
            {
                Console.WriteLine("Trying to add a role for a second time, terminating.");
                return false;
            }

            await rightUser.AddRoleAsync(role);
            return true;
        }

        public async Task<bool> RemoveRoleAsync(string roleName, ulong discordId)
        {
            int roleCount = _socket.Roles.Count(x => x.Name == roleName);
            if (roleCount == 0 || roleCount >= 2)
            {
                Console.WriteLine("There is zero roles or more than one role of the same name, terminating.");
                return false;
            }

            SocketRole role = _socket.Roles.FirstOrDefault(x => x.Name == roleName);
            if (role == null)
            {
                return false;
            }

            SocketGuildUser rightUser = GetSingleUser(discordId);

            if (!rightUser.Roles.Contains(role))
            {
                Console.WriteLine("The user already does not have this role. Terminating.");
                return false;
            }

            await rightUser.RemoveRoleAsync(role);
            return true;
        }


        /// <summary>
        /// A one-shot method to give everyone in a guild one specific role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async Task GiveEveryoneARoleAsync(string roleName)
        {
            int roleCount = _socket.Roles.Count(x => x.Name == roleName);
            if (roleCount == 0 || roleCount >= 2)
            {
                Console.WriteLine("There is zero roles or more than one role of the same name, terminating.");
            }

            SocketRole rightRole = _socket.Roles.FirstOrDefault(x => x.Name == roleName);
            var allUsers = _socket.Users;

            foreach (var user in allUsers)
            {
                if (!user.IsBot)
                {
                    await user.AddRoleAsync(rightRole);
                }
            }

        }

    }
}
