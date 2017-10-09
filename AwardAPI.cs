using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GrandTheftMultiplayer.Server;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Shared;
using Newtonsoft.Json;

namespace AwardAPI
{
    public class Award
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public string TXDLib { get; set; }
        public string TXDName { get; set; }
        public int TXDColor { get; set; }

        public int RequiredProgress { get; set; }

        public Award(string name, string description, string txd_lib, string txd_name, int txd_color, int required_progress)
        {
            Name = name;
            Description = description;

            TXDLib = txd_lib;
            TXDName = txd_name;
            TXDColor = (txd_color < 0) ? 0 : ((txd_color > 3) ? 3 : txd_color);

            RequiredProgress = required_progress;
        }
    }

    public class PlayerAward
    {
        public string ID { get; set; }
        public int Progress { get; set; }
        public bool Unlocked { get; set; }
        public DateTime? UnlockDate { get; set; }

        public PlayerAward(string ID, int progress, bool unlocked, DateTime? unlockdate)
        {
            this.ID = ID;
            Progress = progress;
            Unlocked = unlocked;
            UnlockDate = unlockdate;
        }
    }

    public class AwardAPI : Script
    {
        string AWARDS_SAVE_DIR = "PlayerData";
        Dictionary<string, Award> AwardDict = new Dictionary<string, Award>();
        Dictionary<NetHandle, List<PlayerAward>> PlayerAwardDict = new Dictionary<NetHandle, List<PlayerAward>>();
        
        public event ExportedEvent OnPlayerUnlockAward;

        public AwardAPI()
        {
            API.onResourceStart += Awards_Init;
            API.onPlayerDisconnected += Awards_PlayerLeave;
            API.onResourceStop += Awards_Exit;
        }

        #region Exported Methods (Award)
        public bool CreateAward(string ID, string name, string description, string txd_lib, string txd_name, int txd_color, int required_progress)
        {
            if (AwardDict.ContainsKey(ID))
            {
                API.consoleOutput("AwardAPI: Can't create {0}, ID in use.", ID);
                return false;
            }

            AwardDict.Add(ID, new Award(name, description, txd_lib, txd_name, txd_color, required_progress));
            return true;
        }

        public bool IsIDInUse(string ID)
        {
            return (AwardDict.ContainsKey(ID));
        }

        public string[] GetAllAwardIDs()
        {
            return AwardDict.Keys.ToArray();
        }

        public string GetAwardName(string ID)
        {
            return ((AwardDict.ContainsKey(ID)) ? AwardDict[ID].Name : string.Empty);
        }

        public string GetAwardDescription(string ID)
        {
            return ((AwardDict.ContainsKey(ID)) ? AwardDict[ID].Description : string.Empty);
        }

        public int GetAwardRequiredProgress(string ID)
        {
            return ((AwardDict.ContainsKey(ID)) ? AwardDict[ID].RequiredProgress : 0);
        }
        #endregion

        #region Exported Methods (Player)
        public void InitPlayer(Client player)
        {
            string player_file = AWARDS_SAVE_DIR + Path.DirectorySeparatorChar + player.socialClubName + ".json";

            if (File.Exists(player_file))
            {
                List<PlayerAward> data = JsonConvert.DeserializeObject<List<PlayerAward>>(File.ReadAllText(player_file));

                if (PlayerAwardDict.ContainsKey(player.handle))
                {
                    PlayerAwardDict[player.handle].Clear();
                    PlayerAwardDict[player.handle] = data;
                }
                else
                {
                    PlayerAwardDict.Add(player.handle, data);
                }
            }
            else
            {
                PlayerAwardDict.Add(player.handle, new List<PlayerAward>());
            }
        }

        public bool GiveAwardProgress(Client player, string ID, int progress)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null)
            {
                PlayerAward new_award = null;
                bool unlocked = false;

                if (progress >= AwardDict[ID].RequiredProgress)
                {
                    new_award = new PlayerAward(ID, progress, true, DateTime.Now);
                    unlocked = true;
                }
                else
                {
                    new_award = new PlayerAward(ID, progress, false, null);
                }

                PlayerAwardDict[player.handle].Add(new_award);

                if (unlocked)
                {
                    UnlockAward(player, ID);
                }
                else
                {
                    SavePlayer(player);
                }
            }
            else
            {
                award.Progress += progress;

                if (award.Progress >= AwardDict[ID].RequiredProgress)
                {
                    UnlockAward(player, ID);
                }
                else
                {
                    SavePlayer(player);
                }
            }

            return true;
        }

        public bool SetAwardProgress(Client player, string ID, int progress)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null)
            {
                PlayerAward new_award = null;
                bool unlocked = false;

                if (progress >= AwardDict[ID].RequiredProgress)
                {
                    new_award = new PlayerAward(ID, progress, true, DateTime.Now);
                    unlocked = true;
                }
                else
                {
                    new_award = new PlayerAward(ID, progress, false, null);
                }

                PlayerAwardDict[player.handle].Add(new_award);

                if (unlocked)
                {
                    UnlockAward(player, ID);
                }
                else
                {
                    SavePlayer(player);
                }
            }
            else
            {
                award.Progress = progress;

                if (award.Progress >= AwardDict[ID].RequiredProgress)
                {
                    UnlockAward(player, ID);
                }
                else
                {
                    SavePlayer(player);
                }
            }

            return true;
        }

        public int GetAwardProgress(Client player, string ID)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return 0;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null) return 0;

            return award.Progress;
        }

        public bool RemoveAward(Client player, string ID)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null) return false;

            PlayerAwardDict[player.handle].Remove(award);
            SavePlayer(player);
            return true;
        }

        public bool RemoveAllAwards(Client player)
        {
            if (!PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAwardDict[player.handle].Clear();
            SavePlayer(player);
            return true;
        }

        public bool IsAwardUnlocked(Client player, string ID)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null) return false;

            return award.Unlocked;
        }

        public DateTime? GetAwardUnlockDate(Client player, string ID)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return null;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null) return null;

            return award.UnlockDate;
        }

        public bool LockAward(Client player, string ID)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            if (award == null) return false;

            award.Unlocked = false;
            award.UnlockDate = null;
            SavePlayer(player);
            return true;
        }

        public bool UnlockAward(Client player, string ID)
        {
            if (!AwardDict.ContainsKey(ID) || !PlayerAwardDict.ContainsKey(player.handle)) return false;

            PlayerAward award = PlayerAwardDict[player.handle].FirstOrDefault(a => a.ID == ID);
            bool new_unlock = false;

            if (award == null)
            {
                PlayerAwardDict[player.handle].Add(new PlayerAward(ID, AwardDict[ID].RequiredProgress, true, DateTime.Now));
                new_unlock = true;
            }
            else
            {
                if (!award.Unlocked)
                {
                    new_unlock = true;
                    award.Unlocked = true;
                    award.UnlockDate = DateTime.Now;
                }
            }

            SavePlayer(player);

            if (new_unlock)
            {
                player.triggerEvent("Award_Unlocked", API.toJson(AwardDict[ID]));
                OnPlayerUnlockAward(player, ID, AwardDict[ID].Name);
            }

            return true;
        }
        #endregion

        #region Methods
        public void SavePlayer(Client player)
        {
            if (!PlayerAwardDict.ContainsKey(player.handle)) return;

            string player_file = AWARDS_SAVE_DIR + Path.DirectorySeparatorChar + player.socialClubName + ".json";
            File.WriteAllText(player_file, JsonConvert.SerializeObject(PlayerAwardDict[player.handle], Formatting.Indented));
        }
        #endregion

        #region Events
        public void Awards_Init()
        {
            AWARDS_SAVE_DIR = API.getResourceFolder() + Path.DirectorySeparatorChar + AWARDS_SAVE_DIR;
            if (!Directory.Exists(AWARDS_SAVE_DIR)) Directory.CreateDirectory(AWARDS_SAVE_DIR);
        }

        public void Awards_PlayerLeave(Client player, string reason)
        {
            if (PlayerAwardDict.ContainsKey(player.handle)) PlayerAwardDict.Remove(player.handle);
        }

        public void Awards_Exit()
        {
            PlayerAwardDict.Clear();
        }
        #endregion
    }
}