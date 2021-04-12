﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Basic class for any panel containing slots that can be selected
    /// </summary>

    public class UISlotPanel : UIPanel
    {
        [Header("Slot Panel")]
        public float refresh_rate = 0.1f; //For optimization, set to 0f to refresh every frame
        public int slots_per_row = 99; //Useful for gamepad controls (know how the rows/column are setup)
        public UISlot[] slots;

        public UnityAction<UISlot> onClickSlot;
        public UnityAction<UISlot> onRightClickSlot;
        public UnityAction<UISlot> onPressAccept;
        public UnityAction<UISlot> onPressCancel;
        public UnityAction<UISlot> onPressUse;

        [HideInInspector]
        public int selection_index = 0; //For gamepad selection

        [HideInInspector]
        public bool unfocus_when_out = false; //Unfocus automatically if go out of panel

        [HideInInspector]
        public bool focused = false; //Focused panel

        private float timer = 0f;

        private static List<UISlotPanel> slot_panels = new List<UISlotPanel>();

        protected override void Awake()
        {
            base.Awake();
            slot_panels.Add(this);

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; //Important to copy so not overwritten in loop
                slots[i].index = index;
                slots[i].onClick += OnClickSlot;
                slots[i].onClickRight += OnClickSlotRight;
                slots[i].onClickLong += OnClickSlotRight;
                slots[i].onClickDouble += OnClickSlotRight;
                slots[i].onPressAccept += OnPressAccept;
                slots[i].onPressCancel += OnPressCancel;
                slots[i].onPressUse += OnPressUse;
            }
        }

        protected virtual void OnDestroy()
        {
            slot_panels.Remove(this);
        }

        protected override void Update()
        {
            base.Update();

            timer += Time.deltaTime;
            if (IsVisible())
            {
                if (timer > refresh_rate)
                {
                    timer = 0f;
                    SlowUpdate();
                }
            }
        }

        private void SlowUpdate()
        {
            RefreshPanel();
        }

        protected virtual void RefreshPanel()
        {

        }

        public void Focus()
        {
            UnfocusAll();
            focused = true;
            UISlot slot = GetSelectSlot();
            if (slot == null && slots.Length > 0)
                selection_index = slots[0].index;
        }

        public void PressSlot(int index)
        {
            UISlot slot = GetSlot(index);
            if (slot != null && onPressAccept != null)
                onPressAccept.Invoke(slot);
        }

        private void OnPressAccept(UISlot slot) {
            if (onPressAccept != null)
                onPressAccept.Invoke(slot);
        }

        private void OnPressCancel(UISlot slot)
        {
            if (onPressCancel != null)
                onPressCancel.Invoke(slot);
        }

        private void OnPressUse(UISlot slot)
        {
            if (onPressUse != null)
                onPressUse.Invoke(slot);
        }

        private void OnClickSlot(UISlot islot)
        {
            if (onClickSlot != null)
                onClickSlot.Invoke(islot);
        }

        private void OnClickSlotRight(UISlot islot)
        {
            //Event
            if (onRightClickSlot != null)
                onRightClickSlot.Invoke(islot);
        }

        public int CountActiveSlots()
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        public UISlot GetSlot(int index)
        {
            foreach (UISlot slot in slots)
            {
                if (slot.index == index)
                    return slot;
            }
            return null;
        }

        public UISlot GetSelectSlot()
        {
            return GetSlot(selection_index);
        }

        public bool IsSelectedInvisible()
        {
            UISlot slot = GetSelectSlot();
            return slot != null && !slot.IsVisible();
        }

        public bool IsSelectedValid()
        {
            UISlot slot = GetSelectSlot();
            return slot != null && slot.IsVisible();
        }

        public static void UnfocusAll()
        {
            foreach (UISlotPanel panel in slot_panels)
                panel.focused = false;
        }

        public static UISlotPanel GetFocusedPanel()
        {
            foreach (UISlotPanel panel in slot_panels)
            {
                if (panel.focused)
                    return panel;
            }
            return null;
        }

        public static List<UISlotPanel> GetAll()
        {
            return slot_panels;
        }
    }

}
