using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// FX that show the item going to the inventory when picked
    /// </summary>

    public class ItemTakeFX : MonoBehaviour
    {
        public SpriteRenderer icon;
        public float fx_speed = 10f;

        private Vector3 start_pos;
        private Vector3 start_scale;
        private InventoryType inventory_target;
        private int slot_target = -1;
        private float timer = 0f;

        private void Awake()
        {
            start_pos = transform.position;
            start_scale = transform.localScale;
        }

        void Start()
        {
            if (slot_target < 0)
                Destroy(gameObject);
        }

        void Update()
        {
            ItemSlotPanel panel = ItemSlotPanel.Get(inventory_target);

            if (panel != null)
            {
                Vector3 wPos = panel.GetSlotWorldPosition(slot_target);
                Vector3 dir = wPos - transform.position;
                Vector3 tDir = wPos - start_pos;
                float mdist = Mathf.Min(fx_speed * Time.deltaTime, dir.magnitude);
                float scale = dir.magnitude / tDir.magnitude;
                transform.position += dir.normalized * mdist;
                transform.localScale = start_scale * scale;
                transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up);

                if (dir.magnitude < 0.1f)
                    Destroy(gameObject);
            }

            timer += Time.deltaTime;
            if (timer > 2f)
                Destroy(gameObject);
        }

        public void SetItem(ItemData item, InventoryType inventory, int slot)
        {
            inventory_target = inventory;
            slot_target = slot;
            icon.sprite = item.icon;
        }

        public static void DoTakeFX(Vector3 pos, ItemData item, InventoryType inventory, int target_slot)
        {
            if (AssetData.Get().item_take_fx != null)
            {
                GameObject fx = Instantiate(AssetData.Get().item_take_fx, pos, Quaternion.identity);
                fx.GetComponent<ItemTakeFX>().SetItem(item, inventory, target_slot);
            }
        }
    }

}