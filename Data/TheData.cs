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
        public AssetData assets;

        private static TheData _instance;

        void Awake()
        {
            _instance = this;
            ItemData.Load(assets.items_folder);
            ConstructionData.Load(assets.constructions_folder);
            PlantData.Load(assets.plants_folder);
            CharacterData.Load(assets.characters_folder);
            CraftData.Load();

            //Load managers
            if (!FindObjectOfType<TheUI>())
                Instantiate(TheGame.IsMobile() ? assets.ui_canvas_mobile : assets.ui_canvas);
            if (!FindObjectOfType<TheAudio>())
                Instantiate(assets.audio_manager);
            if (!FindObjectOfType<ActionSelector>())
                Instantiate(assets.action_selector);
        }

        public static TheData Get()
        {
            if (_instance == null)
                _instance = FindObjectOfType<TheData>();
            return _instance;
        }
    }

}