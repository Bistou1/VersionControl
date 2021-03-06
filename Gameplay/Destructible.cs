using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum AttackGroup
    {
        Neutral=0, //Can be attacked by anyone, but won't be attacked automatically, use for resources
        Ally=10, //Will be attacked automatically by wild animals, cant be attacked by the player unless it has the required_item.
        Enemy=20, //Will be attacked automatically by allied pets and wild animals (unless in same team group), can be attacked by anyone.
        CantAttack =50, //Cannot be attacked
    }

    [System.Serializable]
    public struct RandomLoot
    {
        public CraftData item;
        public float probability; //Between 0 and 1
    }

    /// <summary>
    /// Destructibles are objects that can be destroyed. They have HP and can be damaged by the player or by animals. 
    /// They often spawn loot items when destroyed (or killed)
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Destructible : MonoBehaviour
    {
        [Header("Stats")]
        public int hp = 100;
        public int armor = 0; //Reduces each attack's damage by the armor value

        [Header("Targeting")]
        public AttackGroup attack_group; //Check above for description of each group
        public GroupData team_group; //Enemies of the same group won't attack each other.
        public GroupData required_item; //Required item to attack it (Only the player is affected by this)
        public bool attack_melee_only = true; //If set to true, this object cannot be attacked with a ranged weapon
        public float hit_range = 1f; //Range from which it can be attacked

        [Header("Loot")]
        public int xp = 0;
        public CraftData[] loots;
        public RandomLoot[] loots_random;

        [Header("FX")]
        public bool shake_on_hit = true; //Shake animation when hit
        public float destroy_delay = 0f; //In seconds, use this if you want a death animation before the object disappears
        public GameObject attack_center; //For projectiles mostly, since most objects have their pivot directly on the floor, we sometimes dont want projectiles to aim at the pivot but at this position instead
        public GameObject death_fx; //Prefab spawned then dying
        public AudioClip hit_sound;
        public AudioClip death_sound;

        //Events
        public UnityAction onDamaged;
        public UnityAction onDeath;

        private bool dead = false;

        private Selectable select;
        private Collider[] colliders;
        private UniqueID unique_id;
        private Vector3 shake_center;
        private Vector3 shake_vector = Vector3.zero;
        private bool is_shaking = false;
        private float shake_timer = 0f;
        private float shake_intensity = 1f;
        private int max_hp;

        void Awake()
        {
            shake_center = transform.position;
            unique_id = GetComponent<UniqueID>();
            select = GetComponent<Selectable>();
            colliders = GetComponentsInChildren<Collider>();
            max_hp = hp;
        }

        private void Start()
        {
            if (PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            if (HasUID() && PlayerData.Get().HasUniqueID(GetHpUID()))
            {
                hp = PlayerData.Get().GetUniqueID(GetHpUID());
            }
        }

        void Update()
        {
            //Shake FX
            if (is_shaking)
            {
                shake_timer -= Time.deltaTime;

                if (shake_timer > 0f)
                {
                    shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 16f) * 0.02f, 0f, Mathf.Sin(shake_timer * Mathf.PI * 8f) * 0.01f);
                    transform.position += shake_vector * shake_intensity;
                }
                else if (shake_timer > -0.5f)
                {
                    transform.position = Vector3.Lerp(transform.position, shake_center, 4f * Time.deltaTime);
                }
                else
                {
                    is_shaking = false;
                }
            }
        }

        //Deal damages to the destructible, if it reaches 0 HP it will be killed
        public void DealDamage(int damage)
        {
            if (!dead)
            {
                int adamage = Mathf.Max(damage - armor, 1);
                hp -= adamage;

                if (onDamaged != null)
                    onDamaged.Invoke();

                if (shake_on_hit)
                    ShakeFX();

                if (hp <= 0)
                    Kill();

                PlayerData.Get().SetUniqueID(GetHpUID(), hp);

                if (select.IsActive() && select.IsNearCamera(20f))
                    TheAudio.Get().PlaySFX("destruct", hit_sound);
            }
        }

        public void Heal(int value)
        {
            if (!dead)
            {
                hp += value;
                hp = Mathf.Min(hp, max_hp);

                PlayerData.Get().SetUniqueID(GetHpUID(), hp);
            }
        }

        //Kill the destructible
        public void Kill()
        {
            if (!dead)
            {
                dead = true;
                hp = 0;

                foreach (Collider collide in colliders)
                    collide.enabled = false;

                //Loot
                foreach (CraftData item in loots)
                {
                    SpawnLoot(item);
                }

                foreach (RandomLoot loot in loots_random)
                {
                    if (Random.value < loot.probability)
                    {
                        SpawnLoot(loot.item);
                    }
                }

                PlayerData.Get().xp += xp;

                //Loot storage
                StoredItemData sdata = PlayerData.Get().GetStoredData(GetUID());
                if (sdata != null)
                {
                    foreach (KeyValuePair<int, InventoryItemData> item in sdata.items)
                    {
                        ItemData idata = ItemData.Get(item.Value.item_id);
                        if (idata != null && item.Value.quantity > 0)
                        {
                            SpawnLoot(idata, item.Value.quantity, item.Value.durability);
                        }
                    }
                }

                PlayerData.Get().RemoveObject(GetUID());
                PlayerData.Get().RemoveUniqueID(GetHpUID());

                if (onDeath != null)
                    onDeath.Invoke();

                //FX
                if (select.IsActive() && select.IsNearCamera(20f))
                {
                    if (death_fx != null)
                        Instantiate(death_fx, transform.position, Quaternion.identity);

                    TheAudio.Get().PlaySFX("destruct", death_sound);
                }

                select.Destroy(destroy_delay);
            }
        }

        public void SpawnLoot(CraftData item, int quantity=1, float durability=0f)
        {
            float radius = Random.Range(0.5f, 1f);
            float angle = Random.Range(0f, 360f) * Mathf.Rad2Deg;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (item is ItemData)
            {
                ItemData aitem = (ItemData)item;
                float idura = durability > 0.01f ? durability : aitem.durability; 
                Item.Create(aitem, pos, quantity, idura);
            }
            if (item is ConstructionData)
            {
                ConstructionData construct_data = (ConstructionData)item;
                Construction.Create(construct_data, pos);
            }
            if (item is PlantData)
            {
                PlantData plant_data = (PlantData)item;
                Plant.Create(plant_data, pos, 0);
            }
        }

        //Delayed kill (useful if the attacking character doing an animation before destroying this)
        public void KillIn(float delay)
        {
            StartCoroutine(KillInRun(delay));
        }

        private IEnumerator KillInRun(float delay)
        {
            yield return new WaitForSeconds(delay);
            Kill();
        }

        public void ShakeFX(float intensity = 1f, float duration = 0.2f)
        {
            is_shaking = true;
            shake_center = transform.position;
            shake_intensity = intensity;
            shake_timer = duration;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public string GetHpUID()
        {
            if (HasUID())
                return unique_id.unique_id + "_hp";
            return "";
        }

        public bool IsDead()
        {
            return dead;
        }

        public Vector3 GetCenter()
        {
            if (attack_center != null)
                return attack_center.transform.position;
            return transform.position + Vector3.up * 0.1f; //Bit higher than floor
        }

        public bool CanBeAttacked()
        {
            return attack_group != AttackGroup.CantAttack && !dead;
        }

        public bool CanAttackRanged()
        {
            return CanBeAttacked() && !attack_melee_only;
        }

        public int GetMaxHP()
        {
            return max_hp;
        }

        public Selectable GetSelectable()
        {
            return select;
        }

        //Get nearest auto attack for player
        public static Destructible GetNearestAutoAttack(Vector3 pos, float range = 999f)
        {
            Destructible nearest = null;
            float min_dist = range;
            foreach (Selectable selectable in Selectable.GetAllActive()) //Loop on active selectables only for optimization
            {
                Destructible destruct = selectable.GetDestructible();
                if (destruct != null && selectable.IsActive() && !destruct.IsDead() && (destruct.attack_group == AttackGroup.Neutral || destruct.attack_group == AttackGroup.Enemy))
                {
                    float dist = (destruct.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = destruct;
                    }
                }
            }
            return nearest;
        }

        //Get nearest active destructible selectable
        public static Destructible GetNearestDestructible(Vector3 pos, float range = 999f)
        {
            Destructible nearest = null;
            float min_dist = range;
            foreach (Selectable selectable in Selectable.GetAllActive()) //Loop on active selectables only for optimization
            {
                Destructible destruct = selectable.GetDestructible();
                if (destruct != null && selectable.IsActive())
                {
                    float dist = (destruct.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = destruct;
                    }
                }
            }
            return nearest;
        }
    }

}