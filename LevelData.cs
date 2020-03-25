using ProtoBuf;
using System.Collections.Generic;

namespace BA_Praxis_Library
{
    // this class contains the information about how the level is build up
    // : which enemies are in the level and what stats do they have

    [ProtoContract]
    public class LevelData
    {
        // ID of LevelData IN DATABASE
        [ProtoMember(101)]
        public int ID;

        // enemies in Level
        [ProtoMember(102)]
        public List<Character> Enemies;

        [ProtoMember(103)]
        // ID of Level INGAME
        public int LevelID;
    }
}