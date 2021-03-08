using System;
using System.Collections.Generic;
using System.Text;

namespace domovoj
{
    class Settings
    {

        // IDs of: DoctorOrson.
        public static readonly ulong[] Operators = { 428263908281942038 };
        private static string botStatus = "Zavolejte mne pomoci !domovoj.";
        private static string logFolder = null;
        public static string otherGameLobbyPrefix = "Jiná hra";

        public static readonly string[] BotChannels = { "rank-bot", "rank-bot-admin" }; // The only channels the bot is operating in.
        public static readonly TimeSpan RepeatPeriod = TimeSpan.FromSeconds(60);

        public static string get_botStatus()
        {
            return botStatus;
        }

        public static string get_logFolder()
        {
            return logFolder;
        }

    }
}
