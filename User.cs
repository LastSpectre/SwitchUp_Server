using ProtoBuf;

namespace BA_Praxis_Library
{
    [ProtoContract]
    public class User
    {
        // ID for database
        [ProtoMember(101)]
        public int ID;

        [ProtoMember(102)]
        // name of player
        public string Name;

        // password of player
        [ProtoMember(103)]
        public string Password;
    }
}