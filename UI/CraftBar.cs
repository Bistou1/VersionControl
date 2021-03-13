using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// The top level crafting bar that contains all the crafting categories
    /// </summary>

    public class CraftBar : MonoBehaviour
    {
        public CategorySlot[] slots;

        public CraftSubBar sub_bar;

        public UnityAction onToggle;
        public UnityAction<CategorySlot> onClickSlot;
        public UnityAction<CategorySlot> onRightClickSlot;

        private Animator animator;

        private float timer = 0f;
        private int selected_slot = -1;
        private bool is_visible = false;

        private static CraftBar _instance;

        void Awake()
        {
            _instance = this;
            animator = GetComponent<Animator>();

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; //Important to copy so not overwritten in loop
                slots[i].slot_index = index;
                slots[i].onClick += (GroupData category) => { OnClickSlot(index, category); };
                slots[i].onClickRight += (GroupData category) => { OnClickSlotRight(index, category); };
            }

            if (animator != null)
                animator.SetBool("Visible", is_visible);
        }

        private void Start()
        {
            PlayerControlsMouse.Get().onClick += (Vector3) => { CancelSubSelection(); };
            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };
        }

        void Update()
        {
            if (is_visible)
            {
                timer += Time.deltaTime;
                if (timer > 0.5f)
                {
                    timer = 0f;
                    SlowUpdate();
                }
            }
        }

        void SlowUpdate()
        {
            foreach (CategorySlot slot in slots)
            {
                List<CraftData> items = CraftData.GetAllCraftableInGroup(slot.group);
                slot.gameObject.SetActive(items.Count > 0);
            }
        }

        public void ToggleBar()
        {
            is_visible = !is_visible;
            CancelSelection();
            if (animator != null)
                animator.SetBool("Visible", is_visible);
            if (sub_bar != null)
                sub_bar.Hide();

            if (onToggle != null)
                onToggle.Invoke();

            SlowUpdate();
        }

        public void SetVisible(bool visible)
        {
            if (!is_visible && visible)
                ToggleBar();
            else if (is_visible && !visible)
                ToggleBar();
        }

        private void OnClickSlot(int slot, GroupData category)
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].UnselectSlot();

            if (category == sub_bar.GetCurrentCategory())
            {
                sub_bar.Hide();
            }
            else
            {
                selected_slot = slot;
                slots[slot].SelectSlot();
                sub_bar.ShowCategory(category);
            }

            CategorySlot cslot = GetSlot(slot);
            if (onClickSlot != null && cslot != null)
                onClickSlot.Invoke(cslot);
        }

        private void OnClickSlotRight(int slot, GroupData category)
        {
            CategorySlot cslot = GetSlot(slot);
            if (onRightClickSlot != null && cslot != null)
                onRightClickSlot.Invoke(cslot);
        }

        public void CancelSubSelection()
        {
            sub_bar.CancelSelection();
        }

        public void CancelSelection()
        {
            selected_slot = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].UnselectSlot();
            }
            CancelSubSelection();
        }

        public bool HasSlotSelected()
        {
            return selected_slot >= 0;
        }

        public int GetSelectedSlotIndex()
        {
            return selected_slot;
        }

        public CategorySlot GetSlot(int index)
        {
            if (index >= 0 && index < slots.Length)
                return slots[index];
            return null;
        }

        public CategorySlot GetSelectedSlot()
        {
            if (selected_slot >= 0 && selected_slot < slots.Length)
                return slots[selected_slot];
            return null;
        }

        public static CraftBar Get()
        {
            return _instance;
        }
    }

}
