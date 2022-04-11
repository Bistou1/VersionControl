using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace SurvivalEngine
{
    /// <summary>
    /// Check if can apply any automatic fixes to issues that could be caused from changing asset version
    /// </summary>

    public class FixProjectVersion : ScriptableWizard
    {
        [MenuItem("Survival Engine/Fix Project Version", priority = 400)]
        static void SelectAllOfTagWizard()
        {
            ScriptableWizard.DisplayWizard<FixProjectVersion>("Fix Project Version", "Fix");
        }

        void OnWizardCreate()
        {
            string[] allPrefabs = GetAllPrefabs();
            foreach (string prefab_path in allPrefabs)
            {
                GameObject prefab = (GameObject) AssetDatabase.LoadMainAssetAtPath(prefab_path);
                if (prefab != null)
                {
                    //Add buildable to constructions
                    if (prefab.GetComponent<Construction>() != null && prefab.GetComponent<Buildable>() == null)
                    {
                        prefab.AddComponent<Buildable>();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log("Added Buildable Component to: " + prefab_path);
                    }

                    //Add buildable to plants
                    if (prefab.GetComponent<Plant>() != null && prefab.GetComponent<Buildable>() == null)
                    {
                        prefab.AddComponent<Buildable>();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log("Added Buildable Component to: " + prefab_path);
                    }

                    //Add character to animals
                    if (prefab.GetComponent<Animal>() != null && prefab.GetComponent<Character>() == null)
                    {
                        prefab.AddComponent<Character>();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log("Added Character Component to: " + prefab_path);
                    }

                    //Add character to birds
                    if (prefab.GetComponent<Bird>() != null && prefab.GetComponent<Character>() == null)
                    {
                        prefab.AddComponent<Character>();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log("Added Character Component to: " + prefab_path);
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static string[] GetAllPrefabs()
        {
            string[] temp = AssetDatabase.GetAllAssetPaths();
            List<string> result = new List<string>();
            foreach (string s in temp)
            {
                if (s.Contains(".prefab")) result.Add(s);
            }
            return result.ToArray();
        }
    }

}