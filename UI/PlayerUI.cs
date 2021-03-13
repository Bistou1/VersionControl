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

        private InventoryPanel inventory_panel;
        private EquipPanel equip_panel;

        private static List<PlayerUI> ui_list = new List<PlayerUI>();

        protected override void Awake()
        {
            base.Awake();
            ui_list.Add(this);

            inventory_panel = GetComponentInChildren<InventoryPanel>();
            equip_panel = GetComponentInChildren<EquipPanel>();

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
        }

        protected override void Update()
        {
            base.Update();

            //Init inventories
            if (inventory_panel != null)
                inventory_panel.InitInventory();
            if (equip_panel != null)
                equip_panel.InitInventory();

            //Fx visibility
            if (build_mode_text != null)
                build_mode_text.enabled = IsBuildMode();

            if (tps_cursor != null)
                tps_cursor.enabled = TheCamera.Get().IsLocked();

            //Controls
            PlayerControls controls = PlayerControls.Get();

            if (controls.IsPressCraft())
            {
                CraftPanel.Get().Toggle();
                ActionSelectorUI.Get().Hide();
                ActionSelector.Get().Hide();
            }

            //Backpack panel
            PlayerCharacter character = GetPlayer();
            if (character != null)
            {
                InventoryItemData item = character.Inventory.GetBestEquippedBag();
                ItemData idata = ItemData.Get(item?.item_id);
                if (idata != null)
                    BagPanel.Get().ShowBag(item.uid, idata.bag_size);
                else
                    BagPanel.Get().HideBag();
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
            CraftPanel.Get().CancelSelection();
            CraftSubPanel.Get().CancelSelection();
            ActionSelectorUI.Get().Hide();
            ActionSelector.Get().Hide();

            if (onCancelSelection != null)
                onCancelSelection.Invoke();
        }

        public void OnClickCraft()
        {
            CancelSelection();
            CraftPanel.Get().Toggle();
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
