using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterAttribute : MonoBehaviour
    {
        [Header("Attributes")]
        public AttributeData[] attributes;

        private PlayerCharacter character;

        private float move_speed_mult = 1f;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        void Start()
        {
            //Init attributes
            foreach (AttributeData attr in attributes)
            {
                if (!CharacterData.HasAttribute(attr.type))
                    CharacterData.SetAttributeValue(attr.type, attr.start_value, attr.max_value);
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            //Update attributes
            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            //Update Attributes
            foreach (AttributeData attr in attributes)
            {
                float update_value = attr.value_per_hour + GetBonusEffectTotal(BonusEffectData.GetAttributeBonusType(attr.type));
                update_value = update_value * game_speed * Time.deltaTime;
                CharacterData.AddAttributeValue(attr.type, update_value, attr.max_value);
            }

            //Penalty for depleted attributes
            move_speed_mult = 1f + GetBonusEffectTotal(BonusType.SpeedBoost);

            foreach (AttributeData attr in attributes)
            {
                if (GetAttributeValue(attr.type) < 0.01f)
                {
                    move_speed_mult = move_speed_mult * attr.deplete_move_mult;
                    float update_value = attr.deplete_hp_loss * game_speed * Time.deltaTime;
                    AddAttribute(AttributeType.Health, update_value);
                }
            }

            //Dying
            float health = GetAttributeValue(AttributeType.Health);
            if (health < 0.01f)
                character.Kill();

            //Sleeps add attributes
            if (character.IsSleeping())
            {
                ActionSleep sleep_target = character.GetSleepTarget();
                AddAttribute(AttributeType.Health, sleep_target.sleep_hp_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Hunger, sleep_target.sleep_hunger_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Thirst, sleep_target.sleep_thirst_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Happiness, sleep_target.sleep_hapiness_hour * game_speed * Time.deltaTime);
            }
        }

        public void GainXP(int xp)
        {
            CharacterData.GainXP(xp);
        }

        public void AddAttribute(AttributeType type, float value)
        {
            CharacterData.AddAttributeValue(type, value, GetAttributeMax(type));
        }

        public void SetAttribute(AttributeType type, float value)
        {
            CharacterData.SetAttributeValue(type, value, GetAttributeMax(type));
        }

        public float GetAttributeValue(AttributeType type)
        {
            return CharacterData.GetAttributeValue(type);
        }

        public float GetAttributeMax(AttributeType type)
        {
            AttributeData adata = GetAttribute(type);
            if (adata != null)
                return adata.max_value;
            return 100f;
        }

        public AttributeData GetAttribute(AttributeType type)
        {
            foreach (AttributeData attr in attributes)
            {
                if (attr.type == type)
                    return attr;
            }
            return null;
        }

        public float GetBonusEffectTotal(BonusType type)
        {
            float value = 0f;

            //Equip bonus
            foreach (KeyValuePair<int, InventoryItemData> pair in character.EquipData.items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                {
                    foreach (BonusEffectData bonus in idata.equip_bonus)
                    {
                        if (bonus.type == type)
                            value += bonus.value;
                    }
                }
            }

            //Aura bonus
            foreach (BonusAura aura in BonusAura.GetAll())
            {
                float dist = (aura.transform.position - transform.position).magnitude;
                if (aura.effect.type == type && dist < aura.range)
                    value += aura.effect.value;
            }

            //Timed bonus
            value += CharacterData.GetTotalTimedBonus(type);

            return value;
        }

        public float GetSpeedMult()
        {
            return move_speed_mult;
        }

        public PlayerCharacterData CharacterData
        {
            get { return character.Data; }
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}
