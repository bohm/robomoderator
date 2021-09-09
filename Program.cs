using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Text;

namespace domovoj
{
    class Bot
    {
        public static Bot Instance;
        // The guild we are operating on. We start doing things only once this is no longer null.
        public Discord.WebSocket.SocketGuild ResidentGuild;

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private Dictionary<string, ulong> groupNameToID;
        private bool initComplete = false;

        private readonly SemaphoreSlim Access;

        private List<ulong> otherGameLobbyIds;
        // private List<Discord.WebSocket.SocketVoiceChannel> otherGameLobbies;
        private List<string> otherGames;
        private RoleHighlighting highlighter;
        public static bool IsOperator(ulong id)
        {
            return (Settings.Operators.Contains(id));
        }
        public Bot()
        {
            groupNameToID = new Dictionary<string, ulong>();
            highlighter = new RoleHighlighting();
            Instance = this;
            otherGameLobbyIds = new List<ulong>();
            otherGames = new List<string>();
            Access = new SemaphoreSlim(1, 1);

        }

        public string FancyName(string otherGame, int number)
        {
            number++;
            return otherGame + " Lobby " + number.ToString() + " (" + Settings.otherGameLobbyPrefix + ")";
        }

        public string DefaultName(int number)
        {
            number++;
            return Settings.otherGameLobbyPrefix + " Lobby " + number.ToString();
        }

        /// <summary>
        /// Gets the list of lobbies with Settings.otherGameLobbyPrefix in them.
        /// </summary>
        /// <param name="Guild"></param>
        /// <returns></returns>
        public IEnumerable<Discord.WebSocket.SocketVoiceChannel> GetLobbies(Discord.WebSocket.SocketGuild Guild)
        {
            var otherGameChannels = Guild.VoiceChannels.Where(x => x.Name.Contains(Settings.otherGameLobbyPrefix));
            return otherGameChannels;
        }

        public string GuessOtherGame(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketVoiceChannel lobby)
        {
            string activity = null;
            if (lobby != null)
            {
                foreach (Discord.WebSocket.SocketGuildUser user in lobby.Users)
                {
                    string username = user.Username;

                    if (user.Activity != null)
                    {
                        activity = user.Activity.ToString();
                        // Console.WriteLine(nickname + "is doing " + activity + "in " + lobby.Name + ".");
                        break;
                    }
                    else
                    {
                        Console.WriteLine(username + "is doing something private in " + lobby.Name + ".");
                    }
                }
            }

            return activity;
        }

        public async Task UpdateNames()
        {
            // Do nothing until the bot is attached to a guild/server.
            if (Bot.Instance.ResidentGuild == null)
            {
                Console.WriteLine("Resident guild is null, cannot proceed with UpdateNames for now.");
                return;
            }

            Discord.WebSocket.SocketGuild Guild = Bot.Instance.ResidentGuild;
            for (int i = 0; i < otherGameLobbyIds.Count; i++)
            {
                Discord.WebSocket.SocketVoiceChannel lobby = Guild.VoiceChannels.FirstOrDefault(y => y.Id == otherGameLobbyIds[i]);
                // Discord.WebSocket.SocketVoiceChannel lobby = otherGameLobbies[i];
                if (lobby == null)
                {
                    Console.WriteLine("Lobby cannot be found, which is strange and we cannot proceed further with the update.");
                    break;
                }

                string act = GuessOtherGame(this.ResidentGuild, lobby);
                if (act != null)
                {
                    if (!act.Equals(otherGames[i]))
                    {
                        if (lobby != null)
                        {
                            string newName = FancyName(act, i);
                            string currentName = lobby.Name;
                            if (!currentName.Equals(newName))
                            {
                                otherGames[i] = act;
                                Console.WriteLine("Renaming " + lobby.Name + " to " + newName + ".");
                                await lobby.ModifyAsync(prop => prop.Name = newName);
                            }
                        }
                    }
                }
                else // no activity detected, set default name
                {
                    if (lobby != null)
                    {
                        string newName = DefaultName(i);
                        string currentName = lobby.Name;
                        if (!currentName.Equals(newName))
                        {
                            otherGames[i] = "";
                            Console.WriteLine("Renaming " + lobby.Name + " to " + newName + ".");
                            await lobby.ModifyAsync(prop => prop.Name = newName);
                        }
                    }
                }
            }
        }

