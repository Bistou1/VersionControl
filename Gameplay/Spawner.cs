using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Spawns random prefabs in the scene at an interval.
    /// WARNING: this scripts was more for testing purposes and is not integrated with the save system yet.
    /// </summary>

    public class Spawner : MonoBehaviour
    {
        public float spawn_interval = 8f; //In game hours
        public float spawn_radius = 1f;
        public int spawn_max = 1;
        public GameObject[] spawn_prefabs;

        private float spawn_timer = 0f;

        private List<GameObject> spawn_list = new List<GameObject>();

        void Start()
        {

        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Remove null
            for (int i = spawn_list.Count - 1; i >= 0; i--)
            {
                if (spawn_list[i] == null || !spawn_list[i].activeSelf)
                {
                    spawn_list.RemoveAt(i);
                }
            }

            if (!IsFull())
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

                spawn_timer += game_speed * Time.deltaTime;
                if (spawn_timer > spawn_interval)
                {
                    spawn_timer = 0f;
                    Spawn();
                }
            }

        }

        public void Spawn()
        {
            GameObject prefab = spawn_prefabs[Random.Range(0, spawn_prefabs.Length)];
            float radius = Random.Range(0f, spawn_radius);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 pos = transform.position + offset;
            GameObject obj = Instantiate(prefab, pos, prefab.transform.rotation);
            spawn_list.Add(obj);

            if (obj.GetComponent<UniqueID>())
            {
                obj.GetComponent<UniqueID>().unique_id = UniqueID.GenerateUniqueID();
            }
        }

        public bool IsFull()
        {
            return spawn_prefabs.Length == 0 || spawn_list.Count >= spawn_max;
        }
    }

}