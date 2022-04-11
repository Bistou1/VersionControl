using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalEngine
{

    public enum ItemSlotType
    {
        None = 0,
        Inventory = 5,
        Equipment = 10,
        Storage = 15,
    }

    /// <summary>
    /// Item slot that shows a single item, in your inventory or equipped bar
    /// </summary>

    public class ItemSlot : MonoBehaviour
    {
        public ItemSlotType type;
        public Image icon;
        public Image default_icon;
        public Image highlight;
        public Image filter;
        public Text value;
        public Text dura;
        public Text title;

        public UnityAction<CraftData> onClick;
        public UnityAction<CraftData> onClickRight;
        public UnityAction<CraftData> onClickLong;
        public UnityAction<CraftData> onClickDouble;
        public UnityAction<CraftData> onPressKey;

        [HideInInspector]
        public int index = -1; //index in the bar

        [HideInInspector]
        public bool is_equip = false;

        private EventTrigger evt_trigger;
        private RectTransform rect;
        private Animator animator;

        private CraftData item;
        private int quantity;
        private float durability;

        private bool is_holding = false;
        private bool can_click = false;
        private float holding_timer = 0f;
        private float double_timer = 0f;

        void Start()
        {
            rect = GetComponent<RectTransform>();
            animator = GetComponent<Animator>();
            evt_trigger = GetComponent<EventTrigger>();

            EventTrigger.Entry entry1 = new EventTrigger.Entry();
            entry1.eventID = EventTriggerType.PointerClick;
            entry1.callback.AddListener((BaseEventData eventData) => { OnClick(eventData); });
            evt_trigger.triggers.Add(entry1);

            EventTrigger.Entry entry2 = new EventTrigger.Entry();
            entry2.eventID = EventTriggerType.PointerDown;
            entry2.callback.AddListener((BaseEventData eventData) => { OnDown(eventData); });
            evt_trigger.triggers.Add(entry2);

            EventTrigger.Entry entry3 = new EventTrigger.Entry();
            entry3.eventID = EventTriggerType.PointerUp;
            entry3.callback.AddListener((BaseEventData eventData) => { OnUp(eventData); });
            evt_trigger.triggers.Add(entry3);

            EventTrigger.Entry entry4 = new EventTrigger.Entry();
            entry4.eventID = EventTriggerType.PointerExit;
            entry4.callback.AddListener((BaseEventData eventData) => { OnExit(eventData); });
            evt_trigger.triggers.Add(entry4);

            if (highlight)
                highlight.enabled = false;
            if (dura)
                dura.enabled = false;
        }

        private void Update()
        {
            if (double_timer < 1f)
                double_timer += Time.deltaTime;

            //Hold
            if (is_holding)
            {
                holding_timer += Time.deltaTime;
                if (holding_timer > 0.5f)
                {
                    can_click = false;
                    is_holding = false;
                    if (onClickLong != null)
                        onClickLong.Invoke(item);
                }
            }

            //Keyboard shortcut
            if (type == ItemSlotType.Inventory)
            {
                int key_index = (index + 1);
                if (key_index == 10)
                    key_index = 0;
                if (key_index < 10 && Input.GetKeyDown(key_index.ToString()))
                {
                    if (onPressKey != null)
                        onPressKey.Invoke(item);
                }
            }
        }

        public void SelectSlot()
        {
            if (item != null)
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

        public void SetSlot(CraftData item, int quantity, float durability, bool selected)
        {
            if (item != null)
            {
                CraftData prev = this.item;
                int prevq = this.quantity;
                this.item = item;
                this.quantity = quantity;
                this.durability = durability;
                icon.sprite = item.icon;
                icon.enabled = true;
                value.text = quantity.ToString();
                value.enabled = quantity > 1;

                if (title != null)
                {
                    title.enabled = selected;
                    title.text = item.title;
                }

                if (highlight != null)
                    highlight.enabled = selected;

                if (default_icon != null)
                    default_icon.enabled = false;

                if (dura != null)
                    dura.enabled = false;
                if (filter != null)
                    filter.enabled = false;

                if (item is ItemData)
                {
                    ItemData idata = (ItemData)item;
                    int durabi = idata.GetDurabilityPercent(durability);
                    if (dura != null)
                    {
                        dura.enabled = idata.HasDurability() && durabi < 100 && (idata.durability_type != DurabilityType.Spoilage || durabi <= 50);
                        dura.text = durabi.ToString() + "%";
                    }

                    if (filter != null)
                    {
                        filter.enabled = idata.HasDurability() && durabi <= 40 && idata.durability_type == DurabilityType.Spoilage;
                        filter.color = durabi <= 20 ? TheUI.Get().filter_red : TheUI.Get().filter_yellow;
                    }
                }

                if (prev != item || prevq != quantity)
                    AnimateGain();
            }
            else
            {
                this.item = null;
                this.quantity = 0;
                this.durability = 0f;
                icon.enabled = false;
                value.enabled = false;

                if (dura != null)
                    dura.enabled = false;

                if (filter != null)
                    filter.enabled = false;

                if (title != null)
                    title.enabled = false;

                if (highlight != null)
                    highlight.enabled = false;

                if (default_icon != null)
                    default_icon.enabled = true;
            }

            gameObject.SetActive(true);
        }

        public void SetSlotCustom(Sprite sicon, string title, bool selected)
        {
            this.item = null;
            this.quantity = 1;
            this.durability = 0f;
            icon.enabled = sicon != null;
            icon.sprite = sicon;
            value.enabled = false;

            if (this.title != null)
            {
                this.title.enabled = selected;
                this.title.text = title;
            }

            if (dura != null)
                dura.enabled = false;

            if (filter != null)
                filter.enabled = false;

            if (highlight != null)
                highlight.enabled = selected;

            if (default_icon != null)
                default_icon.enabled = false;

            gameObject.SetActive(true);
        }

        public void ShowTitle()
        {
            if (this.title != null)
                this.title.enabled = true;
        }

        public void AnimateGain()
        {
            if (animator != null)
                animator.SetTrigger("Gain");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void OnClick(BaseEventData eventData)
        {
            if (can_click)
            {

            }
        }

        void OnDown(BaseEventData eventData)
        {
            is_holding = true;
            can_click = true;
            holding_timer = 0f;

            PointerEventData pEventData = eventData as PointerEventData;

            if (pEventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(item);
            }
            else if (pEventData.button == PointerEventData.InputButton.Left)
            {
                if (double_timer < 0f)
                {
                    double_timer = 0f;
                    if (onClickDouble != null)
                        onClickDouble.Invoke(item);
                }
                else
                {
                    double_timer = -0.3f;
                    if (onClick != null)
                        onClick.Invoke(item);
                }
            }
        }

        void OnUp(BaseEventData eventData)
        {
            is_holding = false;
        }

        void OnExit(BaseEventData eventData)
        {
            is_holding = false;
        }

        public CraftData GetCraftable()
        {
            return item;
        }

        public ItemData GetItem()
        {
            if (item != null)
                return item.GetItem();
            return null;
        }

        public int GetQuantity()
        {
            return quantity;
        }

        public float GetDurability()
        {
            return durability;
        }

        public ConstructionData GetConstruction()
        {
            if (item != null)
                return item.GetConstruction();
            return null;
        }

        public RectTransform GetRect()
        {
            return rect;
        }

    }

}