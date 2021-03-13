using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// ActionSelectorUI is similar to ActionSelector, but for items in the player's inventory in the UI Canvas.
    /// </summary>

    public class ActionSelectorUI : UISlotPanel
    {
        private Animator animator;

        private PlayerCharacter character;
        private ItemSlot slot;

        private static ActionSelectorUI _instance;

        protected override void Awake()
        {
            base.Awake();

            _instance = this;
            animator = GetComponent<Animator>();
            gameObject.SetActive(false);

        }

        protected override void Start()
        {
            base.Start();

            //PlayerControlsMouse.Get().onClick += OnMouseClick;
            PlayerControlsMouse.Get().onRightClick += OnMouseClick;

            onClickSlot += OnClick;
        }

        private void RefreshSelector()
        {
            foreach (ActionSelectorButton button in slots)
                button.Hide();

            if (slot != null)
            {
                int index = 0;
                foreach (SAction action in slot.GetItem().actions)
                {
                    if (index < slots.Length && action.CanDoAction(character, slot))
                    {
                        ActionSelectorButton button = (ActionSelectorButton) slots[index];
                        button.SetButton(action);
                        index++;
                    }
                }
            }
        }

        public void Show(PlayerCharacter character, ItemSlot slot)
        {
            if (slot != null && character != null)
            {
                if (!IsVisible() || this.slot != slot || this.character != character)
                {
                    this.slot = slot;
                    this.character = character;
                    RefreshSelector();
                    //animator.SetTrigger("Show");
                    transform.position = slot.transform.position;
                    gameObject.SetActive(true);
                    animator.Rebind();
                    animator.SetBool("Solo", CountActiveSlots() == 1);
                    selection_index = 0;
                    Show();
                }
            }
        }

        public override void Hide(bool instant = false)
        {
            if (IsVisible())
            {
                base.Hide(instant);
                character = null;
                animator.SetTrigger("Hide");
            }
        }

        private void OnClick(UISlot islot)
        {
            ActionSelectorButton button = (ActionSelectorButton)islot;
            OnClickAction(button.GetAction());
        }

        public void OnClickAction(SAction action)
        {
            if (IsVisible())
            {
                if (action != null && slot != null && character != null)
                {
                    ItemSlot aslot = slot;
                    PlayerCharacter acharacter = character;

                    PlayerUI.Get(character.player_id)?.CancelSelection();
                    Hide();

                    if (action.CanDoAction(acharacter, aslot))
                        action.DoAction(acharacter, aslot);

                    
                }
            }
        }

        private void OnMouseClick(Vector3 pos)
        {
            Hide();
        }

        public static ActionSelectorUI Get()
        {
            return _instance;
        }
    }

}