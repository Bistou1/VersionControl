using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace SurvivalEngine
{

    /// <summary>
    /// Tool to align all selected objects to a round position (1, 2) by removing decimals
    /// </summary>

    public class AlignObjects : ScriptableWizard
    {

        [MenuItem("Survival Engine/Align Objects", priority = 301)]
        static void SelectAllOfTagWizard()
        {
            ScriptableWizard.DisplayWizard<AlignObjects>("AlignObjects", "AlignObjects");
        }

        void DoAlignCubes()
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "align objects");
            foreach (Transform transform in Selection.transforms)
            {
                transform.position = new Vector3(Mathf.RoundToInt(transform.position.x), 0f, Mathf.RoundToInt(transform.position.z));
            }
        }

        void OnWizardCreate()
        {
            DoAlignCubes();
        }
    }

}