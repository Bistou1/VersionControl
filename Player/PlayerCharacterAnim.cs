using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Manages all character animations
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterAnim : MonoBehaviour
    {
        public string move_anim = "Move";
        public string attack_anim = "Attack";
        public string take_anim = "Take";
        public string build_anim = "Build";
        public string damaged_anim = "Damaged";
        public string death_anim = "Death";
        public string sleep_anim = "Sleep";
        public string fish_anim = "Fish";
        public string dig_anim = "Dig";
        public string water_anim = "Water";

        private PlayerCharacter character;
        private PlayerCharacterEquip character_equip;
        private Animator animator;

        private static PlayerCharacterAnim _instance;

        void Awake()
        {
            _instance = this;
            character = GetComponent<PlayerCharacter>();
            character_equip = GetComponent<PlayerCharacterEquip>();
            animator = GetComponentInChildren<Animator>();

            character.onTakeItem += OnTake;
            character.onBuild += OnBuild;
            character.onAttack += OnAttack;
            character.onAttackHit += OnAttackHit;
            character.onJump += OnJump;
            character.onDamaged += OnDamaged;
            character.onDeath += OnDeath;
            character.onTriggerAnim += OnTriggerAnim;

            if (animator == null)
                enabled = false;
        }

        void Update()
        {
            bool paused = TheGame.Get().IsPaused();
            animator.enabled = !paused;

            if (animator.enabled)
            {
                animator.SetBool(move_anim, character.IsMoving());
                animator.SetBool(sleep_anim, character.IsSleeping());
                animator.SetBool(fish_anim, character.IsFishing());
            }
        }

        private void OnTake(Item item)
        {
            animator.SetTrigger(take_anim);
        }

        private void OnBuild(Buildable construction)
        {
            animator.SetTrigger(build_anim);
        }

        private void OnJump()
        {
            //Add jump animation here
        }

        private void OnDamaged()
        {
            animator.SetTrigger(damaged_anim);
        }

        private void OnDeath()
        {
            animator.SetTrigger(death_anim);
        }

        private void OnAttack(Destructible target, bool ranged)
        {
            string anim = attack_anim;

            //Replace anim based on current equipped item
            if (character_equip != null)
            {
                EquipItem equip = character_equip.GetEquippedItem(EquipSlot.Hand);
                if (equip != null)
                {
                    if (!ranged && !string.IsNullOrEmpty(equip.attack_melee_anim))
                        anim = equip.attack_melee_anim;
                    if (ranged && !string.IsNullOrEmpty(equip.attack_ranged_anim))
                        anim = equip.attack_ranged_anim;
                }
            }

            animator.SetTrigger(anim);
        }

        private void OnAttackHit(Destructible target)
        {

        }

        private void OnTriggerAnim(string anim, float duration)
        {
            if(!string.IsNullOrEmpty(anim))
                animator.SetTrigger(anim);
        }

        public static PlayerCharacterAnim Get()
        {
            return _instance;
        }
    }

}