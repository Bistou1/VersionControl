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
        public int player_id;

        public UISlotPanel default_top;
        public UISlotPanel default_down;
        public UISlotPanel default_left;
        public UISlotPanel default_right;

        private UISlotPanel selected_panel = null;
        private UISlotPanel prev_select_panel = null;
        private UISlot prev_slot = null;

        private static List<KeyControlsUI> controls_ui_list = new List<KeyControlsUI>();

        void Awake()
        {
            controls_ui_list.Add(this);
        }

        private void OnDestroy()
        {
            controls_ui_list.Remove(this);
        }

        void Update()
        {
            PlayerControls controls = PlayerControls.Get(player_id);

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

            ActionSelector selector = ActionSelector.Get(player_id);
            ActionSelectorUI selector_ui = ActionSelectorUI.Get(player_id);
            CraftPanel craft_panel = CraftPanel.Get(player_id);
            CraftSubPanel craftsub_panel = CraftSubPanel.Get(player_id);

            if (!IsCraftPanelFocus() && craft_panel != null && craft_panel.IsVisible())
            {
                selected_panel = craft_panel;
                CraftInfoPanel.Get(player_id)?.Hide();
            }

            if (selected_panel == craft_panel && craft_panel != null && craft_panel.IsVisible() && craftsub_panel != null)
            {
                selected_panel.selection_index = Mathf.Clamp(selected_panel.selection_index, 0, selected_panel.CountActiveSlots() - 1);

                UISlot slot = selected_panel.GetSelectSlot();
                if (prev_slot != slot || !craftsub_panel.IsVisible())
                {
                    craft_panel.KeyClick();
                    craftsub_panel.selection_index = 0;
                }
            }

            else if (selected_panel == craftsub_panel && craftsub_panel != null && craftsub_panel.IsVisible())
            {
                selected_panel.selection_index = Mathf.Clamp(selected_panel.selection_index, 0, selected_panel.CountActiveSlots() - 1);

                UISlot slot = selected_panel.GetSelectSlot();
                CraftInfoPanel info_panel = CraftInfoPanel.Get(player_id);
                if (info_panel != null)
                {
                    if (prev_slot != slot || !info_panel.IsVisible())
                    {
                        craftsub_panel.KeyClick();
                    }
                }
            }

            if (TheGame.Get().IsPausedByPlayer())
                selected_panel = PausePanel.Get();
            else if (selector_ui != null && selector_ui.IsVisible())
                selected_panel = selector_ui;
            else if (selector != null && selector.IsVisible())
                selected_panel = selector;

            if (selected_panel == CraftInfoPanel.Get(player_id) && selected_panel != null)
            {
                PlayerCharacter player = PlayerCharacter.Get(player_id);
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
            ActionSelector.Get(player_id)?.Hide();
            ActionSelectorUI.Get(player_id)?.Hide();
            selected_panel = null;
        }

        private void OnPressSelect()
        {
            SlotType selected_type = GetSelectedType();

            if (selected_type == SlotType.Inventory)
            {
                InventoryPanel.Get(player_id)?.KeyClick();
            }

            else if (selected_type == SlotType.Equipment)
            {
                EquipPanel.Get(player_id)?.KeyClick();
            }

            else if(selected_type == SlotType.Storage)
            {
                StoragePanel.Get(player_id)?.KeyClick();
            }

            else if (selected_type == SlotType.ActionSelectorGame)
            {
                ActionSelector.Get(player_id)?.KeyClick();
                selected_type = SlotType.None;
            }

            else if(selected_type == SlotType.ActionSelectorUI)
            {
                ActionSelectorUI.Get(player_id)?.KeyClick();
                selected_panel = prev_select_panel;
            }

            else if (selected_type == SlotType.Craft)
            {
                if (CraftSubPanel.Get(player_id) && CraftSubPanel.Get(player_id).IsVisible())
                {
                    selected_panel = CraftSubPanel.Get(player_id);
                    CraftSubPanel.Get(player_id).KeyClick();
                }
            }

            else if (selected_type == SlotType.CraftSub)
            {
                if (CraftInfoPanel.Get(player_id) && CraftInfoPanel.Get(player_id).IsVisible())
                {
                    PlayerCharacter player = PlayerCharacter.Get(player_id);
                    CraftInfoPanel.Get(player_id).OnClickCraft();
                    if (player.Crafting.IsBuildMode())
                    {
                        selected_type = SlotType.CraftInfo;
                        CraftPanel.Get(player_id)?.Hide();
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
                InventoryPanel.Get(player_id)?.KeyClick(true);
            }

            else if(selected_type == SlotType.Equipment)
            {
                prev_select_panel = selected_panel;
                EquipPanel.Get(player_id)?.KeyClick(true);
            }

            else if(selected_type == SlotType.ActionSelectorGame)
            {
                ActionSelector.Get(player_id)?.Hide();
            }

            else if(selected_type == SlotType.ActionSelectorUI)
            {
                ActionSelectorUI.Get(player_id)?.Hide();
                InventoryPanel.Get(player_id)?.CancelSelection();
                EquipPanel.Get(player_id)?.CancelSelection();
                StoragePanel.Get(player_id)?.CancelSelection();
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
                StoragePanel.Get(player_id)?.Hide();
                selected_panel = null;
            }

            else if (selected_type == SlotType.Craft)
            {
                CraftPanel.Get(player_id)?.Toggle();
                CraftSubPanel.Get(player_id)?.Hide();
                selected_panel = null;
            }

            else if (selected_type == SlotType.CraftSub)
            {
                CraftSubPanel.Get(player_id)?.CancelSelection();
                CraftInfoPanel.Get(player_id)?.Hide();
                selected_panel = CraftPanel.Get(player_id);
            }

            else if (selected_type == SlotType.ActionSelectorGame)
            {
                ActionSelector.Get(player_id)?.Hide();
                selected_panel = null;
            }

            else if (selected_type == SlotType.ActionSelectorUI)
            {
                ActionSelectorUI.Get(player_id)?.Hide();
                InventoryPanel.Get(player_id)?.CancelSelection();
                EquipPanel.Get(player_id)?.CancelSelection();
                StoragePanel.Get(player_id)?.CancelSelection();
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
            if (selected_panel == null)
                return false;
            return selected_panel == CraftPanel.Get(player_id) || selected_panel == CraftSubPanel.Get(player_id) || selected_panel == CraftInfoPanel.Get(player_id);
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

        public static KeyControlsUI Get(int player_id=0)
        {
            foreach (KeyControlsUI panel in controls_ui_list)
            {
                if (panel.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static List<KeyControlsUI> GetAll()
        {
            return controls_ui_list;
        }
    }

}