
using MySql.Data.MySqlClient;
using ProtoBuf;
using System;
using System.Net;
using System.Net.Sockets;
using BA_Praxis_Library;

namespace BA_Praxis_Server
{
    public class Server
    {
        // connection for MariaDatabase
        public string DatabaseConnection => $"SERVER={"127.0.0.1"};DATABASE={"ba_praxis"};UID={"root"};PASSWORD={""};";

        public void Start(IPAddress _ip, int _port)
        {
            // using tcp listener to start server
            TcpListener listener = new TcpListener(_ip, _port);

            // start listening
            listener.Start();

            /* create client | no client is going to stay connected
                             | everytime the client wants to access data, he establishes a new connection */
            TcpClient client;

            // NetworkStream to access clients data
            NetworkStream networkStream;

            // contains players request
            UserRequest userRequest;

            // server response for client
            ServerResponse serverResponse;

            // print out server start
            Console.WriteLine($"Server started. Listening on {_ip}:{_port}.");

            // while server is running
            while (true)
            {
                // accept client
                client = listener.AcceptTcpClient();

                // get client stream
                networkStream = client.GetStream();

                // get request from client
                userRequest = Serializer.DeserializeWithLengthPrefix<UserRequest>(networkStream, PrefixStyle.Fixed32);

                // server does different things depending on request
                // if client:
                switch (userRequest.RequestType)
                {
                    // didnt request anything
                    case EUserRequestType.NONE:
                        serverResponse = new ServerResponse() { ResponseType = EServerResponseType.NONE, Text = "Nothing was requested." };
                        break;

                    // wants to get character data
                    case EUserRequestType.CHAR_GET:
                        serverResponse = GetCharacter(userRequest);
                        break;

                    // wants to upgrade character
                    case EUserRequestType.CHAR_UPGRADE:
                        serverResponse = UpgradeCharacter(userRequest);
                        break;

                    // wants to access level status
                    case EUserRequestType.LEVEL_STATUS:
                        serverResponse = GetLevelStatus(userRequest);
                        break;

                    case EUserRequestType.LEVEL_DATA:
                        serverResponse = GetLevelData(userRequest);
                        break;

                    case EUserRequestType.USER_CREATE:
                        serverResponse = CreateNewUser(userRequest);
                        break;

                    case EUserRequestType.USER_LOGIN:
                        serverResponse = LoginUser(userRequest);
                        break;

                    case EUserRequestType.CHAR_UNLOCK:
                        serverResponse = UnlockCharacterForUser(userRequest);
                        break;

                    case EUserRequestType.LEVEL_UNLOCK:
                        serverResponse = UnlockLevelForUser(userRequest);
                        break;

                    case EUserRequestType.UPDATE_RESOURCES:
                        serverResponse = UpdateMaterial(userRequest);
                        break;

                    case EUserRequestType.GET_RESOURCES:
                        serverResponse = GetMaterial(userRequest);
                        break;

                    case EUserRequestType.CHAR_EXP_GAIN:
                        serverResponse = AddCharacterEXP(userRequest);
                        break;

                    // default case
                    default:
                        serverResponse = new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Request type does not exist." };
                        break;
                }

                // Send answer to client | flush stream after
                Serializer.SerializeWithLengthPrefix(networkStream, serverResponse, PrefixStyle.Fixed32);
                networkStream.Flush();
            }
        }

        // creates a new user in the database and creates every unlockable character in the database
        ServerResponse CreateNewUser(UserRequest _userRequest)
        {
            // get user to create from request data
            User userToCreate = _userRequest.PayloadUser;

            // if no data was sent | error
            if (userToCreate == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, no data was sent." };
            }

            // check if name input is valid
            if (!(Helper.CheckForValidString(userToCreate.Name)))
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, name contains special characters." };
            }

            // check if password input is valid
            if (!(Helper.CheckForValidString(userToCreate.Password)))
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, password contains special characters." };
            }

