using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Main UI panel for storages boxes (chest)
    /// </summary>

    public class StorageBar : UIPanel
    {
        public ItemSlot[] slots;

        public UnityAction<ItemSlot> onClickSlot;
        public UnityAction<ItemSlot> onRightClickSlot;

        private string storage_uid;
        private int max_items;

        private int selected_slot = -1;
        private int selected_right_slot = -1;

        private float timer = 0f;

        private static StorageBar _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; //Important to copy so not overwritten in loop
                slots[i].index = index;
                slots[i].onClick += (CraftData item) => { OnClickSlot(index, item); };
                slots[i].onClickRight += (CraftData item) => { OnClickSlotRight(index, item); };
                slots[i].onClickLong += (CraftData item) => { OnClickSlotRight(index, item); };
                slots[i].onClickDouble += (CraftData item) => { OnClickSlotRight(index, item); };
            }
        }

        protected override void Start()
        {
            base.Start();
            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };
        }

        protected override void Update()
        {
            base.Update();

            if (IsVisible())
            {
                timer += Time.deltaTime;
                if (timer > 0.1f)
                {
                    timer = 0f;
                    SlowUpdate();
                }
            }
        }

        //Slow update, better for performance when refresh not needed each frame
        void SlowUpdate()
        {
            RefreshStorage();

            //Hide if too far
            PlayerCharacter character = PlayerCharacter.Get();
            Selectable select = Selectable.GetByUID(storage_uid);
            if (IsVisible() && character != null && select != null)
            {
                float dist = (select.transform.position - character.transform.position).magnitude;
                if (dist > select.use_range * 1.2f)
                {
                    Hide();
                }
            }
        }

        private void RefreshStorage()
        {
            PlayerData pdata = PlayerData.Get();
            StoredItemData stored_box = pdata.GetStoredData(storage_uid);
            if (stored_box != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    InventoryItemData invdata = pdata.GetStoredItemSlot(stored_box, i);
                    ItemData idata = ItemData.Get(invdata?.item_id);
                    if (invdata != null && idata != null)
                    {
                        slots[i].SetSlot(idata, invdata.quantity, invdata.durability, selected_slot == i || selected_right_slot == i);
                    }
                    else if (i < max_items)
                    {
                        slots[i].SetSlot(null, 0, 0f, false);
                    }
                    else
                    {
                        slots[i].Hide();
                    }
                }
            }
        }

        public void ShowStorage(string uid, int max)
        {
            storage_uid = uid;
            max_items = max;
            RefreshStorage();
            Show();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            storage_uid = "";
            CancelSelection();
        }

        private void OnClickSlot(int slot, CraftData item)
        {
            PlayerData pdata = PlayerData.Get();
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

                if (item1 != null && item2 != null)
                {
                    //Same slot, cancel select
                    if (slot1 == slot2)
                    {
                        CancelSelection();
                        return;
                    }
                    //Same item, combine stacks
                    else if (item1 == item2)
                    {
                        CombineStacks(slot1, slot2);
                        return;
                    }
                }

                //Swap
                if (item1 != item2)
                {
                    SwapSlot(slot1, slot2);
                    return;
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

            /*if (item != null && item.GetItem() != null && item.GetItem().actions.Length > 0)
            {
                selected_right_slot = slot;
                ActionSelectorUI.Get().Show(PlayerCharacter.Get(), slots[slot]);
            }*/

            ItemSlot cslot = GetSlot(slot);
            if (onRightClickSlot != null && cslot != null)
                onRightClickSlot.Invoke(cslot);
        }

        public void CombineStacks(ItemSlot slot_click, ItemSlot slot_other)
        {
            if (slot_click == null || slot_other == null || slot_click.type != ItemSlotType.Storage)
                return;

            PlayerData pdata = PlayerData.Get();
            ItemData item1 = slot_click.GetItem();
            ItemData item2 = slot_other.GetItem();

            if (slot_click.GetQuantity() + slot_other.GetQuantity() <= item1.inventory_max)
            {
                int quantity = slot_other.GetQuantity();
                if (slot_other.type == ItemSlotType.Inventory)
                {
                    pdata.RemoveItemAt(slot_other.index, quantity);
                    pdata.AddStoredItemAt(GetStorageUID(), item1.id, slot_click.index, quantity, slot_other.GetDurability());
                    InventoryBar.Get().CancelSelection();
                }
                if (slot_other.type == ItemSlotType.Storage)
                {
                    pdata.RemoveStoredItemAt(GetStorageUID(), slot_other.index, quantity);
                    pdata.AddStoredItemAt(GetStorageUID(), item1.id, slot_click.index, quantity, slot_other.GetDurability());
                    CancelSelection();
                }
            }
        }

        public void SwapSlot(ItemSlot slot_click, ItemSlot slot_other)
        {
            if (slot_click == null || slot_other == null || slot_click.type != ItemSlotType.Storage)
                return;

            PlayerData pdata = PlayerData.Get();

            //Equip/unequip
            if (slot_other.type == ItemSlotType.Equipment)
            {
                pdata.UnequipItemToStorage(GetStorageUID(), slot_other.index, slot_click.index);
                TheUI.Get().CancelSelection();
            }
            //Swap item with storage
            else if (slot_other.type == ItemSlotType.Storage)
            {
                pdata.SwapStoredItemSlots(GetStorageUID(), slot_click.index, slot_other.index);
                CancelSelection();
            }
            //Swap two items in inventory
            else if (slot_other.type == ItemSlotType.Inventory)
            {
                pdata.SwapStoredItemWithInventory(GetStorageUID(), slot_other.index, slot_click.index);
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
            selected_right_slot = -1;
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

        public Vector3 GetSlotWorldPosition(int slot)
        {
            if (slot >= 0 && slot < slots.Length)
            {
                RectTransform slotRect = slots[slot].GetRect();
                return slotRect.position;
            }
            return Vector3.zero;
        }

        public string GetStorageUID()
        {
            return storage_uid;
        }

        public static StorageBar Get()
        {
            return _instance;
        }
    }

}