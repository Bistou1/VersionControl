using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public UIPanel pause_panel;
        public UIPanel game_over_panel;
        public UIPanel damage_fx;

        [Header("Material")]
        public Material ui_material;
        public Material text_material;

        [Header("Others")]
        public Text build_mode_text;
        public Image tps_cursor;

        public AudioClip ui_sound;
        public Image speaker_btn;
        public Sprite speaker_on;
        public Sprite speaker_off;

        public Color filter_red;
        public Color filter_yellow;

        private Canvas canvas;
        private RectTransform rect;
        private bool ui_hidden = false;

        private static TheUI _instance;

        void Awake()
        {
            _instance = this;
            canvas = GetComponent<Canvas>();
            rect = GetComponent<RectTransform>();

            if (build_mode_text != null)
                build_mode_text.enabled = false;

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

            PlayerCharacter.Get().onDamaged += DoDamageFX;
        }

        void Update()
        {
            pause_panel.SetVisible(TheGame.Get().IsPausedByPlayer());
            speaker_btn.sprite = PlayerData.Get().master_volume > 0.1f ? speaker_on : speaker_off;

            if (build_mode_text != null)
                build_mode_text.enabled = PlayerCharacter.Get().IsBuildMode();

            if (tps_cursor != null)
                tps_cursor.enabled = TheCamera.Get().IsLocked();
        }

        public void DoDamageFX()
        {
            StartCoroutine(DamageFXRun());
        }

        private IEnumerator DamageFXRun()
        {
            damage_fx.Show();
            yield return new WaitForSeconds(1f);
            damage_fx.Hide();
        }

        public void CancelSelection()
        {
            EquipBar.Get().CancelSelection();
            CraftBar.Get().CancelSelection();
            InventoryBar.Get().CancelSelection();
            StorageBar.Get().CancelSelection();
            PlayerCharacter.Get().CancelBuilding();
            ActionSelectorUI.Get().Hide();
            ActionSelector.Get().Hide();
        }

        public void ShowGameOver()
        {
            CancelSelection();
            game_over_panel.Show();
        }

        public void ShowUI()
        {
            ui_hidden = false;
            gameplay_ui.alpha = 1f;
        }

        public void HideUI()
        {
            ui_hidden = true;
            gameplay_ui.alpha = 0f;
        }

        public void OnClickPause()
        {
            if (TheGame.Get().IsPaused())
                TheGame.Get().Unpause();
            else
                TheGame.Get().Pause();

            TheAudio.Get().PlaySFX("UI", ui_sound);
        }

        public void OnClickSave()
        {
            TheGame.Get().Save();
        }

        public void OnClickLoad()
        {
            StartCoroutine(LoadRoutine());
        }

        public void OnClickNew()
        {
            StartCoroutine(NewRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            BlackPanel.Get().Show();

            yield return new WaitForSeconds(1f);

            PlayerData.Unload(); //Make sure to unload first, or it won't load if already loaded
            PlayerData.LoadLast();
            SceneNav.GoTo(PlayerData.Get().current_scene);
        }

        private IEnumerator NewRoutine()
        {
            BlackPanel.Get().Show();

            yield return new WaitForSeconds(1f);

            PlayerData.NewGame();
            SceneNav.GoTo(SceneNav.GetCurrentScene());
        }

        public void OnClickCraft()
        {
            CancelSelection();
            CraftBar.Get().ToggleBar();
        }

        public void OnClickMusicToggle()
        {
            PlayerData.Get().master_volume = PlayerData.Get().master_volume > 0.1f ? 0f : 1f;
            TheAudio.Get().RefreshVolume();
        }

        public ItemSlot GetSelectedItemSlot()
        {
            ItemSlot eslot = EquipBar.Get().GetSelectedSlot();
            ItemSlot sslot = StorageBar.Get().GetSelectedSlot();
            ItemSlot islot = InventoryBar.Get().GetSelectedSlot();

            if (eslot != null)
                return eslot;
            if (sslot != null)
                return sslot;
            return islot;
        }

        //Convert a screen position (like mouse) to a anchored position in the canvas
        public Vector2 ScreenPointToCanvasPos(Vector2 pos)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pos, canvas.worldCamera, out localpoint);
            return localpoint;
        }

        public bool IsHidden()
        {
            return ui_hidden;
        }

        public bool IsBlockingPanelOpened()
        {
            return StorageBar.Get().IsVisible() || ReadPanel.Get().IsVisible() || pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        public static TheUI Get()
        {
            return _instance;
        }
    }

}