using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Main UI panel for storages boxes (chest)
    /// </summary>

    public class StoragePanel : ItemSlotPanel
    {
        private PlayerCharacter player;

        private static StoragePanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            onSelectSlot += OnSelectSlot;
            onMergeSlot += OnMergeSlot;
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            //Hide if too far
            Selectable select = Selectable.GetByUID(inventory_uid);
            if (IsVisible() && player != null && select != null)
            {
                float dist = (select.transform.position - player.transform.position).magnitude;
                if (dist > select.use_range * 1.2f)
                {
                    Hide();
                }
            }
        }

        public void ShowStorage(PlayerCharacter player, string uid, int max)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                this.player = player;
                SetInventory(InventoryType.Storage, uid, max);
                SetPlayer(player);
                RefreshPanel();
                Show();
            }
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            SetInventory(InventoryType.Storage, "", 0);
            CancelSelection();
        }

        private void OnSelectSlot(ItemSlot islot)
        {

        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
           
        }

        public string GetStorageUID()
        {
            return inventory_uid;
        }

        public static StoragePanel Get()
        {
            return _instance;
        }
    }

}