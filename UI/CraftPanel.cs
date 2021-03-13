using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// The top level crafting bar that contains all the crafting categories
    /// </summary>

    public class CraftPanel : UISlotPanel
    {
        [Header("Craft Panel")]
        public Animator animator;

        private PlayerUI parent_ui;

        private CraftStation current_staton = null;
        private int selected_slot = -1;

        private List<GroupData> default_categories = new List<GroupData>();

        private static CraftPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            parent_ui = GetComponentInParent<PlayerUI>();

            for (int i = 0; i < slots.Length; i++)
            {
                CategorySlot cslot = (CategorySlot)slots[i];
                if (cslot.group)
                    default_categories.Add(cslot.group);
            }

            if (animator != null)
                animator.SetBool("Visible", IsVisible());
        }

        protected override void Start()
        {
            base.Start();

            PlayerControlsMouse.Get().onClick += (Vector3) => { CancelSubSelection(); };
            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };

            onClickSlot += OnClick;

            RefreshCategories();
        }

        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get();

            if (!controls.IsGamePad())
            {
                if (controls.IsPressAction() || controls.IsPressAttack())
                    CancelSubSelection();
            }
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            PlayerCharacter player = parent_ui.GetPlayer();
            if (player != null && IsVisible())
            {
                CraftStation station = player.Crafting.GetCraftStation();
                if (current_staton != station)
                {
                    current_staton = station;
                    RefreshCategories();
                }
            }
        }

        private void RefreshCategories()
        {
            foreach (CategorySlot slot in slots)
                slot.Hide();

            PlayerCharacter player = parent_ui.GetPlayer();
            if (player != null)
            {
                int index = 0;
                List<GroupData> groups = player.Crafting.GetCraftGroups();
                
                foreach (GroupData group in groups)
                {
                    if (index < slots.Length)
                    {
                        CategorySlot slot = (CategorySlot)slots[index];
                        List<CraftData> items = CraftData.GetAllCraftableInGroup(parent_ui.GetPlayer(), group);
                        if (items.Count > 0)
                        {
                            slot.SetSlot(group);
                            index++;
                        }
                    }
                }

                CraftSubPanel.Get().Hide();
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);

            CancelSelection();
            if (animator != null)
                animator.SetBool("Visible", IsVisible());
            CraftSubPanel.Get().Hide();

            RefreshCategories();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);

            CancelSelection();
            if (animator != null)
                animator.SetBool("Visible", IsVisible());
            CraftSubPanel.Get().Hide();
        }

        private void OnClick(UISlot uislot)
        {
            if (uislot != null)
            {
                CategorySlot cslot = (CategorySlot)uislot;

                for (int i = 0; i < slots.Length; i++)
                    slots[i].UnselectSlot();

                if (cslot.group == CraftSubPanel.Get().GetCurrentCategory())
                {
                    CraftSubPanel.Get().Hide();
                }
                else
                {
                    selected_slot = uislot.index;
                    uislot.SelectSlot();
                    CraftSubPanel.Get().ShowCategory(cslot.group);
                }
            }
        }

        public void CancelSubSelection()
        {
            CraftSubPanel.Get().CancelSelection();
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

        public int GetSelected()
        {
            return selected_slot;
        }

        public static CraftPanel Get()
        {
            return _instance;
        }
    }

}
