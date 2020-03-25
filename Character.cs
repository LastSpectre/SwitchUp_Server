using ProtoBuf;

namespace BA_Praxis_Library
{
    [ProtoContract]
    public class Character
    {
        // ID for database
        [ProtoMember(101)]
        public int ID;

        // name of character
        [ProtoMember(102)]
        public string Name;

        // current level of character
        [ProtoMember(103)]
        public int Level;

        // shows if player already unlocked this character
        [ProtoMember(104)]
        public int UnlockStatus;

        // shows user that owns this character
        [ProtoMember(105)]
        public int User;

        [ProtoMember(106)]
        public int AttackDamage;

        [ProtoMember(107)]
        public int Defense;

        [ProtoMember(108)]
        public int HP;

        [ProtoMember(109)]
        public int Experience;

        // basic constructor
        public Character()
        {

        }

        // copy constructor
        public Character(Character _copy)
        {
            // copy character
        }
    }
}
