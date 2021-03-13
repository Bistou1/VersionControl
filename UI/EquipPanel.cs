using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Shows currently equipped items
    /// </summary>

    public class EquipPanel : ItemSlotPanel
    {
        private PlayerUI parent_ui;

        private static EquipPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            parent_ui = GetComponentInParent<PlayerUI>();

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
                    SetInventory(InventoryType.Equipment, player.EquipData.uid, 99); //Size not important for equip inventory
                    SetPlayer(parent_ui.GetPlayer());
                    Show(true);
                }
            }
        }

        private void OnSelectSlot(ItemSlot islot)
        {

        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
           
        }

        public static EquipPanel Get()
        {
            return _instance;
        }
    }

}