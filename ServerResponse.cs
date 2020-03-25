using ProtoBuf;
using System.Collections.Generic;

namespace BA_Praxis_Library
{
    [ProtoContract]
    public class ServerResponse
    {
        [ProtoMember(101)]
        // shows if request was successful
        public EServerResponseType ResponseType;

        // message the server sent with response
        [ProtoMember(102)]
        public string Text;

        // list of users requested
        [ProtoMember(103)]
        public List<User> Users;

        // list of chars requested
        [ProtoMember(104)]
        public List<Character> Characters;

        // LevelData requested | enemies etc.
        [ProtoMember(105)]
        public LevelData LevelData;

        // LevelUnlockStatus requested
        [ProtoMember(106)]
        public LevelStatus LevelStatus;

        [ProtoMember(107)]
        public Resources Resources;
    }
}