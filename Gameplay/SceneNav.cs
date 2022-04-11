using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivalEngine
{

    //Script to manage transitions between scenes
    public class SceneNav
    {
        public static void GoToLevel(string level_name, int entry_index = 0)
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null && level_name != "")
            {
                pdata.current_scene = level_name;
                pdata.current_entry_index = entry_index;

                PlayerData.Get().Save();
                GoTo(level_name);
            }
        }

        public static void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static void GoTo(string scene)
        {
            SceneManager.LoadScene(scene);
        }

        public static string GetCurrentScene()
        {
            return SceneManager.GetActiveScene().name;
        }

        public static bool DoSceneExist(string scene)
        {
            return Application.CanStreamedLevelBeLoaded(scene);
        }
    }

}