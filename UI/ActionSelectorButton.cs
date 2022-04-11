using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivalEngine
{
    /// <summary>
    /// One of the buttons on the ActionSelector
    /// </summary>

    public class ActionSelectorButton : MonoBehaviour
    {
        public Text title;

        public UnityAction<SAction> onClick;

        private SAction action;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void Update()
        {

        }

        public void SetButton(SAction action)
        {
            this.action = action;
            title.text = action.title;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public SAction GetAction()
        {
            return action;
        }

        public void OnClick()
        {
            if (onClick != null)
                onClick.Invoke(action);
        }
    }

}