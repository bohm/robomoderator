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

        public static BackupGuildConfiguration RestoreFromFile(string fileName)
        {
            BackupGuildConfiguration ret = null;
            if (File.Exists(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                StreamReader fileStream = File.OpenText(fileName);
                JsonTextReader file = new JsonTextReader(fileStream);
                ret = (BackupGuildConfiguration)serializer.Deserialize(file, typeof(BackupGuildConfiguration));
                file.Close();
            }

            return ret;
        }
        public static BackupGuildConfiguration RestoreFromString(string content)
        {
            BackupGuildConfiguration ret = null;
            TextReader stringr = new StringReader(content);
            JsonSerializer serializer = new JsonSerializer();
            ret = (BackupGuildConfiguration)serializer.Deserialize(stringr, typeof(BackupGuildConfiguration));
            return ret;
        }
    }
}
