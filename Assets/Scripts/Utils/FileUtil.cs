using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
    }
}