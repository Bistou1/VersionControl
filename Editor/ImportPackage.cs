using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SurvivalEngine
{
    /// <summary>
    /// Just notification at package import, can be deleted
    /// </summary>

    [InitializeOnLoad]
    public class ImportPackage
    {
        static bool completed = false;

        static ImportPackage()
        {
            EditorApplication.hierarchyChanged += AfterLoad;
        }

        static void AfterLoad()
        {
            if (!completed)
            {
                string floorLayer = LayerMask.LayerToName(9);
                if (string.IsNullOrEmpty(floorLayer))
                {
                    Debug.LogWarning("Survival Engine: We suggest to assign a name to the floor layer. Layer: 9 Name: Floor");
                }

                completed = true;
            }
        }

    }

}