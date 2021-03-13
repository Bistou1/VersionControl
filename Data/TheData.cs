using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Manager script that will load all scriptable objects for use at runtime
    /// </summary>

    public class TheData : MonoBehaviour
    {
        public GameData data;

        private static TheData _instance;

        void Awake()
        {
            _instance = this;
            ItemData.Load(data.items_folder);
            ConstructionData.Load(data.constructions_folder);
            PlantData.Load(data.plants_folder);
            CharacterData.Load(data.characters_folder);
            CraftData.Load();
        }

        public static TheData Get()
        {
            return _instance;
        }
    }

}