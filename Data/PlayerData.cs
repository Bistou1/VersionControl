using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SurvivalEngine
{

    [System.Serializable]
    public class InventoryItemData
    {
        public string item_id;
        public int quantity;
        public float durability;

        public InventoryItemData(string id, int q, float dura) { item_id = id; quantity = q; durability = dura; }
    }

    [System.Serializable]
    public class DroppedItemData
    {
        public string uid;
        public string item_id;
        public string scene;
        public Vector3Data pos;
        public int quantity;
        public float durability;
    }

    [System.Serializable]
    public class BuiltConstructionData
    {
        public string uid;
        public string construction_id;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
        public float durability;
    }

    [System.Serializable]
    public class SowedPlantData
    {
        public string uid;
        public string plant_id;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
        public int growth_stage;
    }

    [System.Serializable]
    public class TrainedCharacterData
    {
        public string uid;
        public string character_id;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
    }

    [System.Serializable]
    public class StoredItemData
    {
        public Dictionary<int, InventoryItemData> items;

        public StoredItemData()
        {
            items = new Dictionary<int, InventoryItemData>();
        }
    }

    [System.Serializable]
    public class TimedBonusData
    {
        public BonusType bonus;
        public float time;
        public float value;
    }

    /// <summary>
    /// PlayerData is the main save file data script. Everything contained in this script is what will be saved. 
    /// It also contains a lot of functions to easily access the saved data. Make sure to call Save() to write the data to a file on the disk.
    /// The latest save file will be loaded automatically when starting the game
    /// </summary>

    [System.Serializable]
    public class PlayerData
    {

        public const string VERSION = "1.07";

        public const int inventory_size = 15; //Maximum number of items, dont change unless you also change the UI to reflect that

        public string filename;
        public string version;
        public DateTime last_save;

        //-------------------

        public int world_seed = 0; //Randomly Generated world
        public string current_scene = ""; //Scene loaded
        public int current_entry_index = 0; //-1 means go to current_pos, 0 means default scene pos, >0 means at matching entry index
        public Vector3Data current_pos;

        public int day = 0;
        public float day_time = 0f; // 0 = midnight, 24 = end of day
        public int xp = 0;

        public float master_volume = 1f;
        public float music_volume = 1f;
        public float sfx_volume = 1f;

        public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();
        public Dictionary<string, int> unique_ids = new Dictionary<string, int>();
        public Dictionary<string, bool> unlocked_ids = new Dictionary<string, bool>();
        public Dictionary<BonusType, TimedBonusData> timed_bonus_effects = new Dictionary<BonusType, TimedBonusData>();

        public Dictionary<int, InventoryItemData> inventory = new Dictionary<int, InventoryItemData>();
        public Dictionary<int, InventoryItemData> equipped_items = new Dictionary<int, InventoryItemData>(); //0=hand, 1=head, 2=body, 3=boot

        public Dictionary<string, int> removed_objects = new Dictionary<string, int>();
        public Dictionary<string, DroppedItemData> dropped_items = new Dictionary<string, DroppedItemData>();
        public Dictionary<string, BuiltConstructionData> built_constructions = new Dictionary<string, BuiltConstructionData>();
        public Dictionary<string, SowedPlantData> sowed_plants = new Dictionary<string, SowedPlantData>();
        public Dictionary<string, TrainedCharacterData> trained_characters = new Dictionary<string, TrainedCharacterData>();
        public Dictionary<string, StoredItemData> stored_items = new Dictionary<string, StoredItemData>();

        //-------------------

        public static PlayerData player_data = null;

        public PlayerData(string name)
        {
            filename = name;
            version = VERSION;

            day = 1;
            day_time = 6f; // Start game at 6 in the morning

            master_volume = 1f;
            music_volume = 1f;
            sfx_volume = 1f;

        }

        public void FixData()
        {
            //Fix data to make sure old save files compatible with new game version
            if (attributes == null)
                attributes = new Dictionary<AttributeType, float>();
            if (unique_ids == null)
                unique_ids = new Dictionary<string, int>();
            if (unlocked_ids == null)
                unlocked_ids = new Dictionary<string, bool>();
            if (timed_bonus_effects == null)
                timed_bonus_effects = new Dictionary<BonusType, TimedBonusData>();
            if (inventory == null)
                inventory = new Dictionary<int, InventoryItemData>();
            if (equipped_items == null)
                equipped_items = new Dictionary<int, InventoryItemData>();
            if (dropped_items == null)
                dropped_items = new Dictionary<string, DroppedItemData>();
            if (removed_objects == null)
                removed_objects = new Dictionary<string, int>();
            if (built_constructions == null)
                built_constructions = new Dictionary<string, BuiltConstructionData>();
            if (sowed_plants == null)
                sowed_plants = new Dictionary<string, SowedPlantData>();
            if (trained_characters == null)
                trained_characters = new Dictionary<string, TrainedCharacterData>();
            if (stored_items == null)
                stored_items = new Dictionary<string, StoredItemData>();

            if (version == "0.01")
            { //Clear non compatible data
                player_data = new PlayerData(filename);
            }
        }

        //---- Items -----
        public int AddItem(string item_id, int quantity, float durability)
        {
            ItemData idata = ItemData.Get(item_id);
            int max = idata != null ? idata.inventory_max : 999;
            int slot = GetFirstItemSlot(item_id, max - quantity);

            if (slot >= 0)
            {
                AddItemAt(item_id, slot, quantity, durability);
            }
            return slot;
        }

        public void RemoveItem(string item_id, int quantity)
        {
            Dictionary<int, int> remove_list = new Dictionary<int, int>(); //Slot, Quantity
            foreach (KeyValuePair<int, InventoryItemData> pair in inventory)
            {
                if (pair.Value != null && pair.Value.item_id == item_id && pair.Value.quantity > 0 && quantity > 0)
                {
                    int remove = Mathf.Min(quantity, pair.Value.quantity);
                    remove_list.Add(pair.Key, remove);
                    quantity -= remove;
                }
            }

            foreach (KeyValuePair<int, int> pair in remove_list)
            {
                RemoveItemAt(pair.Key, pair.Value);
            }
        }

        public void AddItemAt(string item_id, int slot, int quantity, float durability)
        {
            InventoryItemData invt_slot = GetItemSlot(slot);
            if (invt_slot != null && invt_slot.item_id == item_id)
            {
                int amount = invt_slot.quantity + quantity;
                float durabi = ((invt_slot.durability * invt_slot.quantity) + (durability * quantity)) / (float)amount;
                inventory[slot] = new InventoryItemData(item_id, amount, durabi);
            }
            else if (invt_slot == null || invt_slot.quantity <= 0)
            {
                inventory[slot] = new InventoryItemData(item_id, quantity, durability);
            }
        }

        public void RemoveItemAt(int slot, int quantity)
        {
            InventoryItemData invt_slot = GetItemSlot(slot);
            if (invt_slot != null && invt_slot.quantity > 0)
            {
                int amount = invt_slot.quantity - quantity;
                if (amount <= 0)
                    inventory.Remove(slot);
                else
                    inventory[slot] = new InventoryItemData(invt_slot.item_id, amount, invt_slot.durability);
            }
        }

        public void SwapItemSlots(int slot1, int slot2)
        {
            InventoryItemData invt_slot1 = GetItemSlot(slot1);
            InventoryItemData invt_slot2 = GetItemSlot(slot2);
            inventory[slot1] = invt_slot2;
            inventory[slot2] = invt_slot1;

            if (invt_slot2 == null)
                inventory.Remove(slot1);
            if (invt_slot1 == null)
                inventory.Remove(slot2);
        }

        public int CountItemType(string item_id)
        {
            int value = 0;
            foreach (KeyValuePair<int, InventoryItemData> pair in inventory)
            {
                if (pair.Value != null && pair.Value.item_id == item_id)
                    value += pair.Value.quantity;
            }
            return value;
        }

        public int GetFirstItemSlot(string item_id, int max = 999999)
        {
            foreach (KeyValuePair<int, InventoryItemData> pair in inventory)
            {
                if (pair.Key < inventory_size && pair.Value != null && pair.Value.item_id == item_id && pair.Value.quantity <= max)
                    return pair.Key;
            }
            return GetFirstEmptySlot();
        }

        public int GetFirstEmptySlot()
        {
            for (int i = 0; i < inventory_size; i++)
            {
                InventoryItemData invdata = GetItemSlot(i);
                if (invdata == null || invdata.quantity <= 0)
                    return i;
            }
            return -1;
        }

        public void AddItemDurability(int slot, float value)
        {
            if (inventory.ContainsKey(slot))
            {
                InventoryItemData invdata = inventory[slot];
                invdata.durability += value;
            }
        }

        public InventoryItemData GetItemSlot(int slot)
        {
            if (inventory.ContainsKey(slot))
                return inventory[slot];
            return null;
        }

        public bool CanTakeItem(string item_id, int quantity)
        {
            ItemData idata = ItemData.Get(item_id);
            int max = idata != null ? idata.inventory_max : 999;
            int slot = GetFirstItemSlot(item_id, max - quantity);
            return slot >= 0;
        }

        public bool HasItem(string item_id, int quantity = 1)
        {
            return CountItemType(item_id) >= quantity;
        }


        public bool HasItemIn(int slot)
        {
            return inventory.ContainsKey(slot) && inventory[slot].quantity > 0;
        }

        public bool IsItemIn(string item_id, int slot)
        {
            return inventory.ContainsKey(slot) && inventory[slot].item_id == item_id && inventory[slot].quantity > 0;
        }

        //-------- Dropped items --------

        public DroppedItemData AddDroppedItem(string item_id, string scene, Vector3 pos, int quantity, float durability)
        {
            DroppedItemData ditem = new DroppedItemData();
            ditem.uid = UniqueID.GenerateUniqueID();
            ditem.item_id = item_id;
            ditem.scene = scene;
            ditem.pos = pos;
            ditem.quantity = quantity;
            ditem.durability = durability;
            dropped_items[ditem.uid] = ditem;
            return ditem;
        }

        public void RemoveDroppedItem(string uid)
        {
            if (dropped_items.ContainsKey(uid))
                dropped_items.Remove(uid);
        }

        public DroppedItemData GetDroppedItem(string uid)
        {
            if (dropped_items.ContainsKey(uid))
                return dropped_items[uid];
            return null;
        }

        // ----- Equip Items ---- (islot=inventory, eslot=equipped)

        public void EquipItemTo(int islot, int eslot)
        {
            InventoryItemData invt_slot = GetItemSlot(islot);
            InventoryItemData invt_equip = GetEquippedItemSlot(eslot);
            ItemData idata = ItemData.Get(invt_slot?.item_id);
            ItemData edata = ItemData.Get(invt_equip?.item_id);
            if (invt_slot.quantity > 0 && idata != null && eslot >= 0)
            {
                if (edata == null)
                {
                    //Equip only
                    EquipItem(eslot, idata.id, invt_slot.durability);
                    RemoveItemAt(islot, 1);
                }
                else if (invt_slot.quantity == 1 && idata.type == ItemType.Equipment)
                {
                    //Swap
                    RemoveItemAt(islot, 1);
                    UnequipItem(eslot);
                    EquipItem(eslot, idata.id, invt_slot.durability);
                    AddItemAt(edata.id, islot, 1, invt_equip.durability);
                }
            }
        }

        public void UnequipItemTo(int eslot, int islot)
        {
            InventoryItemData invt_slot = GetItemSlot(islot);
            InventoryItemData invt_equip = GetEquippedItemSlot(eslot);
            ItemData idata = ItemData.Get(invt_slot?.item_id);
            ItemData edata = ItemData.Get(invt_equip?.item_id);
            if (edata != null)
            {
                bool same_item = idata != null && invt_slot != null && invt_slot.quantity > 0 && idata.id == edata.id && invt_slot.quantity < idata.inventory_max;
                bool slot_empty = invt_slot == null || invt_slot.quantity <= 0;
                if (same_item || slot_empty)
                {
                    //Unequip
                    UnequipItem(eslot);
                    AddItemAt(edata.id, islot, 1, invt_equip.durability);
                }
                else if (idata != null && invt_slot != null && !same_item && idata.type == ItemType.Equipment && idata.equip_slot == edata.equip_slot && invt_slot.quantity == 1)
                {
                    //swap
                    RemoveItemAt(islot, 1);
                    UnequipItem(eslot);
                    EquipItem(eslot, idata.id, invt_slot.durability);
                    AddItemAt(edata.id, islot, 1, invt_equip.durability);
                }
            }
        }

        public void EquipItemFromStorage(string box_uid, int sslot, int eslot)
        {
            StoredItemData store_data = GetStoredData(box_uid);
            if (store_data != null)
            {
                InventoryItemData invt_slot = GetStoredItemSlot(store_data, sslot);
                InventoryItemData invt_equip = GetEquippedItemSlot(eslot);
                ItemData idata = ItemData.Get(invt_slot?.item_id);
                ItemData edata = ItemData.Get(invt_equip?.item_id);
                if (invt_slot.quantity > 0 && idata != null && eslot >= 0)
                {
                    if (edata == null)
                    {
                        //Equip only
                        EquipItem(eslot, idata.id, invt_slot.durability);
                        RemoveStoredItemAt(box_uid, sslot, 1);
                    }
                    else if (invt_slot.quantity == 1 && idata.type == ItemType.Equipment)
                    {
                        //Swap
                        RemoveStoredItemAt(box_uid, sslot, 1);
                        UnequipItem(eslot);
                        EquipItem(eslot, idata.id, invt_slot.durability);
                        AddStoredItemAt(box_uid, edata.id, sslot, 1, invt_equip.durability);
                    }
                }
            }
        }

        public void UnequipItemToStorage(string box_uid, int eslot, int sslot)
        {
            StoredItemData store_data = GetStoredData(box_uid);
            if (store_data != null)
            {
                InventoryItemData invt_slot = GetStoredItemSlot(store_data, sslot);
                InventoryItemData invt_equip = GetEquippedItemSlot(eslot);
                ItemData idata = ItemData.Get(invt_slot?.item_id);
                ItemData edata = ItemData.Get(invt_equip?.item_id);
                if (edata != null)
                {
                    bool same_item = idata != null && invt_slot != null && invt_slot.quantity > 0 && idata.id == edata.id && invt_slot.quantity < idata.inventory_max;
                    bool slot_empty = invt_slot == null || invt_slot.quantity <= 0;
                    if (same_item || slot_empty)
                    {
                        //Unequip
                        UnequipItem(eslot);
                        AddStoredItemAt(box_uid, edata.id, sslot, 1, invt_equip.durability);
                    }
                    else if (idata != null && invt_slot != null && !same_item && idata.type == ItemType.Equipment && idata.equip_slot == edata.equip_slot && invt_slot.quantity == 1)
                    {
                        //swap
                        RemoveStoredItemAt(box_uid, sslot, 1);
                        UnequipItem(eslot);
                        EquipItem(eslot, idata.id, invt_slot.durability);
                        AddStoredItemAt(box_uid, edata.id, sslot, 1, invt_equip.durability);
                    }
                }
            }
        }

        public void EquipItem(int eslot, string item_id, float durability)
        {
            InventoryItemData idata = new InventoryItemData(item_id, 1, durability);
            equipped_items[eslot] = idata;
        }

        public void UnequipItem(int eslot)
        {
            if (equipped_items.ContainsKey(eslot))
                equipped_items.Remove(eslot);
        }

        public void AddEquipDurability(int eslot, float value)
        {
            if (equipped_items.ContainsKey(eslot))
            {
                InventoryItemData invdata = equipped_items[eslot];
                invdata.durability += value;
            }
        }

        public InventoryItemData GetEquippedItemSlot(int eslot)
        {
            if (equipped_items.ContainsKey(eslot))
                return equipped_items[eslot];
            return null;
        }

        //---- Stored items -----

        public StoredItemData GetStoredData(string box_uid)
        {
            StoredItemData sdata = null;
            if (!string.IsNullOrEmpty(box_uid))
            {
                if (stored_items.ContainsKey(box_uid))
                {
                    sdata = stored_items[box_uid];
                }
                else
                {
                    sdata = new StoredItemData();
                    stored_items[box_uid] = sdata;
                }
            }
            return sdata;
        }

        public InventoryItemData GetStoredItemSlot(StoredItemData sdata, int slot)
        {
            if (sdata.items.ContainsKey(slot))
                return sdata.items[slot];
            return null;
        }

        public void AddStoredItemAt(string box_uid, string item_id, int slot, int quantity, float durability)
        {
            StoredItemData sdata = GetStoredData(box_uid);
            InventoryItemData invt_slot = GetStoredItemSlot(sdata, slot);
            if (invt_slot != null && invt_slot.item_id == item_id)
            {
                int amount = invt_slot.quantity + quantity;
                float durabi = ((invt_slot.durability * invt_slot.quantity) + (durability * quantity)) / (float)amount;
                sdata.items[slot] = new InventoryItemData(item_id, amount, durabi);
            }
            else if (invt_slot == null || invt_slot.quantity <= 0)
            {
                sdata.items[slot] = new InventoryItemData(item_id, quantity, durability);
            }
        }

        public void RemoveStoredItemAt(string box_uid, int slot, int quantity)
        {
            StoredItemData sdata = GetStoredData(box_uid);
            InventoryItemData invt_slot = GetStoredItemSlot(sdata, slot);
            if (invt_slot != null && invt_slot.quantity > 0)
            {
                int amount = invt_slot.quantity - quantity;
                if (amount <= 0)
                    sdata.items.Remove(slot);
                else
                    sdata.items[slot] = new InventoryItemData(invt_slot.item_id, amount, invt_slot.durability);
            }
        }

        public void SwapStoredItemSlots(string box_uid, int slot1, int slot2)
        {
            StoredItemData sdata = GetStoredData(box_uid);
            InventoryItemData invt_slot1 = GetStoredItemSlot(sdata, slot1);
            InventoryItemData invt_slot2 = GetStoredItemSlot(sdata, slot2);
            sdata.items[slot1] = invt_slot2;
            sdata.items[slot2] = invt_slot1;

            if (invt_slot2 == null)
                sdata.items.Remove(slot1);
            if (invt_slot1 == null)
                sdata.items.Remove(slot2);
        }

        public void SwapStoredItemWithInventory(string box_uid, int islot, int sslot)
        {
            StoredItemData storedata = GetStoredData(box_uid);
            InventoryItemData invt_slot1 = GetItemSlot(islot);
            InventoryItemData invt_slot2 = GetStoredItemSlot(storedata, sslot);
            inventory[islot] = invt_slot2;
            storedata.items[sslot] = invt_slot1;

            if (invt_slot2 == null)
                inventory.Remove(islot);
            if (invt_slot1 == null)
                storedata.items.Remove(sslot);
        }

        public void RemoveStorage(string box_uid)
        {
            if (stored_items.ContainsKey(box_uid))
                stored_items.Remove(box_uid);
        }

        //---- Constructions and Plants and Characters

        public BuiltConstructionData AddConstruction(string construct_id, string scene, Vector3 pos, Quaternion rot, float durability)
        {
            BuiltConstructionData citem = new BuiltConstructionData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.construction_id = construct_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            citem.durability = durability;
            built_constructions[citem.uid] = citem;
            return citem;
        }

        public void RemoveConstruction(string uid)
        {
            if (built_constructions.ContainsKey(uid))
                built_constructions.Remove(uid);
        }

        public BuiltConstructionData GetConstructed(string uid)
        {
            if (built_constructions.ContainsKey(uid))
                return built_constructions[uid];
            return null;
        }

        public SowedPlantData AddPlant(string plant_id, string scene, Vector3 pos, Quaternion rot, int stage)
        {
            SowedPlantData citem = new SowedPlantData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.plant_id = plant_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            citem.growth_stage = stage;
            sowed_plants[citem.uid] = citem;
            return citem;
        }

        public void GrowPlant(string plant_uid, int stage)
        {
            if (sowed_plants.ContainsKey(plant_uid))
                sowed_plants[plant_uid].growth_stage = stage;
        }

        public void RemovePlant(string uid)
        {
            if (sowed_plants.ContainsKey(uid))
                sowed_plants.Remove(uid);
        }

        public SowedPlantData GetSowedPlant(string uid)
        {
            if (sowed_plants.ContainsKey(uid))
                return sowed_plants[uid];
            return null;
        }

        public TrainedCharacterData AddCharacter(string character_id, string scene, Vector3 pos, Quaternion rot)
        {
            TrainedCharacterData citem = new TrainedCharacterData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.character_id = character_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            trained_characters[citem.uid] = citem;
            return citem;
        }

        public TrainedCharacterData AddCharacterUID(string uid, string character_id, string scene, Vector3 pos, Quaternion rot)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                TrainedCharacterData citem = new TrainedCharacterData();
                citem.uid = uid;
                citem.character_id = character_id;
                citem.scene = scene;
                citem.pos = pos;
                citem.rot = rot;
                trained_characters[citem.uid] = citem;
                return citem;
            }
            return null;
        }

        public void RemoveCharacter(string uid)
        {
            if (trained_characters.ContainsKey(uid))
                trained_characters.Remove(uid);
        }

        public TrainedCharacterData GetCharacter(string uid)
        {
            if (trained_characters.ContainsKey(uid))
                return trained_characters[uid];
            return null;
        }

        // ---- Unlock groups -----

        public void UnlockID(string id)
        {
            if (!string.IsNullOrEmpty(id) && !unlocked_ids.ContainsKey(id))
                unlocked_ids[id] = true;
        }

        public void RemoveUnlockedID(string id)
        {
            if (unlocked_ids.ContainsKey(id))
                unlocked_ids.Remove(id);
        }

        public bool IsIDUnlocked(string id)
        {
            return unlocked_ids.ContainsKey(id);
        }

        //---- Destructibles -----

        public void RemoveObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                removed_objects[uid] = 1;
        }

        public bool IsObjectRemoved(string uid)
        {
            if (removed_objects.ContainsKey(uid))
                return removed_objects[uid] > 0;
            return false;
        }

        // ---- Unique Ids (Custom data) ----
        public void SetUniqueID(string unique_id, int val)
        {
            if (!string.IsNullOrEmpty(unique_id))
            {
                if (!unique_ids.ContainsKey(unique_id))
                    unique_ids[unique_id] = val;
            }
        }

        public void RemoveUniqueID(string unique_id)
        {
            if (unique_ids.ContainsKey(unique_id))
                unique_ids.Remove(unique_id);
        }

        public int GetUniqueID(string unique_id)
        {
            if (unique_ids.ContainsKey(unique_id))
                return unique_ids[unique_id];
            return 0;
        }

        public bool HasUniqueID(string unique_id)
        {
            return unique_ids.ContainsKey(unique_id);
        }

        //--- Attributes ----

        public bool HasAttribute(AttributeType type)
        {
            return attributes.ContainsKey(type);
        }

        public float GetAttributeValue(AttributeType type)
        {
            if (attributes.ContainsKey(type))
                return attributes[type];
            return 0f;
        }

        public void SetAttributeValue(AttributeType type, float value)
        {
            attributes[type] = value;
        }

        public void AddAttributeValue(AttributeType type, float value, float max)
        {
            if (!attributes.ContainsKey(type))
                attributes[type] = value;
            else
                attributes[type] += value;

            attributes[type] = Mathf.Clamp(attributes[type], 0f, max);
        }

        public void AddTimedBonus(BonusType type, float value, float duration)
        {
            TimedBonusData new_bonus = new TimedBonusData();
            new_bonus.bonus = type;
            new_bonus.value = value;
            new_bonus.time = duration;

            if (!timed_bonus_effects.ContainsKey(type) || timed_bonus_effects[type].time < duration)
                timed_bonus_effects[type] = new_bonus;
        }

        public void RemoveTimedBonus(BonusType type)
        {
            if (timed_bonus_effects.ContainsKey(type))
                timed_bonus_effects.Remove(type);
        }

        public float GetTotalTimedBonus(BonusType type)
        {
            if (timed_bonus_effects.ContainsKey(type) && timed_bonus_effects[type].time > 0f)
                return timed_bonus_effects[type].value;
            return 0f;
        }

        public bool IsWorldGenerated(){
            return world_seed != 0;
        }

        //--- Save / load -----

        public static void NewGame()
        {
            NewGame("player"); //default name
        }

        public static PlayerData NewGame(string name)
        {
            SaveSystem.Unload();
            player_data = new PlayerData(name);
            player_data.FixData();
            return player_data;
        }

        public void Save()
        {
            last_save = System.DateTime.Now;
            version = VERSION;
            SaveSystem.Save(filename, player_data);
        }

        public void Restart()
        {
            player_data = new PlayerData(filename);
            player_data.FixData();
        }

        public static void Unload()
        {
            player_data = null;
            SaveSystem.Unload();
        }

        public void Delete()
        {
            SaveSystem.Delete(filename);
            player_data = new PlayerData(filename);
        }

        public bool IsVersionValid()
        {
            return version == VERSION;
        }

        public static PlayerData LoadLast()
        {
            string name = SaveSystem.GetLastSave();
            if (string.IsNullOrEmpty(name))
                name = "player"; //Default name
            return Load(name);
        }

        public static PlayerData Load(string name)
        {
            if (player_data == null)
                player_data = SaveSystem.Load(name);
            if (player_data == null)
                player_data = new PlayerData(name);
            player_data.FixData();
            return player_data;
        }

        public static PlayerData Get()
        {
            return player_data;
        }
    }

}