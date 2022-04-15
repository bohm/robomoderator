using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RoboModerator
{

    class SingleGuildConfig
    {
        public ulong id;
        public string reportChannel;
        public List<string> commandChannels;
        public List<string> roleHighlightChannels;
    }

    class BackupGuildConfiguration
    {
        public List<SingleGuildConfig> guildList = new List<SingleGuildConfig>();
    }
}
