using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Shows currently equipped items
    /// </summary>

    public class EquipBar : MonoBehaviour
    {
        public ItemSlot[] slots;

        public UnityAction<ItemSlot> onClickSlot;
        public UnityAction<ItemSlot> onRightClickSlot;

        private int selected_slot = -1;
        private int selected_right_slot = -1;

        private float timer = 0f;

        private static EquipBar _instance;

        void Awake()
        {
            _instance = this;

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; //Important to copy so not overwritten in loop
                slots[i].index = index;
                slots[i].is_equip = true;
                slots[i].onClick += (CraftData item) => { OnClickSlot(index, item); };
                slots[i].onClickRight += (CraftData item) => { OnClickSlotRight(index, item); };
                slots[i].onClickLong += (CraftData item) => { OnClickSlotRight(index, item); };
                slots[i].onClickDouble += (CraftData item) => { OnClickSlotRight(index, item); };
            }
        }

        void Start()
        {
            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer > 0.1f)
            {
                timer = 0f;
                SlowUpdate();
            }
        }

        //Slow update, better for performance when refresh not needed each frame
        void SlowUpdate()
        {
            RefreshEquip();
        }

        private void RefreshEquip()
        {
            PlayerData pdata = PlayerData.Get();
            for (int i = 0; i < slots.Length; i++)
            {
                InventoryItemData inv_data = pdata.GetEquippedItemSlot(i);
                ItemData idata = ItemData.Get(inv_data?.item_id);
                if (inv_data != null && idata != null)
                {
                    slots[i].SetSlot(idata, 1, inv_data.durability, selected_slot == i || selected_right_slot == i);
                }
                else
                {
                    slots[i].SetSlot(null, 0, 0f, false);
                }
            }
        }

        private void OnClickSlot(int slot, CraftData item)
        {
            PlayerData pdata = PlayerData.Get();
            PlayerControlsMouse controls = PlayerControlsMouse.Get();

            ItemSlot cslot = GetSlot(slot); //click slot
            ItemSlot selslot = TheUI.Get().GetSelectedItemSlot();

            int previous_right_select = selected_right_slot;
            ActionSelectorUI.Get().Hide();
            selected_right_slot = -1;

            //Cancel action selector
            if (slot == previous_right_select)
            {
                CancelSelection();
                return;
            }

            //Merge/swap items
            if (selslot != null && cslot != null)
            {
                ItemSlot slot1 = cslot;
                ItemSlot slot2 = selslot;
                ItemData item1 = slot1.GetItem();
                ItemData item2 = slot2.GetItem();
                MAction action1 = item1 != null ? item1.FindMergeAction(item2) : null;
                MAction action2 = item2 != null ? item2.FindMergeAction(item1) : null;

                if (item1 != null && item2 != null)
                {
                    //Same slot, cancel select
                    if (slot1 == slot2)
                    {
                        CancelSelection();
                        return;
                    }
                    //Else, use merge action
                    else if (action1 != null && action1.CanDoAction(PlayerCharacter.Get(), slot2))
                    {
                        DoMergeAction(action1, slot1, slot2);
                        return;
                    }

                    else if (action2 != null && action2.CanDoAction(PlayerCharacter.Get(), slot1))
                    {
                        DoMergeAction(action2, slot2, slot1);
                        return;
                    }
                }

                //Swap
                if (item1 != item2)
                {
                    if (item2 != null && item2.type == ItemType.Equipment)
                    {
                        SwapSlot(slot1, slot2);
                        return;
                    }
                }
            }

            if (item != null)
            {
                TheUI.Get().CancelSelection();
                selected_slot = slot;

                if (onClickSlot != null && cslot != null)
                    onClickSlot.Invoke(cslot);
            }
        }

        private void OnClickSlotRight(int slot, CraftData item)
        {
            selected_slot = -1;
            selected_right_slot = -1;
            ActionSelectorUI.Get().Hide();

            if (item != null && item.GetItem() != null && item.GetItem().actions.Length > 0)
            {
                selected_right_slot = slot;
                ActionSelectorUI.Get().Show(PlayerCharacter.Get(), slots[slot]);
            }

            ItemSlot eslot = GetSlot(slot);
            if (onRightClickSlot != null && eslot != null)
                onRightClickSlot.Invoke(eslot);
        }

        public void SwapSlot(ItemSlot slot_click, ItemSlot slot_other)
        {
            if (slot_click == null || slot_other == null || slot_click.type != ItemSlotType.Equipment)
                return;

            PlayerData pdata = PlayerData.Get();
            ItemData item2 = slot_other.GetItem();

            if (item2.type != ItemType.Equipment)
                return;

            //Swap with storage
            if (slot_other.type == ItemSlotType.Storage)
            {
                pdata.EquipItemFromStorage(StorageBar.Get().GetStorageUID(), slot_other.index, slot_click.index);
                TheUI.Get().CancelSelection();
            }
            //Swap with inventory
            else if (slot_other.type == ItemSlotType.Inventory)
            {
                pdata.EquipItemTo(slot_other.index, slot_click.index);
                TheUI.Get().CancelSelection();
            }
        }

        public void DoMergeAction(MAction action, ItemSlot slot_action, ItemSlot slot_other)
        {
            if (slot_action == null || slot_other == null)
                return;

            action.DoAction(PlayerCharacter.Get(), slot_action, slot_other);
            TheUI.Get().CancelSelection();
        }

        public void CancelSelection()
        {
            selected_slot = -1;
        }

        public bool HasSlotSelected()
        {
            return selected_slot >= 0;
        }

        public int GetSelectedSlotIndex()
        {
            return selected_slot;
        }

        public ItemSlot GetSlot(int index)
        {
            if (index >= 0 && index < slots.Length)
                return slots[index];
            return null;
        }

        public ItemSlot GetSelectedSlot()
        {
            if (selected_slot >= 0 && selected_slot < slots.Length)
                return slots[selected_slot];
            return null;
        }

        public static EquipBar Get()
        {
            return _instance;
        }
    }

}