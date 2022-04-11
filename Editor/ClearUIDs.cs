using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace SurvivalEngine
{

    /// <summary>
    /// Clear all Unique IDs in the scene 
    /// (WARNING: changing UIDs will make objects incompatible with older save file as all UIDs are changed, objects are tracked by their UID in the save file).
    /// </summary>

    public class ClearUIDs : ScriptableWizard
    {

        [MenuItem("Survival Engine/Clear UIDs", priority = 201)]
        static void SelectAllOfTagWizard()
        {
            ScriptableWizard.DisplayWizard<ClearUIDs>("Clear Unique IDs", "Clear All UIDs");
        }

        void OnWizardCreate()
        {
            UniqueID.ClearAll(GameObject.FindObjectsOfType<UniqueID>());

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

}