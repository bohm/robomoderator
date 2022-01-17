using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator
{
    class ButtonHandler
    {

        private BotProperties _p;

        public ButtonHandler(BotProperties props)
        {
            _p = props;
        }

        public async Task ButtonHandlerAsync(SocketMessageComponent component)
        {
            switch(component.Data.CustomId)
            {
                case "add-contest":
                    {
                        SocketGuildChannel guildChannel = component.Channel as SocketGuildChannel;

                        if (guildChannel == null)
                        {
                            return;
                        }

                        ulong guildId = guildChannel.Guild.Id;

                        if (!_p.Guilds.byId.ContainsKey(guildId))
                        {
                            return;
                        }

                        DiscordGuild targetGuild = _p.Guilds.byId[guildId];
                        ulong targetUser = component.User.Id;

                        bool success = await targetGuild.AddRoleAsync("Soutěž", targetUser);

                        if (success)
                        {
                            await component.RespondAsync("Přidán do soutěže!", ephemeral: true);
                        }
                        else
                        {
                            await component.RespondAsync("We could not add you. Maybe you are already a member?", ephemeral: true);
                        }

                    }
                    break;
                case "remove-contest":
                    {
                        SocketGuildChannel guildChannel = component.Channel as SocketGuildChannel;

                        if (guildChannel == null)
                        {
                            return;
                        }

                        ulong guildId = guildChannel.Guild.Id;

                        if (!_p.Guilds.byId.ContainsKey(guildId))
                        {
                            return;
                        }

                        DiscordGuild targetGuild = _p.Guilds.byId[guildId];
                        ulong targetUser = component.User.Id;

                        bool success = await targetGuild.RemoveRoleAsync("Soutěž", targetUser);


                        if (success)
                        {
                            await component.RespondAsync("Odstraněn ze soutěže!", ephemeral: true);
                        }
                        else
                        {
                            await component.RespondAsync("We could not remove you. Maybe you are no longer a member?", ephemeral: true);
                        }
                    }
                    break;

                case "add-meme-fans":
                    {
                        SocketGuildChannel guildChannel = component.Channel as SocketGuildChannel;

                        if (guildChannel == null)
                        {
                            return;
                        }

                        ulong guildId = guildChannel.Guild.Id;

                        if (!_p.Guilds.byId.ContainsKey(guildId))
                        {
                            return;
                        }

                        DiscordGuild targetGuild = _p.Guilds.byId[guildId];
                        ulong targetUser = component.User.Id;

                        bool success = await targetGuild.AddRoleAsync("Meme Fans", targetUser);

                        if (success)
                        {
                            await component.RespondAsync("Done!", ephemeral: true);
                        } else
                        {
                            await component.RespondAsync("We could not add you. Maybe you are already a member?", ephemeral: true);
                        }

                    }
                    break;

                case "remove-meme-fans":
                    {
                        SocketGuildChannel guildChannel = component.Channel as SocketGuildChannel;

                        if (guildChannel == null)
                        {
                            return;
                        }

                        ulong guildId = guildChannel.Guild.Id;

                        if (!_p.Guilds.byId.ContainsKey(guildId))
                        {
                            return;
                        }

                        DiscordGuild targetGuild = _p.Guilds.byId[guildId];
                        ulong targetUser = component.User.Id;

                        bool success = await targetGuild.RemoveRoleAsync("Meme Fans", targetUser);


                        if (success)
                        {
                            await component.RespondAsync("Removed from Meme Fans!", ephemeral: true);
                        }
                        else
                        {
                            await component.RespondAsync("We could not remove you. Maybe you are no longer a member?", ephemeral: true);
                        }
                    }
                    break;
            }
        }
    }
}
