using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Class that manages the player character attacks, hp and death
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterCombat : MonoBehaviour
    {
        [Header("Combat")]
        public bool can_attack = true;
        public int hand_damage = 5;
        public int base_armor = 0;
        public float attack_speed = 150f;
        public float attack_windup = 0.7f;
        public float attack_windout = 0.7f;
        public float attack_range = 1.2f;

        [Header("Audio")]
        public AudioClip hit_sound;

        public UnityAction<Destructible, bool> onAttack;
        public UnityAction<Destructible> onAttackHit;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        private PlayerCharacter character;
        private PlayerCharacterAttribute character_attr;

        private float attack_timer = 0f;
        private bool is_dead = false;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            character_attr = GetComponent<PlayerCharacterAttribute>();
        }

        void Start()
        {

        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            //Attack when target is in range
            float speed = GetAttackSpeed();
            attack_timer += speed * Time.deltaTime;
            Destructible auto_move_attack = character.GetAutoAttackTarget();
            if (auto_move_attack != null && !character.IsDoingAction() && IsAttackTargetInRange(auto_move_attack))
            {
                character.FaceTorward(auto_move_attack.transform.position);

                if (attack_timer > 100f)
                {
                    DoAttack(auto_move_attack);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (is_dead)
                return;

            if (character.Attributes.GetBonusEffectTotal(BonusType.Invulnerable) > 0.5f)
                return;

            int dam = damage - GetArmor();
            dam = Mathf.Max(dam, 1);

            int invuln = Mathf.RoundToInt(dam * character.Attributes.GetBonusEffectTotal(BonusType.Invulnerable));
            dam = dam - invuln;

            if (dam <= 0)
                return;

            character_attr.AddAttribute(AttributeType.Health, -dam);

            //Durability
            character.Inventory.UpdateAllEquippedItemsDurability(false, -1f);

            character.StopSleep();

            TheCamera.Get().Shake();
            TheAudio.Get().PlaySFX("player", hit_sound);

            if (onDamaged != null)
                onDamaged.Invoke();
        }

        public void Kill()
        {
            if (is_dead)
                return;

            character.StopMove();
            is_dead = true;

            if (onDeath != null)
                onDeath.Invoke();
        }

        //Perform one attack
        public void DoAttack(Destructible resource)
        {
            if (!character.IsDoingAction())
            {
                attack_timer = -10f;
                StartCoroutine(AttackRun(resource));
            }
        }

        public void DoAttackNoTarget()
        {
            if (!character.IsDoingAction() && HasRangedWeapon())
            {
                attack_timer = -10f;
                StartCoroutine(AttackRunNoTarget());
            }
        }

        //Melee or ranged targeting one target
        private IEnumerator AttackRun(Destructible target)
        {
            character.SetDoingAction(true);

            bool is_ranged = target != null && CanWeaponAttackRanged(target);

            //Start animation
            if (onAttack != null)
                onAttack.Invoke(target, is_ranged);

            //Face target
            character.FaceTorward(target.transform.position);

            //Wait for windup
            float windup = GetAttackWindup();
            yield return new WaitForSeconds(windup);

            int nb_strikes = GetAttackStrikes(target);
            float strike_interval = GetAttackStikesInterval(target);

            while (nb_strikes > 0)
            {
                DoAttackStrike(target, is_ranged);
                yield return new WaitForSeconds(strike_interval);
                nb_strikes--;
            }

            //Durability
            character.Inventory.UpdateAllEquippedItemsDurability(true, -1f);

            attack_timer = 0f;

            //Wait for the end of the attack before character can move again
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            character.SetDoingAction(false);
        }

        //Ranged attack without a target
        private IEnumerator AttackRunNoTarget()
        {
            character.SetDoingAction(true);

            //Rotate toward 
            bool freerotate = TheCamera.Get().IsFreeRotation();
            if (freerotate)
                character.FaceTorward(transform.position + TheCamera.Get().GetFacingFront());

            //Start animation
            if (onAttack != null)
                onAttack.Invoke(null, true);

            //Wait for windup
            float windup = GetAttackWindup();
            yield return new WaitForSeconds(windup);

            int nb_strikes = GetAttackStrikes();
            float strike_interval = GetAttackStikesInterval();

            while (nb_strikes > 0)
            {
                DoRangedAttackStrike();
                yield return new WaitForSeconds(strike_interval);
                nb_strikes--;
            }

            //Durability
            character.Inventory.UpdateAllEquippedItemsDurability(true, -1f);

            attack_timer = 0f;

            //Wait for the end of the attack before character can move again
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            character.SetDoingAction(false);
        }

        private void DoAttackStrike(Destructible target, bool is_ranged)
        {
            //Ranged attack
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (target != null && is_ranged && equipped != null)
            {
                InventoryItemData projectile_inv = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(projectile_inv?.item_id);
                if (projectile != null && CanWeaponAttackRanged(target))
                {
                    character.Inventory.UseItem(projectile, 1);
                    Vector3 pos = GetProjectileSpawnPos(equipped);
                    Vector3 dir = target.GetCenter() - pos;
                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    proj.GetComponent<Projectile>().dir = dir.normalized;
                    proj.GetComponent<Projectile>().damage = equipped.damage;
                }
            }

            //Melee attack
            else if (IsAttackTargetInRange(target))
            {
                target.TakeDamage(GetAttackDamage(target));

                if (onAttackHit != null)
                    onAttackHit.Invoke(target);
            }
        }

        //Strike without target
        private void DoRangedAttackStrike()
        {
            //Ranged attack
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && equipped.ranged)
            {
                InventoryItemData projectile_inv = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(projectile_inv?.item_id);
                if (projectile != null)
                {
                    character.Inventory.UseItem(projectile, 1);
                    Vector3 pos = GetProjectileSpawnPos(equipped);
                    Vector3 dir = transform.forward;
                    bool freerotate = TheCamera.Get().IsFreeRotation();
                    if (freerotate)
                        dir = TheCamera.Get().GetAimDir(character);

                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    proj.GetComponent<Projectile>().dir = dir.normalized;
                    proj.GetComponent<Projectile>().damage = equipped.damage;
                }
            }
        }

        //Does Attack has priority on actions?
        public bool CanAutoAttack(Destructible target)
        {
            bool has_required_item = target != null && target.required_item != null && character.EquipData.HasItemInGroup(target.required_item);
            return CanAttack(target) && (has_required_item || target.attack_group == AttackGroup.Enemy || !target.GetSelectable().CanAutoInteract());
        }

        //Can it be attacked at all?
        public bool CanAttack(Destructible target)
        {
            return can_attack && target != null && target.CanBeAttacked()
                && (target.required_item != null || target.attack_group != AttackGroup.Ally) //Cant attack allied unless has required item
                && (target.required_item == null || character.EquipData.HasItemInGroup(target.required_item)); //Cannot attack unless has equipped item
        }

        public int GetAttackDamage(Destructible target)
        {
            int damage = hand_damage;

            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                damage = equipped.damage;

            damage += Mathf.RoundToInt(damage * character.Attributes.GetBonusEffectTotal(BonusType.AttackBoost));

            return damage;
        }

        public float GetAttackRange(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                return equipped.range;
            return attack_range;
        }

        public int GetAttackStrikes(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        public float GetAttackStikesInterval(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        public int GetAttackStrikes()
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null)
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        public float GetAttackStikesInterval()
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null)
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        public float GetAttackWindup()
        {
            EquipItem item_equip = character.Inventory.GetEquippedItemMesh(EquipSlot.Hand);
            if (item_equip != null)
                return item_equip.attack_windup;
            return attack_windup;
        }

        public float GetAttackWindout()
        {
            EquipItem item_equip = character.Inventory.GetEquippedItemMesh(EquipSlot.Hand);
            if (item_equip != null)
                return item_equip.attack_windout;
            return attack_windout;
        }

        public Vector3 GetProjectileSpawnPos(ItemData weapon)
        {
            EquipAttach attach = character.Inventory.GetEquipAttachment(EquipSlot.Hand, weapon.equip_side);
            if (attach != null)
                return attach.transform.position;
            return transform.position + Vector3.up;
        }

        public float GetAttackSpeed()
        {
            return attack_speed;
        }

        //Make sure the current equipped weapon can hit target, and has enough bullets
        public bool CanWeaponHitTarget(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            bool valid_ranged = equipped != null && equipped.ranged && CanWeaponAttackRanged(target);
            bool valid_melee = equipped != null && !equipped.ranged;
            return valid_melee || valid_ranged;
        }

        //Check if target is valid for ranged attack, and if enough bullets
        public bool CanWeaponAttackRanged(Destructible destruct)
        {
            if (destruct == null)
                return false;

            return destruct.CanAttackRanged() && HasRangedProjectile();
        }

        public bool HasRangedWeapon()
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            return (equipped != null && equipped.ranged);
        }

        public bool HasRangedProjectile()
        {
            ItemData equipped = character.EquipData.GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && equipped.ranged)
            {
                InventoryItemData invdata = character.InventoryData.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(invdata?.item_id);
                return projectile != null && character.Inventory.HasItem(projectile);
            }
            return false;
        }

        public float GetTargetAttackRange(Destructible target)
        {
            return GetAttackRange(target) + target.hit_range;
        }

        public bool IsAttackTargetInRange(Destructible target)
        {
            if (target != null)
            {
                float dist = (target.transform.position - transform.position).magnitude;
                return dist < GetTargetAttackRange(target);
            }
            return false;
        }

        public int GetArmor()
        {
            int armor = base_armor;
            foreach (KeyValuePair<int, InventoryItemData> pair in character.EquipData.items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                    armor += idata.armor;
            }

            armor += Mathf.RoundToInt(armor * character.Attributes.GetBonusEffectTotal(BonusType.ArmorBoost));

            return armor;
        }

        //Count total number of things killed of that type
        public int CountTotalKilled(CraftData craftable)
        {
            if (craftable != null)
                return character.Data.GetKillCount(craftable.id);
            return 0;
        }

        public void ResetKillCount(CraftData craftable)
        {
            if (craftable != null)
                character.Data.ResetKillCount(craftable.id);
        }

        public void ResetKillCount()
        {
            character.Data.ResetKillCount();
        }

        public bool IsDead()
        {
            return is_dead;
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}
