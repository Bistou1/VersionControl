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

    public class CraftSubPanel : UISlotPanel
    {
        [Header("Craft Sub Panel")]
        public Text title;
        public Animator animator;

        private PlayerUI parent_ui;

        private GroupData current_category;

        private static CraftSubPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            parent_ui = GetComponentInParent<PlayerUI>();

            if (animator != null)
                animator.SetBool("Visible", IsVisible());
        }

        protected override void Start()
        {
            base.Start();

            onClickSlot += OnClick;
        }

        public void RefreshCraftPanel()
        {
            foreach (ItemSlot slot in slots)
                slot.Hide();

            if (current_category == null || !IsVisible())
                return;

            //Show all items of a category
            PlayerCharacter player = parent_ui.GetPlayer();
            if (player != null)
            {
                List<CraftData> items = CraftData.GetAllCraftableInGroup(parent_ui.GetPlayer(), current_category);

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
                        ItemSlot slot = (ItemSlot)slots[i];
                        slot.SetSlot(item, 1, false);
                        slot.AnimateGain();
                    }
                }
            }
        }

        public void ShowCategory(GroupData group)
        {
            Hide(true); //Instant hide to do show animation

            current_category = group;
            title.text = group.title;
            
            Show();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);

            ShowAnim(true);
            RefreshCraftPanel();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);

            current_category = null;
            CraftInfoPanel.Get().Hide();
            ShowAnim(false);

            if(instant && animator != null)
                animator.Rebind();
        }

        private void ShowAnim(bool visible)
        {
            SetVisible(visible);
            if (animator != null)
                animator.SetBool("Visible", IsVisible());
        }

        private void OnClick(UISlot uislot)
        {
            int slot = uislot.index;
            ItemSlot islot = (ItemSlot)uislot;
            CraftData item = islot.GetCraftable();

            foreach (ItemSlot aslot in slots)
                aslot.UnselectSlot();

            if (item == CraftInfoPanel.Get().GetData())
            {
                CraftInfoPanel.Get().Hide();
            }
            else
            {
                parent_ui.CancelSelection();
                slots[slot].SelectSlot();
                CraftInfoPanel.Get().ShowData(item);
            }
        }

        public void CancelSelection()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].UnselectSlot();
            CraftInfoPanel.Get().Hide();
        }

        public GroupData GetCurrentCategory()
        {
            return current_category;
        }

        public static CraftSubPanel Get()
        {
            return _instance;
        }
    }

}