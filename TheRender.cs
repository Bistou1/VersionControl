﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Optimize rendering and apply visual effects
    /// Author: Indie Marc (Marc-Antoine Desbiens)
    /// </summary>

    public class TheRender : MonoBehaviour
    {
        [Header("Optimization")]
        public float refresh_rate = 0.5f; //In seconds, interval at which selectable are shown/hidden
        public float distance_multiplier = 1f; //will make all selectable active_range multiplied
        public float active_area_facing_offset = 10f; //active area will be offset by X in the direction the camera is facing
        public bool turn_off_gameobjects = false; //If on, will turn off the whole gameObjects, otherwise will just turn off scripts

        private Light dir_light;
        private Quaternion start_rot;
        private float update_timer = 0f;

        void Start()
        {
            //Light
            GameData gdata = GameData.Get();
            bool is_night = TheGame.Get().IsNight();
            dir_light = FindObjectOfType<Light>();
            float target = is_night ? gdata.night_light_ambient_intensity : gdata.day_light_ambient_intensity;
            float light_angle = PlayerData.Get().day_time * 360f / 24f;
            RenderSettings.ambientIntensity = target;
            if (dir_light != null && dir_light.type == LightType.Directional)
            {
                start_rot = dir_light.transform.rotation;
                dir_light.intensity = is_night ? gdata.night_light_dir_intensity : gdata.day_light_dir_intensity;
                dir_light.shadowStrength = is_night ? 0f : 1f;
                if (gdata.rotate_shadows)
                    dir_light.transform.rotation = Quaternion.Euler(0f, light_angle + 180f, 0f) * start_rot;
            }
        }

        void Update()
        {

            //Day night
            GameData gdata = GameData.Get();
            bool is_night = TheGame.Get().IsNight();
            float target = is_night ? gdata.night_light_ambient_intensity : gdata.day_light_ambient_intensity;
            float light_angle = PlayerData.Get().day_time * 360f / 24f;
            RenderSettings.ambientIntensity = Mathf.MoveTowards(RenderSettings.ambientIntensity, target, 0.2f * Time.deltaTime);
            if (dir_light != null && dir_light.type == LightType.Directional)
            {
                float dtarget = is_night ? gdata.night_light_dir_intensity : gdata.day_light_dir_intensity;
                dir_light.intensity = Mathf.MoveTowards(dir_light.intensity, dtarget, 0.2f * Time.deltaTime);
                dir_light.shadowStrength = Mathf.MoveTowards(dir_light.shadowStrength, is_night ? 0f : 1f, 0.2f * Time.deltaTime);
                if (gdata.rotate_shadows)
                    dir_light.transform.rotation = Quaternion.Euler(0f, light_angle + 180f, 0f) * start_rot;
            }

            //Slow update
            update_timer += Time.deltaTime;
            if (update_timer > refresh_rate)
            {
                update_timer = 0f;
                SlowUpdate();
            }
        }

        void SlowUpdate()
        {
            //Optimization
            Vector3 center_pos = TheCamera.Get().GetTargetPosOffsetFace(active_area_facing_offset);
            foreach (Selectable select in Selectable.GetAll())
            {
                float dist = (select.GetPosition() - center_pos).magnitude;
                select.SetActive(dist < select.active_range * distance_multiplier, turn_off_gameobjects);
            }
        }
    }

}