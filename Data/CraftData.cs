using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    [System.Serializable]
    public class CraftCostData
    {
        public Dictionary<ItemData, int> craft_items = new Dictionary<ItemData, int>();
        public GroupData craft_near;
    }

    /// <summary>
    /// Parent data class for craftable items (items, constructions, plants)
    /// </summary>

    public class CraftData : ScriptableObject
    {
        [Header("--- CraftData ------------------")]
        public string id;

        [Header("Display")]
        public string title;
        public Sprite icon;
        [TextArea(3, 5)]
        public string desc;

        [Header("Groups")]
        public GroupData[] groups;

        [Header("Crafting")]
        public bool craftable; //Can be crafted? If false, can still be learn through the learn action
        public int craft_quantity = 1; //Does it craft more than 1?
        public GroupData craft_near; //Group of selectable required near the player to craft this (ex: fire source, water source)
        public ItemData[] craft_items; //Items needed to craft this
        public int craft_sort_order = 0; //Which items appear first in crafting menu

        [Header("FX")]
        public AudioClip craft_sound;

        private static List<CraftData> craft_data = new List<CraftData>();

        public bool HasGroup(GroupData group)
        {
            foreach (GroupData agroup in groups)
            {
                if (agroup == group)
                    return true;
            }
            return false;
        }

        public bool HasGroup(GroupData[] mgroups)
        {
            foreach (GroupData mgroup in mgroups)
            {
                foreach (GroupData agroup in groups)
                {
                    if (agroup == mgroup)
                        return true;
                }
            }
            return false;
        }


        public ItemData GetItem()
        {
            if (this is ItemData)
                return (ItemData)this;
            return null;
        }

        public ConstructionData GetConstruction()
        {
            if (this is ConstructionData)
                return (ConstructionData)this;
            return null;
        }

        public PlantData GetPlant()
        {
            if (this is PlantData)
                return (PlantData)this;
            return null;
        }

        public CharacterData GetCharacter()
        {
            if (this is CharacterData)
                return (CharacterData)this;
            return null;
        }

        public CraftCostData GetCraftCost()
        {
            CraftCostData cost = new CraftCostData();
            foreach (ItemData item in craft_items)
            {
                if (!cost.craft_items.ContainsKey(item))
                    cost.craft_items[item] = 1;
                else
                    cost.craft_items[item] += 1;
            }

            if (craft_near != null)
                cost.craft_near = craft_near;

            return cost;
        }

        public static void Load()
        {
            craft_data.Clear();
            craft_data.AddRange(ItemData.GetAll());
            craft_data.AddRange(ConstructionData.GetAll());
            craft_data.AddRange(PlantData.GetAll());
        }

        public static List<CraftData> GetAllInGroup(GroupData group)
        {
            List<CraftData> olist = new List<CraftData>();
            foreach (CraftData item in craft_data)
            {
                if (item.HasGroup(group))
                    olist.Add(item);
            }
            return olist;
        }

        public static List<CraftData> GetAllCraftableInGroup(GroupData group)
        {
            List<CraftData> olist = new List<CraftData>();
            foreach (CraftData item in craft_data)
            {
                if (item.craft_quantity > 0 && item.HasGroup(group))
                {
                    bool learnt = item.craftable || PlayerData.Get().IsIDUnlocked(item.id);
                    if(learnt)
                        olist.Add(item);
                }
            }
            return olist;
        }

        public static CraftData Get(string id)
        {
            foreach (CraftData item in craft_data)
            {
                if (item.id == id)
                    return item;
            }
            return null;
        }

        public static List<CraftData> GetAll()
        {
            return craft_data;
        }
    }

}