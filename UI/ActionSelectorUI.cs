using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// ActionSelectorUI is similar to ActionSelector, but for items in the player's inventory in the UI Canvas.
    /// </summary>

    public class ActionSelectorUI : MonoBehaviour
    {
        public ActionSelectorButton[] buttons;

        private Animator animator;
        private bool visible = false;

        private PlayerCharacter character;
        private ItemSlot slot;

        private static ActionSelectorUI _instance;

        void Awake()
        {
            _instance = this;
            animator = GetComponent<Animator>();
            gameObject.SetActive(false);

            foreach (ActionSelectorButton button in buttons)
            {
                button.onClick += OnClickAction;
            }
        }

        private void Start()
        {
            //PlayerControlsMouse.Get().onClick += OnMouseClick;
            PlayerControlsMouse.Get().onRightClick += OnMouseClick;

        }

        void Update()
        {
            
        }

        private void RefreshPanel()
        {
            foreach (ActionSelectorButton button in buttons)
                button.Hide();

            if (slot != null)
            {
                int index = 0;
                foreach (SAction action in slot.GetItem().actions)
                {
                    if (index < buttons.Length && action.CanDoAction(character, slot))
                    {
                        ActionSelectorButton button = buttons[index];
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
                if (!visible || this.slot != slot || this.character != character)
                {
                    this.slot = slot;
                    this.character = character;
                    visible = true;
                    RefreshPanel();
                    animator.Rebind();
                    //animator.SetTrigger("Show");
                    transform.position = slot.transform.position;
                    gameObject.SetActive(true);
                }
            }
        }

        public void Hide()
        {
            if (visible)
            {
                character = null;
                visible = false;
                animator.SetTrigger("Hide");
                Invoke("AfterHide", 1f);
            }
        }

        private void AfterHide()
        {
            if (!visible)
                gameObject.SetActive(false);
        }

        public void OnClickAction(SAction action)
        {
            if (visible)
            {
                if (action != null && slot != null && character != null)
                {
                    ItemSlot aslot = slot;
                    PlayerCharacter acharacter = character;

                    TheUI.Get().CancelSelection();
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

        public bool IsVisible()
        {
            return visible;
        }

        public static ActionSelectorUI Get()
        {
            return _instance;
        }
    }

}