﻿using Discord;
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
using Discord.Net;
using Newtonsoft.Json;

namespace RoboModerator
{
    /// <summary>
    /// Properties of Bot which are detached so that they can be passed to various subsystems/plugins.
    /// The subsystems can for example communicate with Discord guilds without accessing the Bot object itself.
    /// </summary>
    class BotProperties
    {
        public DiscordGuilds Guilds;
        public PrimaryDiscordGuild Primary;

        public BotProperties(PrimaryDiscordGuild p)
        {
            Primary = p;
        }

        public BotProperties(PrimaryDiscordGuild p, DiscordGuilds g)
        {
            Guilds = g;
        }
    }

    partial class Bot
    {
        public static Bot Instance;
        // The guild we are operating on. We start doing things only once this is no longer null.
        // public Discord.WebSocket.SocketGuild ResidentGuild;

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private Dictionary<string, ulong> groupNameToID;
        private bool initComplete = false;
        // private DiscordGuilds _guilds;
        private ButtonHandler _bh;
        private EventOrganizer _orga;
        private BotProperties _p;

        private PrimaryDiscordGuild _primary;
        private DiscordGuilds _guilds;

        private bool _primaryServerLoaded = false;

        public SocketGuild ResidentGuild;

        HashSet<ulong> _highlightedToday;
        DateTime _highlightSetDate;

        private readonly SemaphoreSlim Access;

