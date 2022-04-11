using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalEngine
{

    /// <summary>
    /// A button slot for one of the crafting categories
    /// </summary>

    public class CategorySlot : MonoBehaviour
    {
        public GroupData group;
        public Image icon;
        public Image highlight;

        public UnityAction<GroupData> onClick;
        public UnityAction<GroupData> onClickRight;

        [HideInInspector]
        public int slot_index = -1;

        private EventTrigger evt_trigger;
        private RectTransform rect;

        void Start()
        {
            rect = GetComponent<RectTransform>();
            evt_trigger = GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((BaseEventData eventData) => { OnClick(eventData); });
            evt_trigger.triggers.Add(entry);

            if (group != null && group.icon != null)
                icon.sprite = group.icon;

            if (highlight)
                highlight.enabled = false;
        }

        public void SelectSlot()
        {
            if (highlight != null)
                highlight.enabled = true;
        }

        public void UnselectSlot()
        {
            highlight.enabled = false;
        }

        public bool IsSelected()
        {
            return highlight.enabled;
        }

        public void SetSlot(GroupData group, Sprite sprite)
        {
            this.group = group;
            icon.sprite = sprite;
        }

        void OnClick(BaseEventData eventData)
        {
            PointerEventData pEventData = eventData as PointerEventData;

            if (pEventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(group);
            }
            else if (pEventData.button == PointerEventData.InputButton.Left)
            {
                if (onClick != null)
                    onClick.Invoke(group);
            }
        }

        public RectTransform GetRect()
        {
            return rect;
        }

    }

}