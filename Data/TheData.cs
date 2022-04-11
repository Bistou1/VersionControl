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

        [Header("Resources")]
        public string items_folder = "Items";
        public string constructions_folder = "Constructions";
        public string plants_folder = "Plants";
        public string characters_folder = "Characters";

        private static TheData _instance;

        void Awake()
        {
            _instance = this;
            ItemData.Load(items_folder);
            ConstructionData.Load(constructions_folder);
            PlantData.Load(plants_folder);
            CharacterData.Load(characters_folder);
            CraftData.Load();
        }

        public static TheData Get()
        {
            return _instance;
        }
    }

}