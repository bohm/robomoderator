using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Events
{
    class CoreData
    {
        public ulong MessageWithButtons;
        public ulong MessageWithSignups;
        public List<List<ulong>> SignUpLists;
        public List<bool> eventHappening;

        public void FillGaps()
        {
            if (SignUpLists == null)
            {
                SignUpLists = new List<List<ulong>>();
            }

            if (eventHappening == null)
            {
                eventHappening = new List<bool> { true, true, true, true, true, true, true };
            }

            while (SignUpLists.Count < 7)
            {
                SignUpLists.Add(new List<ulong>());
            }
        }

        public void Clear()
        {
            foreach (var signup in SignUpLists)
            {
                signup.Clear();
            }

            for (int i = 0; i < 7; i++)
            {
                eventHappening[i] = true;
            }

            MessageWithButtons = 0;
            MessageWithSignups = 0;
        }
    }
}
