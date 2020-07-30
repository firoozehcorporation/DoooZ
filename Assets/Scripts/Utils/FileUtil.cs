using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FiroozehGameService.Core;
using FiroozehGameService.Models.BasicApi;
using Handlers;
using Models;
using UnityEngine;

namespace Utils
{
    public static class FileUtil
    {
        public static void SaveUserToken(string userToken)
        {
            var bf = new BinaryFormatter();
            var file = File.Create (Application.persistentDataPath + "/Login.dat");
            bf.Serialize(file,userToken);
            file.Close();
        }

        public static string GetUserToken()
        {
            if (!File.Exists(Application.persistentDataPath + "/Login.dat")) return null;
            var bf = new BinaryFormatter();
            var file = File.Open(Application.persistentDataPath + "/Login.dat", FileMode.Open);
            if (file.Length == 0) return null;
            var userToken = (string)bf.Deserialize(file);
            file.Close();
            return userToken;
        }

        public static bool IsLoginBefore()
        {
            return GetUserToken() != null;
        }

        public static async Task IncreaseWin()
        {
            try
            {
                var wins = GetWins();
                if(wins != -1) SaveWins(wins + 1);
                else SaveWins(1);
            
                // Save New Win
                await GameService.SaveGame("SaveFile", new Save { WinCounts = GetWins()});
            
                wins = GetWins();
                Achievement achievement;
                switch (wins)
                {
                    //Achievements Checker
                    case 1:
                        achievement = await AchievementHandler.UnlockFirstWin();
                        NotificationUtils.NotifyUnlockAchievement(achievement);
                        break;
                    case 10:
                        achievement = await AchievementHandler.UnlockProfessional();
                        NotificationUtils.NotifyUnlockAchievement(achievement);
                        break;
                    case 50:
                        achievement = await AchievementHandler.UnlockMaster();
                        NotificationUtils.NotifyUnlockAchievement(achievement);
                        break;
                    default:
                    {
                        // SubmitScore To LeaderBoard
                        if (wins > 50)
                        {
                            var score = await LeaderBoardHandler.SubmitScore(wins);
                            NotificationUtils.NotifySubmitScore(score.Leaderboard,score.Score);
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
               Debug.LogError("IncreaseWin : " + e);
            }
            
        }

        private static int GetWins()
        {
            if (!File.Exists(Application.persistentDataPath + "/data.dat")) return -1;
            var bf = new BinaryFormatter();
            var file = File.Open(Application.persistentDataPath + "/data.dat", FileMode.Open);
            if (file.Length == 0) return -1;
            var wins = (int)bf.Deserialize(file);
            file.Close();
            return wins;
        }

        public static void SaveWins(int wins)
        {
                var bf = new BinaryFormatter();
                var file = File.Create (Application.persistentDataPath + "/data.dat");
                bf.Serialize(file,wins);
                file.Close();
        }
    }
}