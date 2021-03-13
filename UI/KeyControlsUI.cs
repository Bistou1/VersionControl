using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Allow keyboard/gamepad to control the UI
    /// </summary>

    public class KeyControlsUI : MonoBehaviour
    {
        public UISlotPanel default_top;
        public UISlotPanel default_down;
        public UISlotPanel default_left;
        public UISlotPanel default_right;

        private UISlotPanel selected_panel = null;
        private UISlotPanel prev_select_panel = null;
        private UISlot prev_slot = null;

        private static KeyControlsUI _instance;

        void Awake()
        {
            _instance = this;
        }

        void Update()
        {
            PlayerControls controls = PlayerControls.Get();

            if (!controls.IsGamePad())
                return;

            if (controls.IsUIPressLeft())
                Navigate(Vector2.left);
            else if (controls.IsUIPressRight())
                Navigate(Vector2.right);
            else if(controls.IsUIPressUp())
                Navigate(Vector2.up);
            else if (controls.IsUIPressDown())
                Navigate(Vector2.down);

            if (!IsCraftPanelFocus() && CraftPanel.Get().IsVisible())
            {
                selected_panel = CraftPanel.Get();
                CraftInfoPanel.Get().Hide();
            }

            if (selected_panel == CraftPanel.Get() && CraftPanel.Get().IsVisible())
            {
                selected_panel.selection_index = Mathf.Clamp(selected_panel.selection_index, 0, selected_panel.CountActiveSlots() - 1);

                UISlot slot = selected_panel.GetSelectSlot();
                if (prev_slot != slot || !CraftSubPanel.Get().IsVisible())
                {
                    CraftPanel.Get().KeyClick();
                    CraftSubPanel.Get().selection_index = 0;
                }
            }

            else if (selected_panel == CraftSubPanel.Get() && CraftSubPanel.Get().IsVisible())
            {
                selected_panel.selection_index = Mathf.Clamp(selected_panel.selection_index, 0, selected_panel.CountActiveSlots() - 1);

                UISlot slot = selected_panel.GetSelectSlot();
                if (prev_slot != slot || !CraftInfoPanel.Get().IsVisible())
                {
                    CraftSubPanel.Get().KeyClick();
                }
            }

            if (TheGame.Get().IsPausedByPlayer())
                selected_panel = PausePanel.Get();
            else if (ActionSelectorUI.Get().IsVisible())
                selected_panel = ActionSelectorUI.Get();
            else if (ActionSelector.Get().IsVisible())
                selected_panel = ActionSelector.Get();

            if (selected_panel == CraftInfoPanel.Get())
            {
                PlayerCharacter player = CraftInfoPanel.Get().GetParentUI().GetPlayer();
                if (player.Crafting.GetCurrentBuildable() == null)
                    selected_panel = null;
            }
            else if (selected_panel == PausePanel.Get())
            {
                if (!TheGame.Get().IsPausedByPlayer())
                    selected_panel = null;
            }

            //If panel is invisible, stop focus on it
            if (selected_panel != null && !selected_panel.IsVisible())
                StopNavigate();
            else if (selected_panel != null && selected_panel.GetSelectSlot() != null && !selected_panel.GetSelectSlot().IsVisible())
                StopNavigate();

            //Controls
            if (controls.IsPressUISelect())
                OnPressSelect();

            if (controls.IsPressUIUse())
                OnPressUse();

            if (controls.IsPressUICancel())
                OnPressCancel();

            if (controls.IsPressAttack())
                OnPressAttack();

            prev_slot = selected_panel?.GetSelectSlot();
        }

        public void Navigate(Vector2 dir)
        {
            UISlot current = GetSelectedSlot();
            if (selected_panel == null || current == null)
            {
                if (IsLeft(dir))
                    selected_panel = default_left;
                else if (IsRight(dir))
                    selected_panel = default_right;
                else if (IsUp(dir))
                    selected_panel = default_top;
                else if (IsDown(dir))
                    selected_panel = default_down;
            }
            else 
            {
                if (IsLeft(dir) && current.left)
                    NavigateTo(current.left, dir);
                else if (IsRight(dir) && current.right)
                    NavigateTo(current.right, dir);
                else if (IsUp(dir) && current.top)
                    NavigateTo(current.top, dir);
                else if (IsDown(dir) && current.down)
                    NavigateTo(current.down, dir);
                else
                    NavigateAuto(dir);

            }
        }

        public void NavigateAuto(Vector2 dir)
        {
            if (selected_panel != null)
            {
                int slots_per_row = selected_panel.slots_per_row;
                int max_slot = selected_panel.CountActiveSlots();

                if (IsLeft(dir))
                    selected_panel.selection_index--;
                else if (IsRight(dir))
                    selected_panel.selection_index++;
                else if (IsUp(dir))
                    selected_panel.selection_index -= slots_per_row;
                else if (IsDown(dir))
                    selected_panel.selection_index += slots_per_row;

                //If outside of panel, stop focus on it
                if (!IsCraftPanelFocus())
                {
                    if (selected_panel.selection_index < 0 || selected_panel.selection_index >= max_slot)
                    {
                        selected_panel.selection_index = 0;
                        StopNavigate();
                    }
                }
            }
        }

        public void NavigateTo(UISlot slot, Vector2 dir)
        {
            selected_panel = slot?.GetParent();

            if (selected_panel != null)
                selected_panel.selection_index = slot.index;

            if (selected_panel != null && !slot.IsVisible())
                Navigate(dir);
        }

        public void StopNavigate()
        {
            ActionSelector.Get().Hide();
            ActionSelectorUI.Get().Hide();
            selected_panel = null;
        }

        private void OnPressSelect()
        {
            SlotType selected_type = GetSelectedType();

            if (selected_type == SlotType.Inventory)
            {
                InventoryPanel.Get().KeyClick();
            }

            else if (selected_type == SlotType.Equipment)
            {
                EquipPanel.Get().KeyClick();
            }

            else if(selected_type == SlotType.Storage)
            {
                StoragePanel.Get().KeyClick();
            }

            else if (selected_type == SlotType.ActionSelectorGame)
            {
                ActionSelector.Get().KeyClick();
                selected_type = SlotType.None;
            }

            else if(selected_type == SlotType.ActionSelectorUI)
            {
                ActionSelectorUI.Get().KeyClick();
                selected_panel = prev_select_panel;
            }

            else if (selected_type == SlotType.Craft)
            {
                if (CraftSubPanel.Get().IsVisible())
                {
                    selected_panel = CraftSubPanel.Get();
                    CraftSubPanel.Get().KeyClick();
                }
            }

            else if (selected_type == SlotType.CraftSub)
            {
                if (CraftInfoPanel.Get().IsVisible())
                {
                    PlayerCharacter player = CraftInfoPanel.Get().GetParentUI().GetPlayer();
                    CraftInfoPanel.Get().OnClickCraft();
                    if (player.Crafting.IsBuildMode())
                    {
                        selected_type = SlotType.CraftInfo;
                        CraftPanel.Get().Hide();
                    }
                }
            }

            else if (selected_type == SlotType.CraftInfo)
            {
                //CraftInfoPanel.Get().OnClickCraft();
            }

            if (ReadPanel.Get().IsFullyVisible())
                ReadPanel.Get().Hide();
        }

        private void OnPressUse()
        {
            SlotType selected_type = GetSelectedType();

            if (selected_type == SlotType.Inventory)
            {
                prev_select_panel = selected_panel;
                InventoryPanel.Get().KeyClick(true);
            }

            else if(selected_type == SlotType.Equipment)
            {
                prev_select_panel = selected_panel;
                EquipPanel.Get().KeyClick(true);
            }

            else if(selected_type == SlotType.ActionSelectorGame)
            {
                ActionSelector.Get().Hide();
            }

            else if(selected_type == SlotType.ActionSelectorUI)
            {
                ActionSelectorUI.Get().Hide();
                InventoryPanel.Get().CancelSelection();
                EquipPanel.Get().CancelSelection();
                StoragePanel.Get().CancelSelection();
                selected_panel = prev_select_panel;
            }

        }

        private void OnPressCancel()
        {
            SlotType selected_type = GetSelectedType();

            if (selected_type == SlotType.Inventory || selected_type == SlotType.Equipment || selected_type == SlotType.CraftInfo)
            {
                ItemSlotPanel.CancelSelectionAll();
                selected_panel = null;
            }

            if (selected_type == SlotType.Storage)
            {
                StoragePanel.Get().Hide();
                selected_panel = null;
            }

            else if (selected_type == SlotType.Craft)
            {
                CraftPanel.Get().Toggle();
                CraftSubPanel.Get().Hide();
                selected_panel = null;
            }

            else if (selected_type == SlotType.CraftSub)
            {
                CraftSubPanel.Get().CancelSelection();
                CraftInfoPanel.Get().Hide();
                selected_panel = CraftPanel.Get();
            }

            else if (selected_type == SlotType.ActionSelectorGame)
            {
                ActionSelector.Get().Hide();
                selected_panel = null;
            }

            else if (selected_type == SlotType.ActionSelectorUI)
            {
                ActionSelectorUI.Get().Hide();
                InventoryPanel.Get().CancelSelection();
                EquipPanel.Get().CancelSelection();
                StoragePanel.Get().CancelSelection();
                selected_panel = prev_select_panel;
            }

            if (ReadPanel.Get().IsVisible())
                ReadPanel.Get().Hide();
        }

        private void OnPressAttack()
        {
            SlotType selected_type = GetSelectedType();

            if (selected_type == SlotType.Inventory || selected_type == SlotType.Equipment || selected_type == SlotType.CraftInfo)
            {
                ItemSlotPanel.CancelSelectionAll();
                selected_panel = null;
            }
        }

        /*private SlotType FindAutoSelectedUI()
        {
            SlotType type = selected_type;
            if (TheGame.Get().IsPausedByPlayer())
            {
                type = SlotType.PauseMenu;
            }
            else if (ActionSelectorUI.Get().IsVisible())
            {
                type = SlotType.ActionSelectorUI;
            }
            else if (ActionSelector.Get().IsVisible())
            {
                type = SlotType.ActionSelectorGame;
            }
            return type;
        }*/

        public UISlot GetSelectedSlot()
        {
            UISlot selected_slot = selected_panel?.GetSelectSlot();
            return selected_slot;
        }

        public SlotType GetSelectedType()
        {
            UISlot selected_slot = selected_panel?.GetSelectSlot();
            return selected_slot ? selected_slot.type : SlotType.None;
        }

        public UISlotPanel GetSelectedPanel()
        {
            return selected_panel;
        }

        public int GetSelectedIndex()
        {
            if (selected_panel != null)
                return selected_panel.selection_index;
            return -1;
        }

        public bool IsCraftPanelFocus()
        {
            return selected_panel == CraftPanel.Get() || selected_panel == CraftSubPanel.Get() || selected_panel == CraftInfoPanel.Get();
        }

        public bool IsPanelFocus()
        {
            return selected_panel != null;
        }

        public bool IsPanelFocusItem()
        {
            UISlot slot = selected_panel?.GetSelectSlot();
            ItemSlot islot = (slot != null && slot is ItemSlot) ? (ItemSlot)slot : null;
            return islot != null && islot.GetItem() != null;
        }

        public bool IsLeft(Vector2 dir) { return dir.x < -0.1f; }
        public bool IsRight(Vector2 dir) { return dir.x > 0.1f; }
        public bool IsDown(Vector2 dir) { return dir.y < -0.1f; }
        public bool IsUp(Vector2 dir) { return dir.y > 0.1f; }

        public static KeyControlsUI Get()
        {
            return _instance;
        }
    }

}