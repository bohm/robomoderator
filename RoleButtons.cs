using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator
{
    class RoleButtonsBackup
    {
        public string RoleName;
        public string ButtonAddId;
        public string ButtonRemoveId;
        public ulong TargetGuildId;
        public string ButtonChannelName;
        public ulong ButtonMessageId;
    }

    class RoleButtons
    {
        private string _roleName;
        private string _buttonAddId;
        private string _buttonRemoveId;

        private DiscordGuild targetGuild;

        public async Task ButtonHandlerAsync(SocketMessageComponent component)
        {
            if (component.Data.CustomId == _buttonAddId)
            {
                SocketGuildChannel guildChannel = component.Channel as SocketGuildChannel;

                if (guildChannel == null)
                {
                    return;
                }

                ulong guildId = guildChannel.Guild.Id;

                if (guildId != targetGuild.Id)
                {
                    return;
                }

                ulong targetUser = component.User.Id;

                bool success = await targetGuild.AddRoleAsync(_roleName, targetUser);

                if (success)
                {
                    await component.RespondAsync($"Přidána role {_roleName}!", ephemeral: true);
                }
                else
                {
                    await component.RespondAsync($"Nepodařilo se přidat roli {_roleName}. Buď se stala chyba, nebo" +
                        $"už ji máte.", ephemeral: true);
                }
            }
            else if (component.Data.CustomId == _buttonRemoveId)
            {
                SocketGuildChannel guildChannel = component.Channel as SocketGuildChannel;

                if (guildChannel == null)
                {
                    return;
                }

                ulong guildId = guildChannel.Guild.Id;

                if (guildId != targetGuild.Id)
                {
                    return;
                }

                ulong targetUser = component.User.Id;

                bool success = await targetGuild.RemoveRoleAsync(_roleName, targetUser);


                if (success)
                {
                    await component.RespondAsync($"Odebrána role {_roleName}!", ephemeral: true);
                }
                else
                {
                    await component.RespondAsync($"Nepodařilo se odebrat roli {_roleName}. Buď se stala chyba, nebo" +
    $"ji vůbec nemáte.", ephemeral: true);
                }
            }
        }



        /// <summary>
        /// Posts a message with adding or removing the role and generates the buttons.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="roleName"></param>
        /// <param name="buttonKeyword"></param>
        /// <param name="channelName"></param>
        /// <returns></returns>
        /*
         * public static async Task<RoleButtons> BuildButtonWithMessage(DiscordGuild guild, string roleName, string buttonKeyword,
            string channelName, string addRoleButtonLabel, string removeRoleButtonLabel)
        {

        }
        */


    }
}
