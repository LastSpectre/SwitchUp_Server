namespace BA_Praxis_Library
{
    public enum EUserRequestType
    {
        NONE = 0,

        // get stats of a character
        CHAR_GET = 1,

        // upgrade a character
        CHAR_UPGRADE = 2,

        // get level data
        LEVEL_DATA = 3,

        // create new user
        USER_CREATE = 4,

        // log the user in
        USER_LOGIN = 5,

        // unlock a character
        CHAR_UNLOCK = 6,

        // check unlock status of level
        LEVEL_STATUS = 7,

        // unlock level for user
        LEVEL_UNLOCK = 8,

        // gets current resources of player
        GET_RESOURCES = 9,

        // updates the resources of player
        UPDATE_RESOURCES = 10,

        // add experience to the char
        CHAR_EXP_GAIN = 11
    }
}