using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    [RequireComponent(typeof(Selectable))]
    public class Furnace : MonoBehaviour
    {
        public GameObject spawn_point;

        public GameObject active_fx;
        public AudioClip put_audio;
        public AudioClip finish_audio;

        private Selectable select;
        private ItemData prev_item = null;
        private ItemData current_item = null;
        private int current_quantity= 0;
        private float timer = 0f;
        private float duration = 0f; //In game hours

        private static List<Furnace> furnace_list = new List<Furnace>();

        void Awake()
        {
            furnace_list.Add(this);
            select = GetComponent<Selectable>();
        }

        private void OnDestroy()
        {
            furnace_list.Remove(this);
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (HasItem())
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
                timer += game_speed * Time.deltaTime;
                if (timer > duration)
                {
                    FinishItem();
                }

                if (active_fx != null && active_fx.activeSelf != HasItem())
                    active_fx.SetActive(HasItem());
            }
        }

        public void PutItem(ItemData item, ItemData create, float duration, int quantity)
        {
            if (current_item == null || item == current_item)
            {
                prev_item = item;
                current_item = create;
                current_quantity += quantity;
                timer = 0f;
                this.duration = duration;

                if (select.IsNearCamera(10f))
                    TheAudio.Get().PlaySFX("furnace", put_audio);
            }
        }

        public void FinishItem()
        {
            if (current_item != null) {

                Item.Create(current_item, spawn_point.transform.position, current_quantity);

                prev_item = null;
                current_item = null;
                current_quantity = 0;
                timer = 0f;

                if (active_fx != null)
                    active_fx.SetActive(false);

                if (select.IsNearCamera(10f))
                    TheAudio.Get().PlaySFX("furnace", finish_audio);
            }
        }

        public bool HasItem()
        {
            return current_item != null;
        }

        public static Furnace GetNearestInRange(Vector3 pos, float range=999f)
        {
            float min_dist = range;
            Furnace nearest = null;
            foreach (Furnace furnace in furnace_list)
            {
                float dist = (pos - furnace.transform.position).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = furnace;
                }
            }
            return nearest;
        }

        public static List<Furnace> GetAll()
        {
            return furnace_list;
        }
    }

}
