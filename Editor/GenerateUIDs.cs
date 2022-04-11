using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace SurvivalEngine
{

    /// <summary>
    /// Generates all empty Unique IDs in the scene. Use this tool after adding new objects to make sure they all have a UID.
    /// This will also find dupplicate UIDs and replace them with a new UID. It will keep UID without dupplicates unchanged.
    /// </summary>

    public class GenerateUIDs : ScriptableWizard
    {
        [MenuItem("Survival Engine/Generate UIDs", priority = 200)]
        static void SelectAllOfTagWizard()
        {
            ScriptableWizard.DisplayWizard<GenerateUIDs>("Generate Unique IDs", "Generate All UIDs");
        }

        void OnWizardCreate()
        {
            UniqueID.GenerateAll(GameObject.FindObjectsOfType<UniqueID>());

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

}