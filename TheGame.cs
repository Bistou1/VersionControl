using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Game Manager Script for Survival Engine
    /// Author: Indie Marc (Marc-Antoine Desbiens)
    /// </summary>

    public class TheGame : MonoBehaviour
    {
        [Header("Loader")]
        public GameObject ui_canvas;
        public GameObject ui_canvas_mobile;
        public GameObject audio_manager;
        public GameObject action_selector;

        private bool paused = false;
        private bool paused_by_player = false;
        private float death_timer = 0f;
        private float speed_multiplier = 1f;

        public UnityAction<bool> onPause;

        private static TheGame _instance;

        void Awake()
        {
            _instance = this;
            PlayerData.LoadLast();

            //Load managers
            if (!FindObjectOfType<TheUI>())
                Instantiate(IsMobile() ? ui_canvas_mobile : ui_canvas);
            if (!FindObjectOfType<TheAudio>())
                Instantiate(audio_manager);
            if (!FindObjectOfType<ActionSelector>())
                Instantiate(action_selector);

        }

        private void Start()
        {
            //Load game data
            PlayerData pdata = PlayerData.Get();
            GameData gdata = GameData.Get();
            if (!string.IsNullOrEmpty(pdata.current_scene) && pdata.current_scene == SceneNav.GetCurrentScene())
            {
                //Entry index: -1 = go to saved pos, 0=dont change character pos, 1+ = go to entry index
                if (pdata.current_entry_index < 0)
                {
                    PlayerCharacter.Get().transform.position = pdata.current_pos;
                    TheCamera.Get().MoveToTarget(pdata.current_pos);
                }

                if (pdata.current_entry_index > 0)
                {
                    ExitZone zone = ExitZone.GetIndex(pdata.current_entry_index);
                    if (zone != null)
                    {
                        Vector3 pos = zone.transform.position + zone.entry_offset;
                        Vector3 dir = new Vector3(zone.entry_offset.x, 0f, zone.entry_offset.z);
                        PlayerCharacter.Get().transform.position = pos;
                        if(dir.magnitude > 0.1f)
                            PlayerCharacter.Get().transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        TheCamera.Get().MoveToTarget(pos);
                    }
                }
            }

            pdata.current_scene = SceneNav.GetCurrentScene();

            GameObject spawn_parent = new GameObject("SaveFileSpawns");

            //Spawn dropped items
            foreach (KeyValuePair<string, DroppedItemData> elem in pdata.dropped_items)
            {
                if (elem.Value.scene == SceneNav.GetCurrentScene() && Item.GetByUID(elem.Key) == null)
                    Item.Spawn(elem.Key, spawn_parent.transform);
            }

            //Spawn constructions
            foreach (KeyValuePair<string, BuiltConstructionData> elem in pdata.built_constructions)
            {
                if (elem.Value.scene == SceneNav.GetCurrentScene() && Construction.GetByUID(elem.Key) == null)
                    Construction.Spawn(elem.Key, spawn_parent.transform);
            }

            //Spawn plants
            foreach (KeyValuePair<string, SowedPlantData> elem in pdata.sowed_plants)
            {
                if (elem.Value.scene == SceneNav.GetCurrentScene() && Plant.GetByUID(elem.Key) == null)
                    Plant.Spawn(elem.Key, spawn_parent.transform);
            }

            //Spawn characters
            foreach (KeyValuePair<string, TrainedCharacterData> elem in pdata.trained_characters)
            {
                if (elem.Value.scene == SceneNav.GetCurrentScene() && Character.GetByUID(elem.Key) == null)
                    Character.Spawn(elem.Key, spawn_parent.transform);
            }

            if (!BlackPanel.Get().IsVisible())
            {
                BlackPanel.Get().Show(true);
                BlackPanel.Get().Hide();
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Check if dead
            PlayerCharacter character = PlayerCharacter.Get();
            if (character.IsDead())
            {
                death_timer += Time.deltaTime;
                if (death_timer > 2f)
                {
                    enabled = false; //Stop running this loop
                    TheUI.Get().ShowGameOver();
                }
            }

            //Game time
            PlayerData pdata = PlayerData.Get();
            float game_speed = GetGameTimeSpeedPerSec();
            pdata.day_time += game_speed * Time.deltaTime;
            if (pdata.day_time >= 24f)
            {
                pdata.day_time = 0f;
                pdata.day++; //New day
            }

            //Set music
            AudioClip[] music_playlist = GameData.Get().music_playlist;
            if (music_playlist.Length > 0 && !TheAudio.Get().IsMusicPlaying("music"))
            {
                AudioClip clip = music_playlist[Random.Range(0, music_playlist.Length)];
                TheAudio.Get().PlayMusic("music", clip, 0.4f, false);
            }

            //Inventory durability
            UpdateDurability();
        }

        private void UpdateDurability()
        {
            PlayerData pdata = PlayerData.Get();
            float game_speed = GetGameTimeSpeedPerSec();

            List<int> remove_items = new List<int>();
            List<string> remove_items_uid = new List<string>();

            //Inventory
            foreach (KeyValuePair<int, InventoryItemData> pair in pdata.inventory)
            {
                InventoryItemData invdata = pair.Value;
                ItemData idata = ItemData.Get(invdata?.item_id);

                if (idata != null && invdata != null && idata.durability_type == DurabilityType.Spoilage)
                {
                    invdata.durability -= game_speed * Time.deltaTime;
                }

                if (idata != null && invdata != null && idata.HasDurability() && invdata.durability <= 0f)
                    remove_items.Add(pair.Key);
            }

            foreach (int slot in remove_items)
            {
                InventoryItemData invdata = pdata.GetItemSlot(slot);
                ItemData idata = ItemData.Get(invdata?.item_id);
                pdata.RemoveItemAt(slot, invdata.quantity);
                if (idata.container_data)
                    pdata.AddItemAt(idata.container_data.id, slot, invdata.quantity, idata.container_data.durability);
            }
            remove_items.Clear();

            //Equipped
            foreach (KeyValuePair<int, InventoryItemData> pair in pdata.equipped_items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                InventoryItemData invdata = pair.Value;
                if (idata != null && invdata != null && (idata.durability_type == DurabilityType.Spoilage || idata.durability_type == DurabilityType.UsageTime))
                {
                    invdata.durability -= game_speed * Time.deltaTime;
                }

                if (idata != null && invdata != null && idata.HasDurability() && invdata.durability <= 0f)
                    remove_items.Add(pair.Key);
            }

            foreach (int slot in remove_items)
            {
                InventoryItemData invdata = pdata.GetEquippedItemSlot(slot);
                ItemData idata = ItemData.Get(invdata?.item_id);
                pdata.UnequipItem(slot);
                if (idata.container_data)
                    pdata.EquipItem(slot, idata.container_data.id, idata.container_data.durability);
            }
            remove_items.Clear();

            //Dropped
            foreach (KeyValuePair<string, DroppedItemData> pair in pdata.dropped_items)
            {
                DroppedItemData ddata = pair.Value;
                ItemData idata = ItemData.Get(ddata?.item_id);

                if (idata != null && ddata != null && idata.durability_type == DurabilityType.Spoilage)
                {
                    ddata.durability -= game_speed * Time.deltaTime;
                }

                if (idata != null && ddata != null && idata.HasDurability() && ddata.durability <= 0f)
                    remove_items_uid.Add(pair.Key);
            }

            foreach (string uid in remove_items_uid)
            {
                Item item = Item.GetByUID(uid);
                if (item != null)
                    item.SpoilItem();
            }
            remove_items_uid.Clear();

            //Stored
            foreach (KeyValuePair<string, StoredItemData> spair in pdata.stored_items)
            {
                if (spair.Value != null)
                {
                    foreach (KeyValuePair<int, InventoryItemData> pair in spair.Value.items)
                    {
                        InventoryItemData invdata = pair.Value;
                        ItemData idata = ItemData.Get(invdata?.item_id);

                        if (idata != null && invdata != null && idata.durability_type == DurabilityType.Spoilage)
                        {
                            invdata.durability -= game_speed * Time.deltaTime;
                        }

                        if (idata != null && invdata != null && idata.HasDurability() && invdata.durability <= 0f)
                            remove_items.Add(pair.Key);
                    }

                    foreach (int slot in remove_items)
                    {
                        InventoryItemData invdata = pdata.GetStoredItemSlot(spair.Value, slot);
                        ItemData idata = ItemData.Get(invdata?.item_id);
                        pdata.RemoveStoredItemAt(spair.Key, slot, invdata.quantity);
                        if (idata.container_data)
                            pdata.AddStoredItemAt(spair.Key, idata.container_data.id, slot, invdata.quantity, idata.container_data.durability);
                    }
                    remove_items.Clear();
                }
            }

            //Constructions
            foreach (KeyValuePair<string, BuiltConstructionData> pair in pdata.built_constructions)
            {
                BuiltConstructionData bdata = pair.Value;
                ConstructionData cdata = ConstructionData.Get(bdata?.construction_id);

                if (cdata != null && bdata != null && (cdata.durability_type == DurabilityType.Spoilage || cdata.durability_type == DurabilityType.UsageTime))
                {
                    bdata.durability -= game_speed * Time.deltaTime;
                }

                if (cdata != null && bdata != null && cdata.HasDurability() && bdata.durability <= 0f)
                    remove_items_uid.Add(pair.Key);
            }

            foreach (string uid in remove_items_uid)
            {
                Construction item = Construction.GetByUID(uid);
                if (item != null)
                    item.Kill();
            }
            remove_items_uid.Clear();

            //Timed bonus
            List<BonusType> remove_bonus_list = new List<BonusType>();
            foreach (KeyValuePair<BonusType, TimedBonusData> pair in pdata.timed_bonus_effects)
            {
                TimedBonusData bdata = pair.Value;
                bdata.time -= game_speed * Time.deltaTime;

                if (bdata.time <= 0f)
                    remove_bonus_list.Add(pair.Key);
            }
            foreach (BonusType bonus in remove_bonus_list)
                PlayerData.Get().RemoveTimedBonus(bonus);
            remove_bonus_list.Clear();

        }

        public bool IsNight()
        {
            PlayerData pdata = PlayerData.Get();
            return pdata.day_time >= 18f || pdata.day_time < 6f;
        }

        //Set to 1f for default speed
        public void SetGameSpeedMultiplier(float mult)
        {
            speed_multiplier = mult;
        }

        //Game hours per real time hours
        public float GetGameTimeSpeed()
        {
            float game_speed = speed_multiplier * GameData.Get().game_time_mult;
            return game_speed;
        }

        //Game hours per real time seconds
        public float GetGameTimeSpeedPerSec()
        {
            float hour_to_sec = GetGameTimeSpeed() / 3600f;
            return hour_to_sec;
        }

        public void Save()
        {
            PlayerData.Get().current_scene = SceneNav.GetCurrentScene();
            PlayerData.Get().current_pos = PlayerCharacter.Get().transform.position;
            PlayerData.Get().current_entry_index = -1; //Go to current_pos
            PlayerData.Get().Save();
        }

        public void Pause()
        {
            paused = true;
            paused_by_player = true;
            if (onPause != null)
                onPause.Invoke(paused);
        }

        public void Unpause()
        {
            paused = false;
            paused_by_player = false;
            if (onPause != null)
                onPause.Invoke(paused);
        }

        public void PauseScripts()
        {
            paused = true;
            paused_by_player = false;
        }

        public void UnpauseScripts()
        {
            paused = false;
            paused_by_player = false;
        }

        public bool IsPaused()
        {
            return paused;
        }

        public bool IsPausedByPlayer()
        {
            return paused_by_player;
        }

        public static bool IsMobile()
        {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#elif UNITY_WEBGL
        return WebGLTool.isMobile();
#else
            return false;
#endif
        }

        public static TheGame Get()
        {
            return _instance;
        }
    }

}