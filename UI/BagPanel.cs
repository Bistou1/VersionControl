using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Main UI panel for storages boxes (chest)
    /// </summary>

    public class BagPanel : ItemSlotPanel
    {
        private PlayerUI parent_ui;

        private static BagPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            parent_ui = GetComponentInParent<PlayerUI>();

            onSelectSlot += OnSelectSlot;
            onMergeSlot += OnMergeSlot;
        }

        public void ShowBag(string uid, int max)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                SetInventory(InventoryType.Bag, uid, max);
                SetPlayer(parent_ui.GetPlayer());
                SetVisible(true);
            }
        }

        public void HideBag()
        {
            SetInventory(InventoryType.Bag, "", 0);
            SetVisible(false);
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

        public static BagPanel Get()
        {
            return _instance;
        }
    }

}