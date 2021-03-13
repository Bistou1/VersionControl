﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    public enum ItemType
    {

        Basic = 0,
        Consumable = 10,
        Equipment = 20,

    }

    public enum DurabilityType
    {
        None = 0,
        UsageCount = 5, //Each use (like attacking or receiving hit) reduces durability, value is in use count
        UsageTime = 8, //Similar to spoilage, but only reduces while equipped, value is in game-hours
        Spoilage = 10, //Reduces over time, even when not in inventory, value is in game-hours
    }

    public enum EquipSlot
    {
        None = 0,
        Hand = 10,
        Head = 20,
        Body = 30,
        Feet = 40,
        Backpack = 50,
        Accessory = 60,

        //Generic slot for other parts, rename them to your own
        Slot7 = 70,
        Slot8 = 80,
        Slot9 = 90,
    }

    public enum EquipSide
    {
        Default = 0,
        Right = 2,
        Left = 4,
    }

    /// <summary>
    /// Data file for Items
    /// </summary>

    [CreateAssetMenu(fileName = "ItemData", menuName = "SurvivalEngine/ItemData", order = 2)]
    public class ItemData : CraftData
    {
        [Header("--- ItemData ------------------")]
        public ItemType type;

        [Header("Stats")]
        public int inventory_max = 20;
        public DurabilityType durability_type;
        public float durability = 0f; //0f means infinite, 1f per hour for consumable, 1f per hit for equipment

        [Header("Stats Equip")]
        public EquipSlot equip_slot;
        public EquipSide equip_side;
        public int armor = 0;
        public int bag_size = 0;
        public BonusEffectData[] equip_bonus;

        [Header("Stats Equip Weapon")]
        public bool weapon;
        public bool ranged;
        public int damage = 0;
        public float range = 1f;
        public int strike_per_attack = 0; //Minimum is 1, if set to 3, each attack will hit 3 times, or shoot 3 projectiles
        public float strike_interval = 0f; //Interval in seconds between each strike of a single attack

        [Header("Stats Consume")]
        public int eat_hp = 0;
        public int eat_hunger = 0;
        public int eat_thirst = 0;
        public int eat_happiness = 0;
        public BonusEffectData[] eat_bonus;
        public float eat_bonus_duration = 0f;

        [Header("Action")]
        public SAction[] actions;

        [Header("Ref Data")]
        public ItemData container_data;
        public PlantData plant_data;
        public ConstructionData construction_data;
        public GroupData projectile_group;

        [Header("Prefab")]
        public GameObject item_prefab;
        public GameObject equipped_prefab;
        public GameObject projectile_prefab;


        private static List<ItemData> item_data = new List<ItemData>(); //For looping
        private static Dictionary<string, ItemData> item_dict = new Dictionary<string, ItemData>(); //Faster access

        public MAction FindMergeAction(ItemData other)
        {
            if (other == null)
                return null;

            foreach (SAction action in actions)
            {
                if (action is MAction)
                {
                    MAction maction = (MAction)action;
                    if (other.HasGroup(maction.merge_target))
                    {
                        return maction;
                    }
                }
            }
            return null;
        }

        public MAction FindMergeAction(Selectable other)
        {
            if (other == null)
                return null;

            foreach (SAction action in actions)
            {
                if (action is MAction)
                {
                    MAction maction = (MAction)action;
                    if (other.HasGroup(maction.merge_target))
                    {
                        return maction;
                    }
                }
            }
            return null;
        }

        public bool CanBeDropped()
        {
            return item_prefab != null;
        }

        public bool CanBeBuilt()
        {
            return construction_data != null;
        }

        public bool CanBeSowed()
        {
            return plant_data != null;
        }

        public bool HasDurability()
        {
            return durability_type != DurabilityType.None && durability >= 0.1f;
        }

        //From 0 to 100
        public int GetDurabilityPercent(float current_durability)
        {
            float perc = durability > 0.01f ? Mathf.Clamp01(current_durability / durability) : 0f;
            return Mathf.RoundToInt(perc * 100f);
        }

        public static void Load(string items_folder)
        {
            item_data.Clear();
            item_dict.Clear();
            item_data.AddRange(Resources.LoadAll<ItemData>(items_folder));
            foreach (ItemData item in item_data)
            {
                item_dict.Add(item.id, item);
            }
        }

        public new static ItemData Get(string item_id)
        {
            if (item_id != null && item_dict.ContainsKey(item_id))
                return item_dict[item_id];
            return null;
        }

        public new static List<ItemData> GetAll()
        {
            return item_data;
        }

        public static int GetEquipIndex(EquipSlot slot)
        {
            if (slot == EquipSlot.Hand)
                return 0;
            if (slot == EquipSlot.Head)
                return 1;
            if (slot == EquipSlot.Body)
                return 2;
            if (slot == EquipSlot.Feet)
                return 3;
            if (slot == EquipSlot.Backpack)
                return 4;
            if (slot == EquipSlot.Accessory)
                return 5;
            if (slot == EquipSlot.Slot7)
                return 6;
            if (slot == EquipSlot.Slot8)
                return 7;
            if (slot == EquipSlot.Slot9)
                return 8;
            return -1;
        }

        public static EquipSlot GetEquipType(int index)
        {
            if (index == 0)
                return EquipSlot.Hand;
            if (index == 1)
                return EquipSlot.Head;
            if (index == 2)
                return EquipSlot.Body;
            if (index == 3)
                return EquipSlot.Feet;
            if (index == 4)
                return EquipSlot.Backpack;
            if (index == 5)
                return EquipSlot.Accessory;
            if (index == 6)
                return EquipSlot.Slot7;
            if (index == 7)
                return EquipSlot.Slot8;
            if (index == 8)
                return EquipSlot.Slot9;
            return EquipSlot.None;
        }
    }

}