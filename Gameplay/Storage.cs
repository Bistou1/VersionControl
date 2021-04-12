using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Storage : MonoBehaviour
    {
        public int storage_size = 10;
        public ItemData[] starting_items;

        private UniqueID unique_id;

        private static List<Storage> storage_list = new List<Storage>();

        void Awake()
        {
            storage_list.Add(this);
            unique_id = GetComponent<UniqueID>();
        }

        private void OnDestroy()
        {
            storage_list.Remove(this);
        }

        private void Start()
        {
            //Add starting items
            if (!string.IsNullOrEmpty(unique_id.unique_id))
            {
                bool has_inventory = InventoryData.Exists(unique_id.unique_id);
                if (!has_inventory)
                {
                    InventoryData invdata = InventoryData.Get(InventoryType.Storage, unique_id.unique_id);
                    foreach (ItemData item in starting_items)
                    {
                        if (item != null)
                            invdata.AddItem(item.id, 1, item.durability, UniqueID.GenerateUniqueID());
                    }
                }
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

        }

        public void OpenStorage(PlayerCharacter player)
        {

            if (!string.IsNullOrEmpty(unique_id.unique_id))
                StoragePanel.Get(player.player_id).ShowStorage(player, unique_id.unique_id, storage_size);
            else
                Debug.LogError("You must generate the UID to use the storage feature.");

        }

        public static Storage GetNearest(Vector3 pos, float range=999f)
        {
            float min_dist = range;
            Storage nearest = null;
            foreach (Storage storage in storage_list)
            {
                float dist = (pos - storage.transform.position).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = storage;
                }
            }
            return nearest;
        }

        public static List<Storage> GetAll()
        {
            return storage_list;
        }
    }

}
