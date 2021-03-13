using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivalEngine {

    /// <summary>
    /// In-Game UI, specific to one player
    /// </summary>

    public class PlayerUI : UIPanel
    {
        [Header("Player")]
        public int player_id;

        [Header("Gameplay UI")]
        public UIPanel damage_fx;
        public Text build_mode_text;
        public Image tps_cursor;

        public UnityAction onCancelSelection;

        private ItemSlotPanel[] item_slot_panels;

        private static List<PlayerUI> ui_list = new List<PlayerUI>();

        protected override void Awake()
        {
            base.Awake();
            ui_list.Add(this);

            item_slot_panels = GetComponentsInChildren<ItemSlotPanel>();

            if (build_mode_text != null)
                build_mode_text.enabled = false;

            Show(true);
        }

        void OnDestroy()
        {
            ui_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

            PlayerCharacter ui_player = GetPlayer();
            if (ui_player != null)
                ui_player.Combat.onDamaged += DoDamageFX;

            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            mouse.onRightClick += (Vector3 pos) => { CancelSelection(); };
        }

        protected override void Update()
        {
            base.Update();

            //Init inventories from here because they are disabled
            foreach (ItemSlotPanel panel in item_slot_panels)
                panel.InitPanel();

            //Fx visibility
            if (build_mode_text != null)
                build_mode_text.enabled = IsBuildMode();

            if (tps_cursor != null)
                tps_cursor.enabled = TheCamera.Get().IsLocked();

            //Controls
            PlayerControls controls = PlayerControls.Get(player_id);

            if (controls.IsPressCraft())
            {
                CraftPanel.Get(player_id)?.Toggle();
                ActionSelectorUI.Get(player_id)?.Hide();
                ActionSelector.Get(player_id)?.Hide();
            }

            //Backpack panel
            PlayerCharacter character = GetPlayer();
            BagPanel bag_panel = BagPanel.Get(player_id);
            if (character != null && bag_panel != null)
            {
                InventoryItemData item = character.Inventory.GetBestEquippedBag();
                ItemData idata = ItemData.Get(item?.item_id);
                if (idata != null)
                    bag_panel.ShowBag(character, item.uid, idata.bag_size);
                else
                    bag_panel.HideBag();
            }
        }

        public void DoDamageFX()
        {
            if(damage_fx != null)
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
            ItemSlotPanel.CancelSelectionAll();
            CraftPanel.Get(player_id)?.CancelSelection();
            CraftSubPanel.Get(player_id)?.CancelSelection();
            ActionSelectorUI.Get(player_id)?.Hide();
            ActionSelector.Get(player_id)?.Hide();

            if (onCancelSelection != null)
                onCancelSelection.Invoke();
        }

        public void OnClickCraft()
        {
            CancelSelection();
            CraftPanel.Get(player_id)?.Toggle();
        }

        public ItemSlot GetSelectedSlot()
        {
            return ItemSlotPanel.GetSelectedSlotInAllPanels();
        }

        public int GetSelectedSlotIndex()
        {
            ItemSlot slot = ItemSlotPanel.GetSelectedSlotInAllPanels();
            return slot != null ? slot.index : -1;
        }

        public InventoryData GetSelectedSlotInventory()
        {
            ItemSlot slot = ItemSlotPanel.GetSelectedSlotInAllPanels();
            return slot != null ? slot.GetInventory() : null;
        }

        public bool IsBuildMode()
        {
            PlayerCharacter player = GetPlayer();
            if (player)
                return player.Crafting.IsBuildMode();
            return false;
        }

        public PlayerCharacter GetPlayer()
        {
            return PlayerCharacter.Get(player_id);
        }

        public static void ShowUI()
        {
            foreach (PlayerUI ui in ui_list)
                ui.Show();
        }

        public static void HideUI()
        {
            foreach (PlayerUI ui in ui_list)
                ui.Hide();
        }

        public static bool IsUIVisible()
        {
            if (ui_list.Count > 0)
                return ui_list[0].IsVisible();
            return false;
        }

        public static PlayerUI Get(int player_id=0)
        {
            foreach (PlayerUI ui in ui_list)
            {
                if (ui.player_id == player_id)
                    return ui;
            }
            return null;
        }

        public static PlayerUI GetFirst()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (player != null)
                return Get(player.player_id);
            return null;
        }

        public static List<PlayerUI> GetAll()
        {
            return ui_list;
        }
    }

}
