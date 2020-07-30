using FiroozehGameService.Models.BasicApi;
using UnityEngine;

namespace Utils
{
    
    public static class NotificationUtils
    {        
        public static void Init()
        {
            // TODO Implement it
        }
        
        public static void NotifyUnlockAchievement(Achievement achievement)
        {            
            // TODO Implement it
            Debug.LogError("NotifyUnlockAchievement Called : " + achievement.Name);
        }
        
        public static void NotifySubmitScore(LeaderBoard leaderBoard,int score)
        {
            // TODO Implement it
            Debug.LogError("NotifySubmitScore Called : " + leaderBoard.Name);
        }
      
    }
}