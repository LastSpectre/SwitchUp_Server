using ProtoBuf;

namespace BA_Praxis_Library
{
    [ProtoContract]
    public class LevelStatus
    {
        // ID of LevelStatus in data
        [ProtoMember(101)]
        public int ID;

        // Level ID INGAME
        [ProtoMember(102)]
        public int LevelID;

        // ID of user that this level status belongs to
        [ProtoMember(103)]
        public int UserID;

        // UnlockStatus of the level of UserID
        [ProtoMember(104)]
        public int UnlockStatus;
    }
}
