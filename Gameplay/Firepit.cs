using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Firepits can be fueled with wood or other materials. Will be lit until it run out of fuel
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Construction))]
    public class Firepit : MonoBehaviour
    {
        public GroupData fire_group;
        public GameObject fire_fx;
        public GameObject fuel_model;

        public float start_fuel = 10f;
        public float max_fuel = 50f;
        public float fuel_per_hour = 1f; //In Game hours
        public float wood_add_fuel = 2f;

        private Selectable select;
        private Construction construction;
        private Buildable buildable;
        private UniqueID unique_id;

        private bool is_on = false;
        private float fuel = 0f;

        void Awake()
        {
            select = GetComponent<Selectable>();
            construction = GetComponent<Construction>();
            buildable = GetComponent<Buildable>();
            unique_id = GetComponent<UniqueID>();
            if (fire_fx)
                fire_fx.SetActive(false);
            if (fuel_model)
                fuel_model.SetActive(false);
        }

        private void Start()
        {
            //select.onUse += OnUse;
            select.RemoveGroup(fire_group);
            buildable.onBuild += OnFinishBuild;

            if (!construction.was_spawned && !buildable.IsBuilding())
                fuel = start_fuel;
            if (PlayerData.Get().HasUniqueID(GetFireUID()))
                fuel = PlayerData.Get().GetUniqueID(GetFireUID());
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (is_on)
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
                fuel -= game_speed * Time.deltaTime;

                PlayerData.Get().SetUniqueID(GetFireUID(), Mathf.RoundToInt(fuel));
            }

            is_on = fuel > 0f;
            if (fire_fx)
                fire_fx.SetActive(is_on);
            if (fuel_model)
                fuel_model.SetActive(fuel > 0f);

            if (is_on)  
                select.AddGroup(fire_group);
            else
                select.RemoveGroup(fire_group);
        }

        public void AddFuel(float value)
        {
            fuel += value;
            is_on = fuel > 0f;

            PlayerData.Get().SetUniqueID(GetFireUID(), Mathf.RoundToInt(fuel));
        }

        private void OnFinishBuild()
        {
            fuel = start_fuel;
        }

        public string GetFireUID()
        {
            if(!string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_fire";
            return "";
        }

        public bool IsOn()
        {
            return is_on;
        }
    }

}
