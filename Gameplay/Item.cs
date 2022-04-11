using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Items are objects that can be picked, dropped and held into the player's inventory. Some item can also be crafted or used as crafting material.
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Item : MonoBehaviour
    {
        [Header("Item")]
        public ItemData data;
        public int quantity = 1;

        [Header("FX")]
        public bool snap_to_ground = true; //If true, item will be automatically placed on the ground instead of floating if spawns in the air
        public AudioClip take_audio;
        public GameObject take_fx;

        [HideInInspector]
        public bool was_spawned = false; //If true, item was dropped by the player, or loaded from save file

        private Selectable selectable;
        private UniqueID unique_id;

        private static List<Item> item_list = new List<Item>();

        void Awake()
        {
            item_list.Add(this);
            selectable = GetComponent<Selectable>();
            unique_id = GetComponent<UniqueID>();
            selectable.onUse += OnUse;
        }

        private void OnDestroy()
        {
            item_list.Remove(this);
        }

        private void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            if (snap_to_ground)
            {
                float dist;
                bool grounded = DetectGrounded(out dist);
                if (!grounded)
                {
                    transform.position += Vector3.down * dist;
                }
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (was_spawned && selectable.IsActive())
            {
                PlayerData pdata = PlayerData.Get();
                DroppedItemData dropped_item = pdata.GetDroppedItem(GetUID());
                if (dropped_item != null)
                {
                    if (data.HasDurability() && dropped_item.durability <= 0f)
                        DestroyItem(); //Destroy item from durability
                }
            }
        }

        private void OnUse(PlayerCharacter character)
        {
            //Take
            character.TakeItem(this);
        }

        public void TakeItem()
        {
            PlayerData pdata = PlayerData.Get();
            if (CanTakeItem())
            {
                DroppedItemData dropped_item = pdata.GetDroppedItem(GetUID());
                float durability = dropped_item != null ? dropped_item.durability : data.durability;
                int slot = pdata.AddItem(data.id, quantity, durability); //Add to inventory

                DestroyItem();

                //Take fx
                ItemTakeFX.DoTakeFX(transform.position, data, slot);

                TheAudio.Get().PlaySFX("item", take_audio);
                if (take_fx != null)
                    Instantiate(take_fx, transform.position, Quaternion.identity);
            }
        }

        //Destroy content but keep container
        public void SpoilItem()
        {
            if (data.container_data)
            {
                Item.Create(data.container_data, transform.position, quantity, data.container_data.durability);
            }
            DestroyItem();
        }

        public void DestroyItem()
        {
            PlayerData pdata = PlayerData.Get();
            if (was_spawned)
                pdata.RemoveDroppedItem(GetUID()); //Removed from dropped items
            else
                pdata.RemoveObject(GetUID()); //Taken from map

            Destroy(gameObject);
        }

        public bool CanTakeItem()
        {
            PlayerData pdata = PlayerData.Get();
            return gameObject.activeSelf && pdata.CanTakeItem(data.id, quantity);
        }

        private bool DetectGrounded(out float dist)
        {
            float radius = 20f;
            float offset = 0.5f;
            Vector3 center = transform.position + Vector3.up * offset;

            RaycastHit hd1, hf1;
            LayerMask everything = ~0;
            bool f1 = Physics.Raycast(center, Vector3.down, out hf1, offset + 0.1f, everything.value, QueryTriggerInteraction.Ignore);
            bool d1 = Physics.Raycast(center, Vector3.down, out hd1, radius + offset, everything.value, QueryTriggerInteraction.Ignore);
            dist = d1 ? hd1.distance - offset : 0f;
            return f1;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public static Item GetNearest(Vector3 pos, float range = 999f)
        {
            Item nearest = null;
            float min_dist = range;
            foreach (Item item in item_list)
            {
                float dist = (item.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = item;
                }
            }
            return nearest;
        }

        public static Item GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Item item in item_list)
                {
                    if (item.GetUID() == uid)
                        return item;
                }
            }
            return null;
        }

        public static List<Item> GetAll()
        {
            return item_list;
        }

        //Spawn an existing one in the save file (such as after loading)
        public static Item Spawn(string uid, Transform parent = null)
        {
            DroppedItemData ddata = PlayerData.Get().GetDroppedItem(uid);
            if (ddata != null)
            {
                ItemData idata = ItemData.Get(ddata.item_id);
                if (idata != null)
                {
                    GameObject build = Instantiate(idata.item_prefab, ddata.pos, idata.item_prefab.transform.rotation);
                    build.transform.parent = parent;

                    Item item = build.GetComponent<Item>();
                    item.data = idata;
                    item.was_spawned = true;
                    item.unique_id.unique_id = uid;
                    item.quantity = ddata.quantity;
                    return item;
                }
            }
            return null;
        }

        //Create a totally new one that will be added to save file
        public static Item Create(ItemData data, Vector3 pos, int quantity, float durability)
        {
            DroppedItemData ditem = PlayerData.Get().AddDroppedItem(data.id, SceneNav.GetCurrentScene(), pos, quantity, durability);
            GameObject obj = Instantiate(data.item_prefab, pos, data.item_prefab.transform.rotation);
            Item item = obj.GetComponent<Item>();
            item.data = data;
            item.was_spawned = true;
            item.unique_id.unique_id = ditem.uid;
            item.quantity = quantity;
            return item;
        }
    }

}