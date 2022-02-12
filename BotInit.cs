using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator
{
    partial class Bot
    {
        public async Task<BackupGuildConfiguration> RestoreGuildConfigurationAsync()
        {
            // First, check that the primary guild is already loaded.
            if (!_primaryServerLoaded)
            {
                throw new PrimaryGuildException("Primary guild (Discord server) did not load and yet RestoreGuildConfiguration() is called.");
            }

            BackupSystem<BackupGuildConfiguration> configRecovery = new BackupSystem<BackupGuildConfiguration>(_primary,
                Settings.PrimaryConfigurationChannel, Settings.PrimaryConfigurationFile);

            BackupGuildConfiguration gc = await configRecovery.RecoverAsync();
            return gc;
        }
    }
}
