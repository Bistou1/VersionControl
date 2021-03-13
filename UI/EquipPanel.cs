﻿using System.Collections;
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

        private static List<EquipPanel> panel_list = new List<EquipPanel>();

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            parent_ui = GetComponentInParent<PlayerUI>();

            Hide(true);
        }

        private void OnDestroy()
        {
            panel_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

        }

        public override void InitPanel()
        {
            if (!IsInventorySet())
            {
                PlayerCharacter player = parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
                if (player != null)
                {
                    SetInventory(InventoryType.Equipment, player.EquipData.uid, 99); //Size not important for equip inventory
                    SetPlayer(player);
                    Show(true);
                }
            }
        }

        protected override void RefreshPanel()
        {
            InventoryData inventory = GetInventory();

            if (inventory != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    EquipSlotUI slot = (EquipSlotUI)slots[i];
                    if (slot != null)
                    {
                        InventoryItemData invdata = inventory.GetItem((int)slot.equip_slot);
                        ItemData idata = ItemData.Get(invdata?.item_id);

                        if (invdata != null && idata != null)
                        {
                            slot.SetSlot(idata, invdata.quantity, selected_slot == slot.index || selected_right_slot == slot.index);
                            slot.SetDurability(idata.GetDurabilityPercent(invdata.durability), ShouldShowDurability(idata, invdata.durability));
                            slot.SetFilter(GetFilterLevel(idata, invdata.durability));
                        }
                        else
                        {
                            slot.SetSlot(null, 0, false);
                        }
                    }
                }
            }
        }

        public static EquipPanel Get(int player_id=0)
        {
            foreach (EquipPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<EquipPanel> GetAll()
        {
            return panel_list;
        }
    }

}