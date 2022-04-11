using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Generates items over time, that can be picked by the player. Examples include bird nest (create eggs), or a fishing spot (create fishes).
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class ItemProvider : MonoBehaviour
    {
        public int item_max = 3;
        public float item_spawn_time = 2f; //In game hours
        public ItemData item;
        public bool take_by_default;

        public AudioClip take_sound;

        public GameObject[] item_models;

        private UniqueID unique_id;

        private int nb_item = 1;
        private float item_progress = 0f;

        void Awake()
        {
            unique_id = GetComponent<UniqueID>();
        }

        private void Start()
        {
            if (PlayerData.Get().HasUniqueID(GetAmountUID()))
                nb_item = PlayerData.Get().GetUniqueID(GetAmountUID());

            if (take_by_default)
                GetComponent<Selectable>().onUse += OnUse;
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            item_progress += game_speed * Time.deltaTime;
            if (item_progress > item_spawn_time)
            {
                item_progress = 0f;
                nb_item += 1;
                nb_item = Mathf.Min(nb_item, item_max);

                PlayerData.Get().SetUniqueID(GetAmountUID(), nb_item);
            }

            for (int i = 0; i < item_models.Length; i++)
            {
                bool visible = (i < nb_item);
                if (item_models[i].activeSelf != visible)
                    item_models[i].SetActive(visible);
            }
        }

        public void RemoveItem()
        {
            if (nb_item > 0)
                nb_item--;

            PlayerData.Get().SetUniqueID(GetAmountUID(), nb_item);
        }

        private void OnUse(PlayerCharacter player)
        {
            if (HasItem())
            {
                string animation = PlayerCharacterAnim.Get() ? PlayerCharacterAnim.Get().take_anim : "";
                PlayerCharacter.Get().TriggerAction(animation, transform.position, 0.5f, () =>
                {
                    RemoveItem();
                    PlayerCharacter.Get().GainItem(item, 1, transform.position);

                    TheAudio.Get().PlaySFX("item", take_sound);
                });
            }
        }

        public bool HasItem()
        {
            return nb_item > 0;
        }

        public int GetNbItem()
        {
            return nb_item;
        }

        public string GetAmountUID()
        {
            if (!string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_amount";
            return "";
        }
    }

}