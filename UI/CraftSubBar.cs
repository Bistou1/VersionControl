using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivalEngine
{

    /// <summary>
    /// Second level crafting bar, that contains the items under a category
    /// </summary>

    public class CraftSubBar : MonoBehaviour
    {
        public Text title;
        public CraftInfoPanel craft_info;

        public ItemSlot[] slots;

        public UnityAction<ItemSlot> onClickSlot;
        public UnityAction<ItemSlot> onRightClickSlot;

        private Animator animator;

        private int selected_slot = -1;
        private bool is_visible = false;

        private GroupData current_category;

        private static CraftSubBar _instance;

        void Awake()
        {
            _instance = this;
            animator = GetComponent<Animator>();

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; //Important to copy so not overwritten in loop
                slots[i].index = index;
                slots[i].onClick += (CraftData item) => { OnClickSlot(index, item); };
                slots[i].onClickRight += (CraftData item) => { OnClickSlotRight(index, item); };
            }

            if (animator != null)
                animator.SetBool("Visible", is_visible);
        }

        void Update()
        {

        }

        public void RefreshPanel()
        {
            foreach (ItemSlot slot in slots)
                slot.Hide();

            if (current_category == null || !is_visible)
                return;

            //Show all items of a category
            List<CraftData> items = CraftData.GetAllCraftableInGroup(current_category);

            //Sort list
            items.Sort((p1, p2) =>
            {
                return (p1.craft_sort_order == p2.craft_sort_order)
                    ? p1.title.CompareTo(p2.title) : p1.craft_sort_order.CompareTo(p2.craft_sort_order);
            });

            for (int i = 0; i < items.Count; i++)
            {
                if (i < slots.Length)
                {
                    CraftData item = items[i];
                    ItemSlot slot = slots[i];
                    slot.SetSlot(item, 1, 0f, false);
                    slot.AnimateGain();
                }
            }
        }

        public void ShowCategory(GroupData group)
        {
            HideInstant(); //Instant hide to do show animation

            current_category = group;
            title.text = group.title;
            SetVisible(true);
            RefreshPanel();
        }

        public void Hide()
        {
            current_category = null;
            craft_info.Hide();
            SetVisible(false);
        }

        public void HideInstant()
        {
            current_category = null;
            craft_info.Hide();
            SetVisible(false);
            if (animator != null)
                animator.Rebind();
        }

        private void SetVisible(bool visible)
        {
            is_visible = visible;
            if (animator != null)
                animator.SetBool("Visible", is_visible);
        }

        private void OnClickSlot(int slot, CraftData item)
        {
            foreach (ItemSlot aslot in slots)
                aslot.UnselectSlot();

            if (item == craft_info.GetData())
            {
                craft_info.Hide();
            }
            else
            {
                TheUI.Get().CancelSelection();
                slots[slot].SelectSlot();
                craft_info.ShowData(item);
            }

            ItemSlot cslot = GetSlot(slot);
            if (onClickSlot != null && cslot != null)
                onClickSlot.Invoke(cslot);
        }

        private void OnClickSlotRight(int slot, CraftData item)
        {
            ItemSlot cslot = GetSlot(slot);
            if (onRightClickSlot != null && cslot != null)
                onRightClickSlot.Invoke(cslot);
        }

        public void CancelSelection()
        {
            selected_slot = -1;
            for (int i = 0; i < slots.Length; i++)
                slots[i].UnselectSlot();
            craft_info.Hide();
        }

        public bool HasSlotSelected()
        {
            return selected_slot >= 0;
        }

        public int GetSelectedSlotIndex()
        {
            return selected_slot;
        }

        public GroupData GetCurrentCategory()
        {
            return current_category;
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

        public static CraftSubBar Get()
        {
            return _instance;
        }
    }

}