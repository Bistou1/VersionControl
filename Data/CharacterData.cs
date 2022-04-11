﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Data file for Characters
    /// </summary>

    [CreateAssetMenu(fileName = "CharacterData", menuName = "Data/CharacterData", order = 5)]
    public class CharacterData : CraftData
    {
        [Header("--- CharacterData ------------------")]

        public GameObject character_prefab; //Prefab spawned when the character is built

        private static List<CharacterData> character_data = new List<CharacterData>();

        public static void Load(string characters_folder)
        {
            character_data.Clear();
            character_data.AddRange(Resources.LoadAll<CharacterData>(characters_folder));
        }

        public new static CharacterData Get(string character_id)
        {
            foreach (CharacterData item in character_data)
            {
                if (item.id == character_id)
                    return item;
            }
            return null;
        }

        public new static List<CharacterData> GetAll()
        {
            return character_data;
        }
    }

}
