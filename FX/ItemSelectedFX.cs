using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    /// <summary>
    /// FX that shows an item following the mouse when selected
    /// </summary>

    public class ItemSelectedFX : MonoBehaviour
    {
        public GameObject icon_group;
        public SpriteRenderer icon;
        public Text title;


        private static ItemSelectedFX _instance;

        void Awake()
        {
            _instance = this;
            icon_group.SetActive(false);
            title.enabled = false;
        }

        void Update()
        {
            transform.position = PlayerControlsMouse.Get().GetPointingPos();
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up);

            ItemSlot slot = TheUI.Get().GetSelectedItemSlot();

            Selectable select = Selectable.GetNearestHover(transform.position);
            MAction maction = slot != null && slot.GetItem() != null ? slot.GetItem().FindMergeAction(select) : null;
            title.enabled = maction != null && maction.CanDoAction(PlayerCharacter.Get(), select);
            title.text = maction != null ? maction.title : "";

            if ((slot != null) != icon_group.activeSelf)
                icon_group.SetActive(slot != null);

            if (slot != null && slot.GetItem())
            {
                icon.sprite = slot.GetItem().icon;
            }
        }


        public static ItemSelectedFX Get()
        {
            return _instance;
        }
    }

}