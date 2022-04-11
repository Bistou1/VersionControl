using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Script to write PlayerData to the disk, or to read a file containing PlayerData from the disk
    /// </summary>

    [System.Serializable]
    public class SaveSystem
    {
        private static string loaded = "";
        private static bool saving = false;

        public static PlayerData saved_data = null;

        private static void LoadData(string filename)
        {
            if (loaded != filename && File.Exists(Application.persistentDataPath + "/" + filename + ".data"))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(Application.persistentDataPath + "/" + filename + ".data", FileMode.Open);
                    saved_data = (PlayerData)bf.Deserialize(file);
                    file.Close();
                }
                catch (System.Exception e) { Debug.Log("Error Loading Data " + e); }
            }
            loaded = filename;
        }

        private static void SaveData(string filename)
        {

            if (IsLoaded() && loaded == filename)
            {
                saving = true;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Create(Application.persistentDataPath + "/" + filename + ".data");
                    bf.Serialize(file, saved_data);
                    file.Close();
                }
                catch (System.Exception e) { Debug.Log("Error Saving Data " + e); }
                saving = false;
            }
        }

        public static PlayerData Load(string filename)
        {
            LoadData(filename);
            if (saved_data == null)
                saved_data = new PlayerData(filename);
            return saved_data;
        }

        public static void Save(string filename, PlayerData player)
        {
            LoadData(filename);
            saved_data = player;
            SaveData(filename);
            PlayerPrefs.SetString("last_save", filename);
        }

        public static void Unload()
        {
            loaded = null;
        }

        public static void Delete(string filename)
        {
            if (loaded == filename)
            {
                loaded = null;
                saved_data = null;
            }

            string path = (Application.persistentDataPath + "/" + filename + ".data");
            if (File.Exists(path))
                File.Delete(path);
        }

        public static string GetLastSave()
        {
            return PlayerPrefs.GetString("last_save", "");
        }

        public static bool IsLoaded()
        {
            return saved_data != null && !string.IsNullOrEmpty(loaded);
        }

        public static bool IsSaving()
        {
            return saving;
        }
    }

}