using System.Collections;
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

        [HideInInspector]
        public int selection_index = 0; //For gamepad selection

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
            }
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

        //Click on slot from keyboard/gamepad
        public void KeyClick(bool right_click = false)
        {
            KeyClickSlot(selection_index, right_click);
        }

        public void KeyClickSlot(int index, bool right_click = false)
        {
            if (index >= 0 && index < slots.Length)
            {
                UISlot slot = slots[index];
                if (right_click)
                    slot.ClickRightSlot();
                else
                    slot.ClickSlot();
            }
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
            if (index >= 0 && index < slots.Length)
                return slots[index];
            return null;
        }

        public UISlot GetSelectSlot()
        {
            return GetSlot(selection_index);
        }

        public static List<UISlotPanel> GetAll()
        {
            return slot_panels;
        }
    }

}
