using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{

    [System.Serializable]
    public struct BiomeSpawn
    {
        public GameObject prefab;
        public float probability;
    }

    [CreateAssetMenu(fileName = "BiomeSpawn", menuName = "Data/BiomeSpawn", order = 10)]
    public class BiomeSpawnData : ScriptableObject
    {
        [Header("Spawns")]
        [Range(1f, 100f)]
        [Tooltip("Density determine the quantity of objects relative to group size")]
        public float density = 50f;
        [Range(1f, 100f)]
        [Tooltip("Variance determine if objects are evenly distributed or not")]
        public float variance = 50f;
       
        public BiomeSpawn[] spawns;

        public GameObject PickRandomPrefab()
        {
            float spawn_total_prob = 0f;
            foreach (BiomeSpawn biome in spawns)
                spawn_total_prob += biome.probability;

            float value = Random.Range(0f, spawn_total_prob);
            foreach (BiomeSpawn biome in spawns)
            {
                if (value < biome.probability)
                {
                    return biome.prefab;
                }
                else
                {
                    value -= biome.probability;
                }
            }
            return null;
        }
    }

}