        private List<ulong> otherGameLobbyIds;
        // private List<Discord.WebSocket.SocketVoiceChannel> otherGameLobbies;
        private List<string> otherGames;
        public static bool IsOperator(ulong id)
        {
            return (Settings.Operators.Contains(id));
        }
        public Bot()
        {
            groupNameToID = new Dictionary<string, ulong>();
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

        public void CheckDateRollover()
        {
            if (this._highlightSetDate != null && this._highlightedToday != null)
            {
                if (DateTime.Now.Date > this._highlightSetDate.Date)
                {
                    this._highlightedToday.Clear();
                    this._highlightSetDate = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Returns a Discord user object based on the name provided. Returns the first result. 
        /// </summary>
        /// <param name="discordNick"></param>
        /// <returns></returns>
        public SocketGuildUser UserByName(string discordNick)
        {
            return this.ResidentGuild.Users.FirstOrDefault(x => ((x.Username == discordNick) || (x.Nickname == discordNick)));
        }

        public SocketRole RoleByName(string roleName)
        {
            return this.ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
        }

        public async Task ManualClear(Discord.WebSocket.ISocketMessageChannel invokedChannel)
        {
            Discord.WebSocket.SocketTextChannel channelToClear = this.ResidentGuild.TextChannels.FirstOrDefault(x => x.Name == Settings.searchChannelNG);
            if (channelToClear == null)
            {
                await invokedChannel.SendMessageAsync("Channel to be cleared was not found.");
                return;
            }
            var asyncMessageList = channelToClear.GetMessagesAsync(int.MaxValue);
            int sleepCounter = 0;

            Console.WriteLine("Iterating through the list.");
            await foreach (var readonlycollection in asyncMessageList)
            {
                foreach(var msg in readonlycollection)
                {
                    Console.WriteLine("Found another message");
                    // Manual rate limit. It is unclear from the documentation if this is necessary.
                    sleepCounter++;
                    if (sleepCounter >= 100)
                    {
                        await Task.Delay(Settings.RateRestPeriod);
                        sleepCounter = 0;
                    }

                    if (msg.Content.StartsWith("Perma:") || msg.Content.StartsWith("perma:"))
                    {

                        // Console.WriteLine($"The message {msg.Content} will be kept.");
                        continue;
                    }
                    else
                    {
                        // Console.WriteLine($"The message {msg.Content} will be deleted.");
                        // ulong msgId = msg.Id;
                        await channelToClear.DeleteMessageAsync(msg);
                    }
                }
            }
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

        public async Task DelayedUserPing(Discord.WebSocket.ISocketMessageChannel chan, Discord.WebSocket.SocketGuildUser user)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            ulong id = user.Id;
            StringBuilder msg = new StringBuilder();
            msg.Append("Pinging ");
            msg.Append("<@");
            msg.Append(id);
            msg.Append(">");
            msg.Append(" .");
            await chan.SendMessageAsync(msg.ToString());
        }

        public async Task DelayedRolePing(Discord.WebSocket.ISocketMessageChannel chan, Discord.WebSocket.SocketRole role)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            StringBuilder msg = new StringBuilder();
            msg.Append("Pinging ");
            msg.Append(role.Mention);
            msg.Append(" .");
            await chan.SendMessageAsync(msg.ToString());
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


        public async Task BuildSlashCommandsAsync(SocketGuild guild)
        {
            SlashCommandBuilder guildCommand = new SlashCommandBuilder();
            guildCommand.WithName("joined-at");
            guildCommand.WithDescription("Shows when a particular user joined the Discord guild.");
            guildCommand.AddOption("user", ApplicationCommandOptionType.User, "The user to be queried.");

            try
            {
                await client.Rest.CreateGuildCommand(guildCommand.Build(), guild.Id);
            }
            catch(HttpException e)
            {
                Console.WriteLine($"RoboModerator: Guild command build error {e.HttpCode}:  {e.Message}");
            }
           
        }

        private async Task JoinedAtAsync(SocketSlashCommand command)
        {
            if (command.Data.Options.Count > 1)
            {
                await command.RespondAsync($"Too many options, this command only takes one.");
                return;
            } else if (command.Data.Options.Count == 0)
            {
                await command.RespondAsync($"Too few options, this command only takes one.");
                return;
            }

            if (command.Data.Options.First().Type != ApplicationCommandOptionType.User)
            {
                await command.RespondAsync($"The first parameter has a wrong type!");
                return;

            }

            var user = command.Data.Options.First().Value as SocketGuildUser;

            if (user == null)
            {
                await command.RespondAsync("Joinedat(): Error during type conversion");
                return;
            }

            string discordUsername = user.Username;

            if (ResidentGuild != null)
            {
                var matchedUsers = ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

                if (matchedUsers.Count() == 0)
                {
                    await command.RespondAsync($"There is no user matching the Discord nickname {discordUsername}.");
                    return;
                }

                if (matchedUsers.Count() > 1)
                {
                    await command.RespondAsync($"Two or more users have the same matching Discord nickname." +
                        $"This command cannot continue.");
                    return;
                }

                Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();

                await command.RespondAsync($"The user with the nickname {discordUsername} joined at {rightUser.JoinedAt}");
            }
        }

        private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
        {
            if(command.CommandName == "joined-at")
            {
                await JoinedAtAsync(command);
            }

            if(command.CommandName == "customs-new-week")
            {
                await _orga.CustomsNewWeekAsync(command);
            }

            if (command.CommandName == "customs-refresh-signup")
            {
                await _orga.RefreshSignupCommandAsync(command);
            }

            if (command.CommandName == "customs-remove-person")
            {
                await _orga.CustomsRemovePersonAsync(command);
            }
        }

        public async Task ClientReadyAsync()
        {
            this._highlightSetDate = DateTime.Now;
            this._highlightedToday = new HashSet<ulong>();
            this.ResidentGuild = client.GetGuild(Settings.residenceID);

            // Connect to the primary configuration server and initialize BotProperties -- with a null guild list, currently.
            _p = new BotProperties(new PrimaryDiscordGuild(client));

            BackupGuildConfiguration gc = await RestoreGuildConfigurationAsync();

            // Do only once:

            DiscordGuilds g = new DiscordGuilds(gc, client);
            DiscordGuild resGuild = g.byId[Settings.residenceID]; // Hardcoded for now.

            // g.Add(resGuild);

            _p.Guilds = g; // Insert guilds into BotProperties -- they should be good to go by this line.
            _orga = new EventOrganizer(_p);
            await _orga.InitializeAsync();
            // Run only once.

            if (Settings.GenerateSlashCommands)
            {
                await BuildSlashCommandsAsync(this.ResidentGuild);
                await _orga.GenerateGuildCommandsAsync(client);
            }

            // await resGuild.GiveEveryoneARoleAsync("Chill Veterán");

            _bh = new ButtonHandler(_p);

            // await TestingAsync();

            client.ButtonExecuted += _bh.ButtonHandlerAsync;
            client.ButtonExecuted += _orga.EventButtonHandlerAsync;

        }

        public async Task RunBotAsync()
        {
            var config = new DiscordSocketConfig();
            config.GatewayIntents = GatewayIntents.All;

            client = new DiscordSocketClient(config);
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
            client.Log += Log;
            client.Ready += ClientReadyAsync;
            client.SlashCommandExecuted += SlashCommandHandlerAsync;

            await RegisterCommandsAsync();
            await client.LoginAsync(Discord.TokenType.Bot, Secrets.botToken);
            await client.StartAsync();
            await client.SetGameAsync(Settings.get_botStatus());
            while (true)
            {
                // do periodic tasks
                CheckDateRollover();
                await Task.Delay(Settings.RepeatPeriod);

            }
            // await Task.Delay();
        }

        static async Task Main(string[] args)
        {
            Settings.UpperCaseLoudDigitRoles = new List<string>(Settings.LoudDigitRoles);
            Settings.UpperCaseLoudMetalRoles = new List<string>(Settings.LoudMetalRoles);


            // Test 1
            // RoleHighlighting rh = new RoleHighlighting();
            // var testMatches = Regex.Matches("Nekdo hru Plat 2 gold Silver? copper 3, Champion Bronzek Dia", rh.RegexMatcher);
            // foreach (Match m in testMatches)
            // {
            //     string canonicalForm = RoleHighlighting.FirstToUppercase(m.Groups[1].Value);
            //      Console.WriteLine($"Canonical form of match is: {canonicalForm}");
            // }

            await new Bot().RunBotAsync();
        }


        /// <summary>
        /// Some testing in order to make sure the bot can change roles.
        /// </summary>
        /// <returns></returns>
        public async Task TestingAsync()
        {
            int totalUsers = ResidentGuild.Users.Count();
            Console.WriteLine($"The total number of users is {totalUsers}");

            var docOrson = ResidentGuild.Users.FirstOrDefault(x => x.Username == "DoctorOrson");

            if (docOrson == null)
            {
                Console.WriteLine("Something is wrong, there is no doctor Orson.");
                throw new Exception();
            }

            var secondDocOrson =  _p.Guilds.byId[ResidentGuild.Id].GetSingleUser(docOrson.Id);

            if (secondDocOrson == null)
            {
                Console.WriteLine("Something is wrong, there is no doctor Orson via custom guild API.");
                throw new Exception();
            }
        }
    }
}
