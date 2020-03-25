using ProtoBuf;

namespace BA_Praxis_Library
{
    [ProtoContract]
    public class UserRequest
    {
        #region --- Requesting ---
        // RequestType
        [ProtoMember(101)]
        public EUserRequestType RequestType;

        // Username of requesting player
        [ProtoMember(102)]
        public string Username;

        // password of requesting player
        [ProtoMember(103)]
        public string Password;
        #endregion

        #region --- Requested ---
        // ID of element requested
        [ProtoMember(104)]
        public int PayloadID;

        // Requested user
        [ProtoMember(105)]
        public User PayloadUser;

        // Requested character
        [ProtoMember(106)]
        public Character PayloadChar;

        // Requested LevelData (How is it build up: enemies etc.)
        [ProtoMember(107)]
        public LevelData PayloadLevelData;

        // Requested Level UnlockStatus
        [ProtoMember(108)]
        public LevelStatus PayloadLevelStatus;

        // requested Resources
        [ProtoMember(109)]
        public Resources PayloadResources;
        #endregion
    }
}