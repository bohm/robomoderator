using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
namespace RoboModerator
{
    /// <summary>
    /// RoleGranter builds a method that grants each user that joins a Discord guild
    /// any role (from the specified list) which they also had on the old Discord guild.
    /// </summary>
    class RoleGranter
    {
        private List<string> _roles;
        private DiscordGuild _newGuild;
        private DiscordGuild _oldGuild;

        public RoleGranter(List<string> grantedRoles, DiscordGuild newServer, DiscordGuild oldServer)
        {
            _roles = grantedRoles;
            _newGuild = newServer;
            _oldGuild = oldServer;
        }


        public async void UserJoinedAsync(SocketGuildUser newUser)
        {
            var userOnOld = _oldGuild.GetSingleUser(newUser.Username);
            if (userOnOld == null)
            {
                return;
            }

            foreach (string roleName in _roles)
            {
                SocketRole oldServerRole = _oldGuild.RoleByName(roleName);
                SocketRole newServerRole = _newGuild.RoleByName(roleName);


                if (oldServerRole == null || newServerRole == null)
                {
                    throw new RoleSyncException("The specified role is not found on one of the Discord guilds.");
                }

                if (userOnOld.Roles.Contains(oldServerRole))
                {
                    await newUser.AddRoleAsync(newServerRole);
                    Console.WriteLine($"Granted role {roleName} for user {newUser.Username}");
                }
            }
        }

        public async void GrantRolesFromOld()
        {

            foreach (string roleName in _roles)
            {
                SocketRole oldServerRole = _oldGuild.RoleByName(roleName);
                SocketRole newServerRole = _newGuild.RoleByName(roleName);


                if (oldServerRole == null || newServerRole == null)
                {
                    throw new RoleSyncException("The specified role is not found on one of the Discord guilds.");
                }

                foreach (var userOnNew in _newGuild._socket.Users)
                {
                    if (_oldGuild.IsGuildMember(userOnNew.Id))
                    {
                        var userOnOld = _oldGuild.GetSingleUser(userOnNew.Id);
                        if (userOnOld == null)
                        {
                            throw new RoleSyncException("User is a member of the old guild and yet we could not get its object.");
                        }

                        if (!userOnNew.Roles.Contains(newServerRole) && userOnOld.Roles.Contains(oldServerRole))
                        {
                            await userOnNew.AddRoleAsync(newServerRole);
                            Console.WriteLine($"Granted role {roleName} for user {user.Username}");
                        }
                    }
                }
            }
        }
    }
}
