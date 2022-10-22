using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboModerator.RoomManager
{

    class Room
    {
        public ulong RoomId = 0;
        public ulong GuildId;
        public ulong Creator;
        public ulong Category;
        public string CreatorGivenName;
        public string MandatoryPrefix = "| ";
        public DateTime RoomCreationTime;

        public Room(ulong guildId, ulong creator, ulong category, string creatorGivenName, DateTime creationTime)
        {
            GuildId = guildId;
            Creator = creator;
            Category = category;
            CreatorGivenName = creatorGivenName;
            RoomCreationTime = creationTime;
        }

        public void SetRoomId(ulong Id)
        {
            RoomId = Id;
        }

    }

    class RoomList
    {
        public List<Room> AllRooms;
    }

    class Registration
    {
        private BotProperties _bot;
        private BackupSystem<RoomList> _backupSystem;
        private RoomList rl;
        public Registration(BotProperties bp)
        {
            _bot = bp;
        }

        public async Task LoadBackup()
        {
            _backupSystem = new BackupSystem<RoomList>(_bot.Primary, "robomod-room-registration", "robomod-room-registration.json");

            // If we are doing a first run ever, we only initialize the structures.
            if (Settings.RoomReservationFirstRun)
            {
                rl = new RoomList();
                rl.AllRooms = new List<Room>();
            }
            else
            {
                rl = await _backupSystem.RecoverAsync();
            }
        }

        public async Task CreateNewRoom(DiscordGuild dg, SocketGuildUser creator, ulong RoomCategory, string creatorGivenName = "")
        {
            Room newRoom = new Room(dg.Id, creator.Id, RoomCategory, creatorGivenName, DateTime.Now);

            var createdChannel = await dg._socket.CreateVoiceChannelAsync(newRoom.MandatoryPrefix + creatorGivenName,
                                                         prop => prop.CategoryId = newRoom.Category);
            newRoom.SetRoomId(createdChannel.Id);
            rl.AllRooms.Add(newRoom);
            await _backupSystem.BackupAsync(rl);
        }

        public async Task RoomMaintenance()
        {
            List<Room> deletionQueue = new List<Room>();

            foreach (Room r in rl.AllRooms)
            {
                DiscordGuild relevantGuild = Bot.Instance.BotProps.Guilds.byId[r.GuildId];
                if (!relevantGuild._socket.VoiceChannels.Any(x => x.Id == r.RoomId))
                {
                    Console.WriteLine($"Room maintenance: Room {r.CreatorGivenName} with id {r.RoomId} not found and thus deleting from the system.");
                    deletionQueue.Add(r);
                } else
                {

                }
            }
        }
    }

}
