using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    /// <summary>
    /// FX that shows an item following the mouse when dragging
    /// </summary>

    public class ItemDragFX : MonoBehaviour
    {
        public GameObject icon_group;
        public SpriteRenderer icon;
        public Text title;
        public float refresh_rate = 0.1f;

        private ItemSlot current_slot = null;
        private float timer = 0f;

        private static ItemDragFX _instance;

        void Awake()
        {
            _instance = this;
            icon_group.SetActive(false);
            title.enabled = false;
        }

        void Update()
        {
            transform.position = PlayerControlsMouse.Get().GetMouseWorldPosition();
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up);

            PlayerControls controls = PlayerControls.GetFirst();

            bool active = current_slot != null && controls != null && !controls.IsGamePad();
            if (active != icon_group.activeSelf)
                icon_group.SetActive(active);

            icon.enabled = false;
            if (current_slot != null && current_slot.GetItem())
            {
                icon.sprite = current_slot.GetItem().icon;
                icon.enabled = true;
            }

            timer += Time.deltaTime;
            if (timer > refresh_rate)
            {
                timer = 0f;
                SlowUpdate();
            }
        }

        private void SlowUpdate()
        {
            current_slot = ItemSlotPanel.GetDragSlotInAllPanels();
        }

        public static ItemDragFX Get()
        {
            return _instance;
        }
    }

}