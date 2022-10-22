using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator
{
    [Serializable]
    public class BackupException : Exception
    {
        public BackupException()
        {
        }

        public BackupException(string message) : base(message)
        {
        }

        public BackupException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BackupException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