            // if there is already a user with the chosen name return error
            if (CheckIfUserExists(_userRequest.PayloadUser, false))
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Username already exists." };
            }

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            // add new user to the database
            string cmdText = $"INSERT INTO `user` (`ID`, `Name`, `Password`) VALUES (NULL," +
                $" '{_userRequest.PayloadUser.Name}', '{_userRequest.PayloadUser.Password}');";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            // if adding failed return error
            if (rowsAffected == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, User couldn't be created." };
            }

            // safe ID from created user
            int IDtmp = GetUserID(_userRequest.PayloadUser.Name);

            CreateAllCharactersForNewUser(IDtmp);

            CreateAllLevelsForNewUser(IDtmp);

            CreateMaterialEntryForNewUser(IDtmp);

            // else create user with that name and password
            return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = $"User was successfully created." };
        }

        // checks if the player is registered in the database
        ServerResponse LoginUser(UserRequest _userRequest)
        {
            // if user doesnt exist, log error
            if (!CheckIfUserExists(new User() { Name = _userRequest.Username, Password = _userRequest.Password }, true))
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, login failed." };
            }
            else
            {
                return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = "Successfully logged in. Starting game." };
            }
        }

        // gets the character stats of the chosen character
        ServerResponse GetCharacter(UserRequest _userRequest)
        {
            if (_userRequest.PayloadChar == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, no character data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            Character characterToCreate = new Character();

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `Chars` WHERE `Name` = '{_userRequest.PayloadChar.Name}' AND `User` = '{userID}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                characterToCreate.AttackDamage = (int)reader["AttackDamage"];
                characterToCreate.Defense = (int)reader["Defense"];
                characterToCreate.HP = (int)reader["HP"];
                characterToCreate.Level = (int)reader["Level"];
                characterToCreate.UnlockStatus = (int)reader["UnlockStatus"];
                characterToCreate.Experience = (int)reader["Experience"];
            }

            con.Close();

            return new ServerResponse()
            {
                ResponseType = EServerResponseType.OK,
                Text = $"Character data was successfully received.",
                Characters = new System.Collections.Generic.List<Character>()
                {
                    characterToCreate
                }
            };
        }

        // upgrades a character to the next level
        ServerResponse UpgradeCharacter(UserRequest _userRequest)
        {
            if (_userRequest.PayloadChar == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, no character data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            // get player resources from database
            Resources playerResources = new Resources();

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `resources` WHERE `UserID` = '{userID}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                playerResources.AncientMaterial = (int)reader["AncientMaterial"];
                playerResources.ChaosMaterial = (int)reader["ChaosMaterial"];
                playerResources.DarkMaterial = (int)reader["DarkMaterial"];
                playerResources.NatureMaterial = (int)reader["NatureMaterial"];
                playerResources.NeutralMaterial = (int)reader["NeutralMaterial"];
                playerResources.StepCounter = (int)reader["StepCounter"];
            }

            con.Close();

            // check if character can be upgraded with the materials
            bool canCharBeUpgraded = false;

            Resources updatedResources = CheckCharacterForUpgrade(_userRequest.PayloadChar.Name, playerResources, userID, out canCharBeUpgraded);

            if (canCharBeUpgraded)
            {
                ServerResponse sr = GetCharacter(_userRequest);
                               
                // try upgrade character
                bool upgradeSucceeded = UpgradeCharForUser(userID, _userRequest.PayloadChar.Name, sr.Characters[0].Level);

                // if upgrade didnt succeed
                if (!upgradeSucceeded)
                {
                    return new ServerResponse()
                    {
                        ResponseType = EServerResponseType.ERROR,
                        Text = $"Character couldn't be upgraded."
                    };
                }
                // upgrade did succeed
                else
                {
                    // TODO: Remove material from player
                    UpdateMaterialFromUser(userID, updatedResources);
                    return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = $"Character was successfully upgraded." };
                }
            }

            return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Your character couldnt be upgraded" };
        }

        // Check if character can be upgraded
        Resources CheckCharacterForUpgrade(string _charName, Resources _resources, int _id, out bool _worked)
        {
            // updated resources
            Resources updatedResources = _resources;

            // character stats from database
            Character tmpCharacter = new Character();

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `Chars` WHERE `Name` = '{_charName}' AND `User` = '{_id}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            // get stats
            while (reader.Read())
            {
                tmpCharacter.AttackDamage = (int)reader["AttackDamage"];
                tmpCharacter.Defense = (int)reader["Defense"];
                tmpCharacter.Experience = (int)reader["Experience"];
                tmpCharacter.HP = (int)reader["HP"];
                tmpCharacter.Level = (int)reader["Level"];
                tmpCharacter.Name = (string)reader["Name"];
                tmpCharacter.UnlockStatus = (int)reader["UnlockStatus"];
                tmpCharacter.User = (int)reader["User"];
            }

            con.Close();

            // if character has a high enough experience
            if (IsExperienceHighEnough(tmpCharacter.Level, tmpCharacter.Experience))
            {
                // if the character is Adventurer
                if (tmpCharacter.Name == "Adventurer")
                {
                    // check if enough neutral material
                    if (_resources.NeutralMaterial >= tmpCharacter.Level)
                    {
                        updatedResources.NeutralMaterial -= tmpCharacter.Level;
                        _worked = true;
                        return updatedResources;
                    }
                    // not enough material;
                    else
                    {
                        _worked = false;
                        return updatedResources;
                    }
                }
                // if character has nature type
                else if (tmpCharacter.Name == "Wasp" || tmpCharacter.Name == "Mandrake" || tmpCharacter.Name == "Rat")
                {
                    // check for nature material
                    if (_resources.NatureMaterial >= tmpCharacter.Level)
                    {
                        updatedResources.NatureMaterial -= tmpCharacter.Level;
                        _worked = true;
                        return updatedResources;
                    }
                    else
                    {
                        _worked = false;
                        return updatedResources;
                    }
                }
                // if character has ancient typing
                else if (tmpCharacter.Name == "Yeti" || tmpCharacter.Name == "Golem" || tmpCharacter.Name == "Satyr")
                {
                    // check for ancient material
                    if (_resources.AncientMaterial >= tmpCharacter.Level)
                    {
                        _resources.AncientMaterial -= tmpCharacter.Level;
                        _worked = true;
                        return updatedResources;
                    }
                    else
                    {
                        _worked = false;
                        return updatedResources;
                    }
                }
                // if character has dark typing
                else if (tmpCharacter.Name == "Bandit" || tmpCharacter.Name == "Red Ogre" || tmpCharacter.Name == "Werewolf")
                {
                    // check for dark material
                    if (_resources.DarkMaterial >= tmpCharacter.Level)
                    {
                        updatedResources.DarkMaterial -= tmpCharacter.Level;
                        _worked = true;
                        return updatedResources;
                    }
                    else
                    {
                        _worked = false;
                        return updatedResources;
                    }
                }
                // if character has chaos typing
                else if (tmpCharacter.Name == "Shade")
                {
                    // check for chaos material
                    if (_resources.ChaosMaterial >= tmpCharacter.Level)
                    {
                        updatedResources.ChaosMaterial -= tmpCharacter.Level;
                        _worked = true;
                        return updatedResources;
                    }
                    else
                    {
                        _worked = false;
                        return updatedResources;
                    }
                }
            }
            // character couldnt be found
            _worked = false;
            return updatedResources;
        }

        // check if experience is high enough for upgrading
        bool IsExperienceHighEnough(int _level, int _exp)
        {
            if (_level == 1 && _exp >= 125)
            {
                return true;
            }
            else if (_level == 2 && _exp >= 250)
            {
                return true;
            }
            else if (_level == 3 && _exp >= 500)
            {
                return true;
            }
            else if (_level == 4 && _exp >= 1000)
            {
                return true;
            }

            return false;
        }

        // upgrades a character to the next level
        bool UpgradeCharForUser(int _id, string _charName, int _currentCharLevel)
        {
            // stats of next level
            Character newCharStats = new Character();

            // get stats of next level for character from database
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `charStats` WHERE `CharacterName` = '{_charName}' AND `Level` = '{_currentCharLevel + 1}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            // get stats
            while (reader.Read())
            {
                newCharStats.AttackDamage = (int)reader["Attack"];
                newCharStats.Defense = (int)reader["Defense"];
                newCharStats.HP = (int)reader["HP"];
                newCharStats.Level = (int)reader["Level"];
            }

            con.Close();

            // set stats from player character to next level
            con = new MySqlConnection(DatabaseConnection);
            con.Open();

            cmdText = $"UPDATE `Chars` SET `Level` = '{newCharStats.Level}', `AttackDamage` = '{newCharStats.AttackDamage}', `Defense` = '{newCharStats.Defense}', `HP` = '{newCharStats.HP}' WHERE `Chars`.`Name` = '{_charName}' AND `Chars`.`User` = '{_id}'";

            sqlCommand = new MySqlCommand(cmdText, con);

            int received = sqlCommand.ExecuteNonQuery();

            con.Close();

            // character couldnt be upgraded
            if (received == -1)
            {
                return false;
            }
            // character could be upgraded
            else
                return true;
        }

        // Updates the material in the database
        bool UpdateMaterialFromUser(int _id, Resources _resources)
        {
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"UPDATE `resources` SET `StepCounter` = '{_resources.StepCounter}', `NatureMaterial` = '{_resources.NatureMaterial}', `DarkMaterial` = '{_resources.DarkMaterial}', `AncientMaterial` = '{_resources.AncientMaterial}', `NeutralMaterial` = '{_resources.NeutralMaterial}', `ChaosMaterial` = '{_resources.ChaosMaterial}' WHERE `resources`.`UserID` = '{_id}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
            {
                return false;
            }
            else
                return true;
        }

        // Updates the material for the user
        ServerResponse UpdateMaterial(UserRequest _userRequest)
        {
            if(_userRequest.PayloadResources == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, no data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            bool updatedMaterials = UpdateMaterialFromUser(userID, _userRequest.PayloadResources);

            if(updatedMaterials)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = "Resources were successfully updated" };
            }
            else
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, resources couldnt be updated." };
            }
        }

        ServerResponse GetMaterial(UserRequest _userRequest)
        {
            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            // resources to return
            Resources res = new Resources();

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `resources` WHERE `UserID` = '{userID}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            while(reader.Read())
            {
                res.AncientMaterial = (int)reader["AncientMaterial"];
                res.ChaosMaterial = (int)reader["ChaosMaterial"];
                res.DarkMaterial = (int)reader["DarkMaterial"];
                res.NatureMaterial = (int)reader["NatureMaterial"];
                res.NeutralMaterial = (int)reader["NeutralMaterial"];
                res.StepCounter = (int)reader["StepCounter"];
            }

            con.Close();

            return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = "Resources Data was successfully gathered.", Resources = res};
        }

        // unlocks the chosen character in the database for the user who requested
        ServerResponse UnlockCharacterForUser(UserRequest _userRequest)
        {
            if (_userRequest.PayloadChar == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, no data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"UPDATE `Chars` SET `UnlockStatus` = '1' WHERE `User` = '{userID}' AND `Name` = '{_userRequest.PayloadChar.Name}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, character is already unlocked, or currently not available." };
            }

            return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = "Character has been unlocked." };
        }

        // adds experience to the requested character
        ServerResponse AddCharacterEXP(UserRequest _userRequest)
        {
            if (_userRequest.PayloadChar == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, no data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"UPDATE `Chars` SET `Experience` = '{_userRequest.PayloadChar.Experience}' WHERE `User` = '{userID}' AND `Name` = '{_userRequest.PayloadChar.Name}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = "Error, exp couldn't be added." };
            }

            return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = "Character gained experience." };
        }

        // get the level stats of the requested level
        ServerResponse GetLevelStatus(UserRequest _userRequest)
        {
            if (_userRequest.PayloadLevelStatus == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, no LevelStatus data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            LevelStatus levelStatusToBeReturned = new LevelStatus();

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `UnlockedLevels` WHERE `LevelID` = '{_userRequest.PayloadLevelStatus.LevelID}' AND `UserID` = '{userID}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                levelStatusToBeReturned.UnlockStatus = (int)reader["UnlockStatus"];
            }

            con.Close();

            return new ServerResponse()
            {
                ResponseType = EServerResponseType.OK,
                Text = $"LevelStatus data was successfully received.",
                LevelStatus = levelStatusToBeReturned
            };
        }

        // get the level data of the requested level | enemies and stats
        ServerResponse GetLevelData(UserRequest _userRequest)
        {
            if (_userRequest.PayloadLevelData == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, no LevelStatus data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `LevelData` WHERE `ID` = '{_userRequest.PayloadLevelData.LevelID}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            string tableName = null;

            while (reader.Read())
            {
                tableName = (string)reader["EnemyTableName"];
            }

            con.Close();

            if (tableName == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, level doesn't exist" };
            }

            LevelData requestedlevelData = new LevelData();

            requestedlevelData.Enemies = new System.Collections.Generic.List<Character>();

            con = new MySqlConnection(DatabaseConnection);
            con.Open();

            cmdText = $"SELECT * FROM `{tableName}`;";

            sqlCommand = new MySqlCommand(cmdText, con);

            reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                Character enemyTmp = new Character();
                enemyTmp.AttackDamage = (int)reader["Attack"];
                enemyTmp.Defense = (int)reader["Defense"];
                enemyTmp.HP = (int)reader["HP"];
                enemyTmp.Name = (string)reader["CharacterName"];

                requestedlevelData.Enemies.Add(enemyTmp);
            }

            con.Close();

            return new ServerResponse()
            {
                ResponseType = EServerResponseType.OK,
                Text = $"LevelData was successfully received.",
                LevelData = requestedlevelData
            };
        }

        // unlocks the requested level for the user
        ServerResponse UnlockLevelForUser(UserRequest _userRequest)
        {
            if (_userRequest.PayloadLevelStatus == null)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, no LevelStatus data was sent." };
            }

            int userID = GetUserID(_userRequest.Username);

            if (userID == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, user doesnt exist." };
            }

            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"UPDATE `UnlockedLevels` SET `UnlockStatus` = '1' WHERE `LevelID` = '{_userRequest.PayloadLevelStatus.LevelID}' AND `UserID` = '{userID}';";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
            {
                return new ServerResponse() { ResponseType = EServerResponseType.ERROR, Text = $"Error, the level couldn't be unlocked." };
            }

            return new ServerResponse() { ResponseType = EServerResponseType.OK, Text = $"The level was successfully unlocked." };
        }

        // creates all new character entries for the new user
        void CreateAllCharactersForNewUser(int _id)
        {
            // create every character for new user in database

            // Adventurer
            CreateCharacterForNewUser(_id, "Adventurer", 1, 12, 12, 20);

            // Bandit                 
            CreateCharacterForNewUser(_id, "Bandit", 0, 13, 8, 22);

            // Golem                  
            CreateCharacterForNewUser(_id, "Golem", 0, 8, 13, 30);

            // Mandrake               
            CreateCharacterForNewUser(_id, "Mandrake", 0, 11, 16, 18);

            // Rat                    
            CreateCharacterForNewUser(_id, "Rat", 0, 16, 11, 18);

            // RedOgre                
            CreateCharacterForNewUser(_id, "Red Ogre", 0, 14, 9, 28);

            // Satyr                  
            CreateCharacterForNewUser(_id, "Satyr", 0, 17, 17, 17);

            // Shade                  
            CreateCharacterForNewUser(_id, "Shade", 0, 26, 8, 16);

            // Wasp                   
            CreateCharacterForNewUser(_id, "Wasp", 0, 20, 12, 17);

            // Yeti                   
            CreateCharacterForNewUser(_id, "Yeti", 0, 11, 13, 28);

            // Werewolf               
            CreateCharacterForNewUser(_id, "Werewolf", 0, 15, 10, 25);
        }

        // creates a new (un)locked character entry in the database
        bool CreateCharacterForNewUser(int _playerID, string _characterName, int _unlockStatus, int _attackDamage, int _defense, int _HP)
        {
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            // add every char to the database
            string cmdText = $"INSERT INTO `Chars` (`ID`, `Name`, `Level`, `UnlockStatus`, `User`, `AttackDamage`, `Defense`, `HP`, `Experience`) VALUES (NULL, '{_characterName}', '1', '{_unlockStatus}', '{_playerID}', '{_attackDamage}', '{_defense}', '{_HP}', '0');";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
                return false;

            return true;
        }

        // creates all level entries for the new user
        void CreateAllLevelsForNewUser(int _id)
        {
            // create every level for the new user

            // Level 1
            CreateLevelForNewUser(_id, 1, 1);

            // Level 2 to 5
            for (int i = 2; i <= 5; i++)
            {
                CreateLevelForNewUser(_id, i, 0);
            }
        }

        bool CreateMaterialEntryForNewUser(int _id)
        {
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            // add a material entry for the new user
            string cmdText = $"INSERT INTO `resources` (`ID`, `UserID`, `StepCounter`, `NatureMaterial`, `DarkMaterial`, `AncientMaterial`, `NeutralMaterial`, `ChaosMaterial`) VALUES (NULL, '{_id}', '0', '0', '0', '0', '0', '0');";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
            {
                return false;
            }

            return true;
        }

        // creates a new (un)locked level ebtry in the database
        bool CreateLevelForNewUser(int _playerID, int _levelID, int _unlockStatus)
        {
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            // add every char to the database
            string cmdText = $"INSERT INTO `UnlockedLevels` (`ID`, `LevelID`, `UserID`, `UnlockStatus`) VALUES (NULL, '{_levelID}', '{_playerID}', '{_unlockStatus}');";

            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            int rowsAffected = sqlCommand.ExecuteNonQuery();

            con.Close();

            if (rowsAffected == -1)
                return false;

            return true;
        }

        // checks if the user exists in the database
        bool CheckIfUserExists(User _user, bool _passwordCheck)
        {
            // Open up connection for database check
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            // if password doesn't need to be checked
            if (!_passwordCheck)
            {
                // sql command to check if there is an user with that name
                string cmdText = $"SELECT * FROM `user` WHERE `Name` = '{_user.Name}';";
                MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

                MySqlDataReader reader = sqlCommand.ExecuteReader();

                while (reader.Read())
                {
                    // if there is an user with that name return true
                    if ((string)reader["Name"] == _user.Name)
                    {
                        con.Close();
                        return true;
                    }
                }
                // close old connection before new one can be opened up
                con.Close();

                // no user found | return false
                return false;
            }

            // if password needs to be checked aswell
            else
            {
                // sql command to check if there is an user with that name and password
                string cmdText = $"SELECT * FROM `user` WHERE `Name` = '{_user.Name}' AND `Password` = '{_user.Password}';";
                MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

                MySqlDataReader reader = sqlCommand.ExecuteReader();

                while (reader.Read())
                {
                    // if there is a user with that name and password return true
                    if ((string)reader["Name"] == _user.Name && (string)reader["Password"] == _user.Password)
                    {
                        con.Close();
                        // user found | return true
                        return true;
                    }
                }
                // close old connection before new one can be opened up
                con.Close();

                // no user found return false
                return false;
            }
        }

        // returns the userID from the database based on the username
        int GetUserID(string _user)
        {
            // Open up connection for database check
            MySqlConnection con = new MySqlConnection(DatabaseConnection);
            con.Open();

            string cmdText = $"SELECT * FROM `user` WHERE `Name` = '{_user}';";
            MySqlCommand sqlCommand = new MySqlCommand(cmdText, con);

            MySqlDataReader reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                // con.Close();
                int IDTemp = (int)reader["ID"];
                con.Close();
                return IDTemp;
            }

            // close old connection before new one can be opened up
            con.Close();

            return -1;
        }
    }
}
