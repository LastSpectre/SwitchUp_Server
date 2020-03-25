using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace BA_Praxis_Library
{
    [ProtoContract]
    public class Resources
    {
        // ID for database
        [ProtoMember(101)]
        public int ID;

        // ID of user
        [ProtoMember(102)]
        public int UserID;

        // number of steps the player has taken
        [ProtoMember(103)]
        public int StepCounter;

        // number of nature material the player owns
        [ProtoMember(104)]
        public int NatureMaterial;

        // number of dark material the player owns
        [ProtoMember(105)]
        public int DarkMaterial;

        // number of ancient material the player owns
        [ProtoMember(106)]
        public int AncientMaterial;

        // number of neutral material the player owns
        [ProtoMember(107)]
        public int NeutralMaterial;

        // number of chaos material the player owns
        [ProtoMember(108)]
        public int ChaosMaterial;
    }
}
