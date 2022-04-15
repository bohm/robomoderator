using System;
using System.Collections.Generic;
using System.Text;

namespace RoboModerator
{
    class SingleGuildEventData
    {
        public ulong GuildId;
        public string GuildName; // Currently used only for logging, but it is okay to keep.
        public string GuildAnnounceChannel;
        public ulong MessageWithButtons;
        public ulong MessageWithSignups;
        public List<List<ulong>> SignUpLists;

        public void FillGaps()
        {
            if (SignUpLists == null)
            {
                SignUpLists = new List<List<ulong>>();
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

            MessageWithButtons = 0;
            MessageWithSignups = 0;
        }
    }


    class OrganizerCoreData
    {
        public List<SingleGuildEventData> DataList;
    }

    class SingleGuildSecondaryData
    {
        public SingleGuildEventData PrimaryData;
        public List<HashSet<ulong>> UserQuery;
        public SingleGuildSecondaryData(SingleGuildEventData sged)
        {
            PrimaryData = sged;
            UserQuery = new List<HashSet<ulong>>();
            foreach (var weeklyList in PrimaryData.SignUpLists)
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

    /// <summary>
    /// A list of secondary data objects. Note: This class is not as important
    /// as OrganizerCoreData -- OrganizerCoreData is stored in a backup, whereas
    /// OrganizerSecondaryData is always recomputed from the backup. In this sense,
    /// OrganizerSecondaryData can be integrated into OrganizerProperties, but it is kept
    /// to maintain the symmetry.
    /// </summary>
    class OrganizerSecondaryData
    {
        public List<SingleGuildSecondaryData> SecondaryDataList;

        public OrganizerSecondaryData()
        {
            SecondaryDataList = new List<SingleGuildSecondaryData>();
        }
    }

    class OrganizerProperties
    {
        public OrganizerCoreData Core;
        public OrganizerSecondaryData Secondary;

        public Dictionary<string, SingleGuildEventData> PrimaryByName;
        public Dictionary<ulong, SingleGuildEventData> PrimaryById;
        public Dictionary<string, SingleGuildSecondaryData> SecondaryByName;
        public Dictionary<ulong, SingleGuildSecondaryData> SecondaryById;


        public OrganizerProperties(OrganizerCoreData c)
        {
            Core = c;
            Secondary = new OrganizerSecondaryData();
            foreach (var singleGuildCore in Core.DataList)
            {
                SingleGuildSecondaryData singleGuildSecondary = new SingleGuildSecondaryData(singleGuildCore);

                Secondary.SecondaryDataList.Add(singleGuildSecondary);
                PrimaryById.Add(singleGuildCore.GuildId, singleGuildCore);
                PrimaryByName.Add(singleGuildCore.GuildName, singleGuildCore);
                SecondaryById.Add(singleGuildCore.GuildId, singleGuildSecondary);
                SecondaryByName.Add(singleGuildCore.GuildName, singleGuildSecondary);
            }
        }
    }

}