        public async Task ResidenceInit()
        {
            await Access.WaitAsync();

            List<Discord.WebSocket.SocketVoiceChannel> otherGameLobbies = GetLobbies(this.ResidentGuild).ToList();
            Console.WriteLine("Other game channel ids:");
            foreach (var lobby in otherGameLobbies)
            {
                Console.WriteLine("Lobby found with name " + lobby.Name + " and id " + lobby.Id + ".");
                otherGames.Add(lobby.Name);
                otherGameLobbyIds.Add(lobby.Id);
            }


            Access.Release();
            initComplete = true;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        private void FetchGroupIDs()
        {
            groupNameToID.Clear();
            foreach (string name in Settings.LoudMetalRoles)
            {
                SocketRole role = this.ResidentGuild.Roles.First(x => x.Name == name);
                if (role != null)
                {
                    groupNameToID.Add(name, role.Id);
                }
            }

            foreach (string name in Settings.LoudDigitRoles)
            {
                SocketRole role = this.ResidentGuild.Roles.First(x => x.Name == name);
                if (role != null)
                {
                    groupNameToID.Add(name, role.Id);
                }
            }
        }

        private async Task RoleHighlightingFilter(SocketMessage rawmsg)
        {
            var message = rawmsg as SocketUserMessage;
            if (message is null || message.Author.IsBot)
            {
                return; // Ignore all bot messages and empty messages.
            }
            if (message.Channel.Name != Settings.roleHighlightChannel)
            {
                return; // Ignore all channels except the allowed channel.
            }

            List<string> rolesToHighlight = highlighter.RolesToHighlight(rawmsg.Content);

            if (rolesToHighlight.Count == 0)
            {
                return;
            }

            StringBuilder taggedRoles = new StringBuilder();
            bool first = true;
            foreach (string role in rolesToHighlight)
            {
                if(first)
                {
                    first = false;
                }
                else
                {
                    taggedRoles.Append(" ");
                }
                taggedRoles.Append("<@&");
                taggedRoles.Append(groupNameToID[role]);
                taggedRoles.Append(">");
            }

            SocketTextChannel responseChannel = (SocketTextChannel)message.Channel;
            await responseChannel.SendMessageAsync(taggedRoles.ToString());
            return;
        }
        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message is null || message.Author.IsBot) return;
            if (!Settings.BotChannels.Contains(message.Channel.Name)) return; // Ignore all channels except the allowed channel.
            int argPos = 0;

            if (message.HasStringPrefix("!", ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(client, message);
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
            }
            await client.SetGameAsync(Settings.get_botStatus());
        }


        public async Task RunBotAsync()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
            client.Log += Log;
            client.Ready += () =>
            {
                this.ResidentGuild = client.GetGuild(Settings.residenceID);
                Console.WriteLine("Setting up residence in Discord guild " + this.ResidentGuild.Name);
                FetchGroupIDs();
                return Task.CompletedTask;
            };

            client.MessageReceived += RoleHighlightingFilter;

            await RegisterCommandsAsync();
            await client.LoginAsync(Discord.TokenType.Bot, Secrets.botToken);
            await client.StartAsync();
            await client.SetGameAsync(Settings.get_botStatus());
            while (true)
            {
                // do periodic tasks
                await UpdateNames();
                await Task.Delay(Settings.RepeatPeriod);
            }
            // await Task.Delay();
        }

        static async Task Main(string[] args)
        {
            // Test 1

            // string tester = "Lopata_6";
            // string testerID = await R6Tab.GetTabID(tester);
            // Console.WriteLine(tester + "'s ID:" + testerID);
            // R6TabDataSnippet snippet = await R6Tab.GetData(testerID);
            // Rank r = snippet.ToRank();
            // Console.WriteLine(tester + "'s rank:" + r.FullPrint());
            // Test 2

            // List<string> darthList = new List<string> {"@everyone", "G", "Raptoil", "Stamgast", "Gold 2", "Gold", "G2" };
            // Rank guessDarth = Ranking.GuessRank(darthList);
            // if (!guessDarth.Equals(new Rank(Metal.Gold, 2)))
            // {
            //     Console.WriteLine("Sanity check failed. Darth's guess is" + guessDarth.FullPrint());
            //     throw new Exception();
            // }
            await new Bot().RunBotAsync();
        }
    }
}
