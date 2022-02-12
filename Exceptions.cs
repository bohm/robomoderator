using System;
using System.Collections.Generic;
using System.Text;

namespace RoboModerator
{
    public class PrimaryGuildException : Exception
    {
        public PrimaryGuildException()
        {
        }

        public PrimaryGuildException(string message)
            : base(message)
        {
        }

        public PrimaryGuildException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class GuildStructureException : Exception
    {
        public GuildStructureException()
        {
        }

        public GuildStructureException(string message)
            : base(message)
        {
        }

        public GuildStructureException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class GuildListException : Exception
    {
        public GuildListException()
        {
        }

        public GuildListException(string message)
            : base(message)
        {
        }

        public GuildListException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class RoleSyncException : Exception
    {
        public RoleSyncException()
        {
        }

        public RoleSyncException(string message)
            : base(message)
        {
        }

        public RoleSyncException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class BackupException : Exception
    {
        public BackupException()
        {
        }

        public BackupException(string message)
            : base(message)
        {
        }

        public BackupException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
