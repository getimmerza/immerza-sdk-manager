using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public class SaveData
    {
        public string AccessToken;
        public string RefreshToken;
        public long ExpiresIn;
    }

    public static class SDKManagerSaving
    {
        private static readonly string SAVE_DATA_PATH = Path.Combine(Application.persistentDataPath, "ImmerzaSDKManager", "save-data.json");

        public static SaveData CrtSaveData = null;

        public static void Save(SaveData data)
        {
            if (!File.Exists(SAVE_DATA_PATH))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SAVE_DATA_PATH));
            }

            File.WriteAllText(SAVE_DATA_PATH, JsonConvert.SerializeObject(data));
        }

        public static SaveData Load()
        {
            SaveData data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SAVE_DATA_PATH));
            CrtSaveData = data;
            return data;
        }

        public static bool HasSave()
        {
            return File.Exists(SAVE_DATA_PATH);
        }
    }
}
