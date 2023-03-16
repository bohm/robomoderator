using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Events
{
    public class TourneyData
    {
        public ulong MessageWithButtons;
        public ulong MessageWithSignups;
        public List<ulong> SignUpList;

        public TourneyData()
        {
            MessageWithButtons = 0;
            MessageWithSignups = 0;
            SignUpList = new List<ulong>();
        }
        public void Clear()
        {
            MessageWithButtons = 0;
            MessageWithSignups = 0;
            SignUpList.Clear();
        }
    }
}
