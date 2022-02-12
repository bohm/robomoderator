using System;
using System.Collections.Generic;
using System.Text;

namespace RoboModerator
{
    class Settings
    {
        // ID of the guild (Discord server) that this instance operates on.
        // For the main use of this bot, this is the ID of Discord server R6 Siege a Chill, a CZ/SK Discord server.
        public static ulong residenceID = 620608384227606528;

        public static readonly ulong ControlGuild = 903649099541270528;
        public const string PrimaryConfigurationChannel = "primary-configuration";
        // If the primary configuration channel is empty, the following file is read instead.
        public const string PrimaryConfigurationFile = @"primary.json";

        // IDs of: DoctorOrson.
        public static readonly ulong[] Operators = { 428263908281942038 };
        public static string botStatus = "Zavolejte mne pomoci !domovoj.";
        public static string otherGameLobbyPrefix = "Jiná hra";

        public static readonly string[] BotChannels = { "rank-bot", "🦾rank-bot", "rank-bot-admin" }; // The only channels the bot is operating in.
        // public static readonly string roleHighlightChannel = "rank-bot-admin"; // TODO: switch this to "hledame-spoluhrace" once we are ready.
        public static readonly string[] roleHighlightChannels = { "🔍hledám-spoluhráče", "hledame-testing" };
        public static readonly string searchChannelNG = "hledame-testing";
        public static readonly TimeSpan RepeatPeriod = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan RateRestPeriod = TimeSpan.FromSeconds(1);

        public static string get_botStatus()
        {
            return botStatus;
        }

        public static readonly string[] LoudMetalRoles =
{
            "Rankless", "Copper", "Bronze", "Silver", "Gold", "Plat", "Dia", "Champ"
        };

        public static readonly string[] LoudDigitRoles =
        {
                            "Copper 5", "Copper 4","Copper 3","Copper 2", "Copper 1",
                            "Bronze 5", "Bronze 4", "Bronze 3", "Bronze 2", "Bronze 1",
                            "Silver 5", "Silver 4", "Silver 3", "Silver 2", "Silver 1",
                            "Gold 3", "Gold 2", "Gold 1",
                            "Plat 3", "Plat 2", "Plat 1",
                            "Dia 3", "Dia 2", "Dia 1"
        };

        // Global variables that do not need to be edited.
        public static List<string> UpperCaseLoudDigitRoles;
        public static List<string> UpperCaseLoudMetalRoles;
        public static List<string> LowerCaseLoudDigitRoles; // Needs to be initialized from LoudDigitRoles.
        public static List<string> LowerCaseLoudMetalRoles; // Needs to be initialized from LoudMetalRoles.

    }
}
