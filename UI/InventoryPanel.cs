using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Main Inventory bar that list all items in your inventory
    /// </summary>

    public class InventoryPanel : ItemSlotPanel
    {
        private PlayerUI parent_ui;

        private static InventoryPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            parent_ui = GetComponentInParent<PlayerUI>();

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].onPressKey += OnPressShortcut;
            }

            onSelectSlot += OnSelectSlot;
            onMergeSlot += OnMergeSlot;

            Hide(true);
        }

        protected override void Start()
        {
            base.Start();

            InitInventory();
        }

        public void InitInventory()
        {
            if (!IsInventorySet())
            {
                PlayerCharacter player = parent_ui.GetPlayer();
                if (player != null)
                {
                    SetInventory(InventoryType.Inventory, player.InventoryData.uid, player.Inventory.inventory_size);
                    SetPlayer(parent_ui.GetPlayer());
                    Show(true);
                }
            }
        }

        private void OnPressShortcut(UISlot slot)
        {
            CancelSelection();
            KeyClickSlot(slot.index, false);
        }

        private void OnSelectSlot(ItemSlot islot)
        {

        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
           
        }

        public static InventoryPanel Get()
        {
            return _instance;
        }
    }

}