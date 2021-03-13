﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalEngine
{

    /// <summary>
    /// Top level UI script that manages the UI
    /// </summary>

    public class TheUI : MonoBehaviour
    {
        [Header("Panels")]
        public CanvasGroup gameplay_ui;
        public PausePanel pause_panel;
        public GameOverPanel game_over_panel;

        [Header("Material")]
        public Material ui_material;
        public Material text_material;
        
        public AudioClip ui_sound;

        public Color filter_red;
        public Color filter_yellow;

        private Canvas canvas;
        private RectTransform rect;

        private static TheUI _instance;

        void Awake()
        {
            _instance = this;
            canvas = GetComponent<Canvas>();
            rect = GetComponent<RectTransform>();

            if (ui_material != null)
            {
                foreach (Image image in GetComponentsInChildren<Image>())
                    image.material = ui_material;
            }
            if(text_material != null)
            {
                foreach (Text txt in GetComponentsInChildren<Text>())
                    txt.material = text_material;
            }
        }

        private void Start()
        {
            canvas.worldCamera = TheCamera.GetCamera();

            if (!TheGame.IsMobile() && ItemSelectedFX.Get() == null && GameData.Get().item_select_fx != null)
            {
                Instantiate(GameData.Get().item_select_fx, transform.position, Quaternion.identity);
            }

            PlayerUI gameplay_ui = GetComponentInChildren<PlayerUI>();
            if (gameplay_ui == null)
                Debug.LogError("Warning: Missing PlayerUI script on the Gameplay tab in the UI prefab");
        }

        void Update()
        {
            pause_panel.SetVisible(TheGame.Get().IsPausedByPlayer());
            
            PlayerControls controls = PlayerControls.Get();
            
            if (controls.IsPressPause() && !TheGame.Get().IsPausedByPlayer())
                TheGame.Get().Pause();
            else if (controls.IsPressPause() && TheGame.Get().IsPausedByPlayer())
                TheGame.Get().Unpause();

        }

        public void ShowGameOver()
        {
            foreach(PlayerUI ui in PlayerUI.GetAll())
                ui.CancelSelection();
            game_over_panel.Show();
        }

        public void OnClickPause()
        {
            if (TheGame.Get().IsPaused())
                TheGame.Get().Unpause();
            else
                TheGame.Get().Pause();

            TheAudio.Get().PlaySFX("UI", ui_sound);
        }

        public bool IsBlockingPanelOpened()
        {
            return StoragePanel.Get().IsVisible() || ReadPanel.Get().IsVisible() || pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        //Convert a screen position (like mouse) to a anchored position in the canvas
        public Vector2 ScreenPointToCanvasPos(Vector2 pos)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pos, canvas.worldCamera, out localpoint);
            return localpoint;
        }

        //Menu are panels that block gamepad controls
        public bool IsMenuOpened()
        {
            return pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        public static TheUI Get()
        {
            return _instance;
        }
    }

}