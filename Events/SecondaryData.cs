using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.Events
{
    class SecondaryData
    {
        public List<HashSet<ulong>> UserQuery;

        public SecondaryData(CoreData cd)
        {
            UserQuery = new List<HashSet<ulong>>();
            foreach (var weeklyList in cd.SignUpLists)
            {
                HashSet<ulong> userSet = new HashSet<ulong>();
                foreach (var user in weeklyList)
                {
                    userSet.Add(user);
                }
                UserQuery.Add(userSet);
            }
        }

        public void Clear()
        {
            foreach (var week in UserQuery)
            {
                week.Clear();
            }
        }
    }
}
