using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Main character script
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Movement")]
        public float move_speed = 4f;
        public float move_accel = 8;
        public float rotate_speed = 180f;
        public float fall_speed = 20f;
        public float ground_detect_dist = 0.1f;
        public LayerMask ground_layer = ~0;
        public bool use_navmesh = false;

        [Header("Combat")]
        public int hand_damage = 5;
        public int base_armor = 0;
        public float attack_speed = 150f;
        public float attack_windup = 0.7f;
        public float attack_windout = 0.7f;
        public float attack_range = 1.2f;

        [Header("Jump")]
        public bool can_jump = false;
        public float jump_power = 10f;
        public float jump_gravity = 20f;
        public float jump_duration = 0.5f;

        [Header("Attributes")]
        public AttributeData[] attributes;

        [Header("Craft")]
        public float construct_range = 4f;

        [Header("Audio")]
        public AudioClip hit_sound;

        public UnityAction<Item> onTakeItem;
        public UnityAction<Item> onDropItem;
        public UnityAction<ItemData> onGainItem;
        public UnityAction<Buildable> onBuild;
        public UnityAction<Destructible, bool> onAttack;
        public UnityAction<Destructible> onAttackHit;
        public UnityAction onDamaged;
        public UnityAction onJump;
        public UnityAction onDeath;
        public UnityAction<string, float> onTriggerAnim;

        private Rigidbody rigid;
        private CapsuleCollider collide;
        private PlayerCharacterEquip character_equip;

        private Vector3 move;
        private Vector3 facing;
        private Vector3 move_average;
        private Vector3 prev_pos;
        private Vector3 jump_vect;

        private bool auto_move = false;
        private Vector3 auto_move_target;
        private Vector3 auto_move_target_next;
        private Selectable auto_move_select = null;
        private Destructible auto_move_attack = null;
        private int auto_move_drop = -1;
        private int auto_move_drop_equip = -1;
        private float auto_move_timer = 0f;
        private float move_speed_mult = 1f;
        private bool controls_enabled = true;

        private bool is_grounded = false;
        private bool is_fronted = false;
        private bool is_action = false;
        private bool is_sleep = false;
        private bool is_dead = false;
        private bool is_fishing = false;

        private float attack_timer = 0f;
        private float jump_timer = 0f;

        private Buildable current_buildable = null;
        private ActionSleep sleep_target = null;
        private bool clicked_build = false;
        private bool build_pay_cost = false;
        private UnityAction<Buildable> build_callback = null;

        private Vector3[] nav_paths = new Vector3[0];
        private int path_index = 0;
        private bool calculating_path = false;
        private bool path_found = false;

        private EquipAttach[] equip_attachments;

        private static PlayerCharacter _instance;

        void Awake()
        {
            _instance = this;
            rigid = GetComponent<Rigidbody>();
            collide = GetComponentInChildren<CapsuleCollider>();
            character_equip = GetComponent<PlayerCharacterEquip>();
            equip_attachments = GetComponentsInChildren<EquipAttach>();
            facing = transform.forward;
            prev_pos = transform.position;
            jump_vect = Vector3.down * fall_speed;
        }

        private void Start()
        {
            PlayerControlsMouse mouse_controls = PlayerControlsMouse.Get();
            mouse_controls.onClickFloor += OnClickFloor;
            mouse_controls.onClickObject += OnClickObject;
            mouse_controls.onClick += OnClick;
            mouse_controls.onRightClick += OnRightClick;
            mouse_controls.onHold += OnMouseHold;

            //Init attributes
            foreach (AttributeData attr in attributes)
            {
                if (!PlayerData.Get().HasAttribute(attr.type))
                    PlayerData.Get().SetAttributeValue(attr.type, attr.start_value);
            }
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
            {
                rigid.velocity = Vector3.zero;
                return;
            }

            if (is_dead)
                return;

            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mcontrols = PlayerControlsMouse.Get();
            Vector3 tmove = Vector3.zero;

            //Update auto move for moving targets
            GameObject auto_move_obj = null;
            if (auto_move_select != null && !auto_move_select.surface)
                auto_move_obj = auto_move_select.gameObject;
            if (auto_move_attack != null)
                auto_move_obj = auto_move_attack.gameObject;

            if (auto_move && auto_move_obj != null)
            {
                Vector3 diff = auto_move_obj.transform.position - auto_move_target;
                if (diff.magnitude > 1f)
                {
                    auto_move_target = auto_move_obj.transform.position;
                    auto_move_target_next = auto_move_obj.transform.position;
                    CalculateNavmesh();
                }
            }

            //Navmesh next path
            if (auto_move && use_navmesh && path_found && path_index < nav_paths.Length)
            {
                auto_move_target_next = nav_paths[path_index];
                Vector3 move_dir_total = auto_move_target_next - transform.position;
                move_dir_total.y = 0f;
                if (move_dir_total.magnitude < 0.2f)
                    path_index++;
            }

            //Moving
            auto_move_timer += Time.fixedDeltaTime;
            if (auto_move && auto_move_timer > 0.02f) //auto_move_timer to let the navmesh time to calculate a path
            {
                Vector3 move_dir_total = auto_move_target - transform.position;
                Vector3 move_dir_next = auto_move_target_next - transform.position;
                Vector3 move_dir = move_dir_next.normalized * Mathf.Min(move_dir_total.magnitude, 1f);
                move_dir.y = 0f;

                float move_dist = Mathf.Min(move_speed * move_speed_mult, move_dir.magnitude * 10f);
                tmove = move_dir.normalized * move_dist;
            }
            else if(controls_enabled)
            {
                Vector3 cam_move = TheCamera.Get().GetRotation() * controls.GetMove();
                if (mcontrols.IsJoystickActive())
                {
                    Vector2 joystick = mcontrols.GetJoystickDir();
                    cam_move = TheCamera.Get().GetRotation() * new Vector3(joystick.x, 0f, joystick.y);
                }
                tmove = cam_move * move_speed * move_speed_mult;
            }

            //Stop moving if doing action
            if (is_action)
                tmove = Vector3.zero;

            //Check ground
            DetectGrounded();

            //Falling
            if (!is_grounded || jump_timer > 0f)
            {
                if(jump_timer <= 0f)
                    jump_vect = Vector3.MoveTowards(jump_vect, Vector3.down * fall_speed, jump_gravity * Time.fixedDeltaTime);
                tmove += jump_vect;
            }

            //Do move
            move = Vector3.Lerp(move, tmove, move_accel * Time.fixedDeltaTime);
            rigid.velocity = move;

            //Facing
            if (!is_action && IsMoving())
            {
                facing = new Vector3(move.x, 0f, move.z).normalized;
            }

            Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);
            rigid.MoveRotation(Quaternion.RotateTowards(rigid.rotation, targ_rot, rotate_speed * Time.fixedDeltaTime));

            //Fronted (need to be done after facing)
            DetectFronted();

            //Traveled calcul
            Vector3 last_frame_travel = transform.position - prev_pos;
            move_average = Vector3.MoveTowards(move_average, last_frame_travel, 1f * Time.fixedDeltaTime);
            prev_pos = transform.position;

            //Stop auto move
            bool stuck_somewhere = move_average.magnitude < 0.02f && auto_move_timer > 1f;
            if (stuck_somewhere)
                StopMove();

            if (controls.IsMoving() || mcontrols.IsJoystickActive())
                StopAction();
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (is_dead)
                return;

            PlayerControls controls = PlayerControls.Get();

            jump_timer -= Time.deltaTime;

            //Update attributes
            ResolveAttributeEffects();

            //Stop sleep
            if (is_action || IsMoving() || sleep_target == null)
            {
                StopSleep();
            }

            //Sleeps
            if (is_sleep)
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
                PlayerData.Get().AddAttributeValue(AttributeType.Health, sleep_target.sleep_hp_hour * game_speed * Time.deltaTime, GetAttributeMax(AttributeType.Health));
                PlayerData.Get().AddAttributeValue(AttributeType.Hunger, sleep_target.sleep_hunger_hour * game_speed * Time.deltaTime, GetAttributeMax(AttributeType.Hunger));
                PlayerData.Get().AddAttributeValue(AttributeType.Thirst, sleep_target.sleep_thirst_hour * game_speed * Time.deltaTime, GetAttributeMax(AttributeType.Thirst));
                PlayerData.Get().AddAttributeValue(AttributeType.Happiness, sleep_target.sleep_hapiness_hour * game_speed * Time.deltaTime, GetAttributeMax(AttributeType.Happiness));
            }

            //Activate Selectable
            Vector3 move_dir = auto_move_target - transform.position;
            if (auto_move && !is_action && auto_move_select != null && move_dir.magnitude < GetTargetInteractRange(auto_move_select))
            {
                auto_move = false;
                auto_move_select.Use(this, auto_move_target);
                auto_move_select = null;
            }

            //Finish construction
            if (auto_move && !is_action && clicked_build && current_buildable != null && move_dir.magnitude < current_buildable.build_distance)
            {
                auto_move = false;
                CompleteBuilding(auto_move_target);
            }

            //Stop move & drop
            if (auto_move && !is_action && move_dir.magnitude < 0.35f)
            {
                auto_move = false;
                DropItem(auto_move_drop);
                DropEquippedItem(auto_move_drop_equip);
            }

            //Attack
            float speed = GetAttackSpeed();
            attack_timer += speed * Time.deltaTime;
            if (auto_move_attack != null && !is_action && IsAttackTargetInRange())
            {
                FaceTorward(auto_move_attack.transform.position);

                if (attack_timer > 100f)
                {
                    DoAttack(auto_move_attack);
                }
            }

            if (!CanAttack(auto_move_attack))
                auto_move_attack = null;

            //Press Action button
            if (controls_enabled && !is_action) {
                if (controls.IsPressAction())
                {
                    if (current_buildable != null)
                    {
                        CompleteBuilding(current_buildable.transform.position);
                    }
                    else
                    {
                        InteractWithNearest();
                    }
                }

                if (controls.IsPressAttack())
                {
                    AttackNearest();
                }

                if (can_jump && controls.IsPressJump())
                {
                    Jump();
                }
            }
        }

        //Detect if character is on the floor
        private void DetectGrounded()
        {
            Vector3 scale = transform.lossyScale;
            float hradius = collide.height * scale.y * 0.5f + ground_detect_dist; //radius is half the height minus offset
            float radius = collide.radius * (scale.x + scale.y) * 0.5f;

            Vector3 center = collide.transform.position + Vector3.Scale(collide.center, scale);
            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.left * radius;
            Vector3 p3 = center + Vector3.right * radius;
            Vector3 p4 = center + Vector3.forward * radius;
            Vector3 p5 = center + Vector3.back * radius;

            RaycastHit h1, h2, h3, h4, h5;
            bool f1 = Physics.Raycast(p1, Vector3.down, out h1, hradius, ground_layer.value);
            bool f2 = Physics.Raycast(p2, Vector3.down, out h2, hradius, ground_layer.value);
            bool f3 = Physics.Raycast(p3, Vector3.down, out h3, hradius, ground_layer.value);
            bool f4 = Physics.Raycast(p4, Vector3.down, out h4, hradius, ground_layer.value);
            bool f5 = Physics.Raycast(p5, Vector3.down, out h5, hradius, ground_layer.value);

            is_grounded = f1 || f2 || f3 || f4 || f5;

            //Debug.DrawRay(p1, Vector3.down * hradius);
            //Debug.DrawRay(p2, Vector3.down * hradius);
            //Debug.DrawRay(p3, Vector3.down * hradius);
            //Debug.DrawRay(p4, Vector3.down * hradius);
            //Debug.DrawRay(p5, Vector3.down * hradius);
        }

        //Detect if there is an obstacle in front of the character
        private void DetectFronted()
        {
            Vector3 scale = transform.lossyScale;
            float hradius = collide.height * scale.y * 0.5f - 0.02f; //radius is half the height minus offset
            float radius = collide.radius * (scale.x + scale.y) * 0.5f + 0.5f;

            Vector3 center = collide.transform.position + Vector3.Scale(collide.center, scale);
            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.up * hradius;
            Vector3 p3 = center + Vector3.down * hradius;

            RaycastHit h1, h2, h3;
            bool f1 = Physics.Raycast(p1, facing, out h1, radius);
            bool f2 = Physics.Raycast(p2, facing, out h2, radius);
            bool f3 = Physics.Raycast(p3, facing, out h3, radius);

            is_fronted = f1 || f2 || f3;

            //Debug.DrawRay(p1, facing * radius);
            //Debug.DrawRay(p2, facing * radius);
            //Debug.DrawRay(p3, facing * radius);
        }

        //Update attribute and apply effects for having empty attribute
        private void ResolveAttributeEffects()
        {
            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            //Update Attributes
            foreach (AttributeData attr in attributes)
            {
                float update_value = attr.value_per_hour + GetTotalBonus(BonusEffectData.GetAttributeBonusType(attr.type));
                update_value = update_value * game_speed * Time.deltaTime;
                PlayerData.Get().AddAttributeValue(attr.type, update_value, attr.max_value);
            }

            //Penalty for depleted attributes
            float health_max = GetAttributeMax(AttributeType.Health);
            float health = GetAttributeValue(AttributeType.Health);

            move_speed_mult = 1f + GetTotalBonus(BonusType.SpeedBoost);

            foreach (AttributeData attr in attributes)
            {
                if (GetAttributeValue(attr.type) < 0.01f)
                {
                    move_speed_mult = move_speed_mult * attr.deplete_move_mult;
                    float update_value = attr.deplete_hp_loss * game_speed * Time.deltaTime;
                    PlayerData.Get().AddAttributeValue(AttributeType.Health, update_value, health_max);
                }
            }

            //Dying
            health = GetAttributeValue(AttributeType.Health);
            if (health < 0.01f)
                Kill();
        }

        //Perform one attack
        private void DoAttack(Destructible resource)
        {
            attack_timer = -10f;
            StartCoroutine(AttackRun(resource));
        }

        private IEnumerator AttackRun(Destructible target)
        {
            is_action = true;

            bool is_ranged = target != null && CanWeaponAttackRanged(target);

            //Start animation
            if (onAttack != null)
                onAttack.Invoke(target, is_ranged);

            //Face target
            FaceTorward(target.transform.position);

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
            UpdateAllEquippedItemsDurability(true, -1f);

            attack_timer = 0f;

            //Wait for the end of the attack before character can move again
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            is_action = false;
        }

        private void DoAttackStrike(Destructible target, bool is_ranged)
        {
            //Ranged attack
            if (target != null && is_ranged)
            {
                ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
                ItemData projectile = GetFirstItemInGroup(equipped.projectile_group);
                if (projectile != null && CanWeaponAttackRanged(target))
                {
                    PlayerData.Get().RemoveItem(projectile.id, 1);
                    Vector3 pos = GetProjectileSpawnPos(equipped);
                    Vector3 dir = target.GetCenter() - pos;
                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    proj.GetComponent<Projectile>().dir = dir.normalized;
                    proj.GetComponent<Projectile>().damage = equipped.damage;
                }
            }

            //Melee attack
            else if (IsAttackTargetInRange())
            {
                target.DealDamage(GetAttackDamage(target));

                if (onAttackHit != null)
                    onAttackHit.Invoke(target);
            }
        }

        public void Sleep(ActionSleep sleep_target)
        {
            this.sleep_target = sleep_target;
            is_sleep = true;
            auto_move = false;
            auto_move_attack = null;
            TheGame.Get().SetGameSpeedMultiplier(sleep_target.sleep_speed_mult);
        }

        public void StopSleep()
        {
            is_sleep = false;
            sleep_target = null;
            TheGame.Get().SetGameSpeedMultiplier(1f);
        }

        public void DealDamage(int damage)
        {
            if (is_dead)
                return;

            if (GetTotalBonus(BonusType.Invulnerable) > 0.5f)
                return;

            int dam = damage - GetArmor();
            dam = Mathf.Max(dam, 1);

            int invuln = Mathf.RoundToInt(dam * GetTotalBonus(BonusType.Invulnerable));
            dam = dam - invuln;

            if (dam <= 0)
                return;

            PlayerData.Get().AddAttributeValue(AttributeType.Health, -dam, GetAttributeMax(AttributeType.Health));

            //Durability
            UpdateAllEquippedItemsDurability(false, -1f);

            StopSleep();

            TheCamera.Get().Shake();
            TheAudio.Get().PlaySFX("character", hit_sound);

            if (onDamaged != null)
                onDamaged.Invoke();
        }

        public void Kill()
        {
            if (is_dead)
                return;

            rigid.velocity = Vector3.zero;
            move = Vector3.zero;
            is_dead = true;

            if (onDeath != null)
                onDeath.Invoke();
        }

        public void FaceTorward(Vector3 pos)
        {
            Vector3 face = (pos - transform.position);
            face.y = 0f;
            if (face.magnitude > 0.01f)
            {
                facing = face.normalized;
            }
        }

        public void Jump()
        {
            if (!IsJumping() && is_grounded)
            {
                jump_vect = Vector3.up * jump_power;
                jump_timer = jump_duration;

                if (onJump != null)
                    onJump.Invoke();
            }
        }

        //Call animation directly
        public void TriggerAnim(string anim, float duration = 0f)
        {
            if (onTriggerAnim != null)
                onTriggerAnim.Invoke(anim, duration);
        }

        //Just animate the character for X seconds, and prevent it from doing other things, then callback
        public void TriggerAction(string anim, Vector3 face_at, float duration = 0.5f, UnityAction callback = null)
        {
            if (!is_action)
            {
                FaceTorward(face_at);
                TriggerAnim(anim, duration);
                StartCoroutine(RunAction(duration, callback));
            }
        }

        private IEnumerator RunAction(float action_duration, UnityAction callback)
        {
            is_action = true;
            yield return new WaitForSeconds(action_duration);
            is_action = false;
            if (callback != null)
                callback.Invoke();
        }

        //------- Items ----------

        //Take an Item on the floor
        public void TakeItem(Item item)
        {
            if (item != null && !is_action && item.CanTakeItem())
            {
                StartCoroutine(TakeItemRoutine(item));
            }
        }

        private IEnumerator TakeItemRoutine(Item item)
        {
            is_action = true;

            FaceTorward(item.transform.position);

            if (onTakeItem != null)
                onTakeItem.Invoke(item);

            yield return new WaitForSeconds(0.4f);

            //Make sure wasnt destroyed during the 0.4 sec
            if (item != null && item.CanTakeItem())
                item.TakeItem();

            is_action = false;
        }

        //Gain an new item directly to inventory
        public void GainItem(ItemData item, int quantity, Vector3 pos)
        {
            if (item != null)
            {
                if (PlayerData.Get().CanTakeItem(item.id, quantity))
                {
                    int islot = PlayerData.Get().AddItem(item.id, quantity, item.durability);
                    ItemTakeFX.DoTakeFX(pos, item, islot);
                }
                else
                {
                    Item.Create(item, transform.position, quantity, item.durability);
                }
            }
        }

        public void FishItem(ItemProvider source, int quantity)
        {
            if (source != null && source.HasItem() && PlayerData.Get().CanTakeItem(source.item.id, quantity))
            {
                StartCoroutine(FishRoutine(source, quantity));
            }
        }

        private IEnumerator FishRoutine(ItemProvider source, int quantity)
        {
            is_action = true;
            is_fishing = true;

            if (source != null)
                FaceTorward(source.transform.position);

            yield return new WaitForSeconds(0.4f);

            is_action = false;

            float timer = 0f;
            while (is_fishing && timer < 3f)
            {
                yield return new WaitForSeconds(0.02f);
                timer += 0.02f;

                if (IsMoving())
                    is_fishing = false;
            }

            if (is_fishing)
            {
                source.RemoveItem();
                GainItem(source.item, quantity, source.transform.position);
            }

            is_fishing = false;
        }

        //Drop item on the floor
        public void DropItem(int slot)
        {
            InventoryItemData invdata = PlayerData.Get().GetItemSlot(slot);
            ItemData idata = ItemData.Get(invdata?.item_id);
            if (invdata != null && idata != null && invdata.quantity > 0)
            {
                if (idata.CanBeDropped())
                {
                    PlayerData.Get().RemoveItemAt(slot, invdata.quantity);
                    Item iitem = Item.Create(idata, transform.position, invdata.quantity, invdata.durability);

                    TheUI.Get().CancelSelection();

                    if (onDropItem != null)
                        onDropItem.Invoke(iitem);
                }
                else if (idata.CanBeBuilt())
                {
                    BuildItem(slot, transform.position);
                }
            }
        }

        //Drop equipped item on the floor
        public void DropEquippedItem(int slot)
        {
            InventoryItemData invdata = PlayerData.Get().GetEquippedItemSlot(slot);
            ItemData idata = ItemData.Get(invdata?.item_id);
            if (invdata != null && idata != null)
            {
                PlayerData.Get().UnequipItem(slot);
                Item iitem = Item.Create(idata, transform.position, 1, invdata.durability);

                TheUI.Get().CancelSelection();

                if (onDropItem != null)
                    onDropItem.Invoke(iitem);
            }
        }

        public void EatItem(ItemData item, int slot)
        {
            if (item.type == ItemType.Consumable)
            {
                PlayerData pdata = PlayerData.Get();

                if (pdata.IsItemIn(item.id, slot))
                {
                    pdata.RemoveItemAt(slot, 1);
                    if (item.container_data)
                        pdata.AddItem(item.container_data.id, 1, item.container_data.durability);

                    pdata.AddAttributeValue(AttributeType.Health, item.eat_hp, GetAttributeMax(AttributeType.Health));
                    pdata.AddAttributeValue(AttributeType.Hunger, item.eat_hunger, GetAttributeMax(AttributeType.Hunger));
                    pdata.AddAttributeValue(AttributeType.Thirst, item.eat_thirst, GetAttributeMax(AttributeType.Thirst));
                    pdata.AddAttributeValue(AttributeType.Happiness, item.eat_happiness, GetAttributeMax(AttributeType.Happiness));

                    foreach (BonusEffectData bonus in item.eat_bonus)
                    {
                        pdata.AddTimedBonus(bonus.type, bonus.value, item.eat_bonus_duration);
                    }
                }
            }
        }

        public void RemoveItem(int slot)
        {
            InventoryItemData invtem = PlayerData.Get().GetItemSlot(slot);
            ItemData idata = ItemData.Get(invtem?.item_id);
            if (idata != null)
            {
                PlayerData.Get().RemoveItemAt(slot, 1);
                if (idata.container_data)
                    PlayerData.Get().AddItem(idata.container_data.id, 1, idata.container_data.durability);
            }
        }

        public void EquipItem(ItemData item, int eslot)
        {
            if (item.type == ItemType.Equipment)
            {
                int index = ItemData.GetEquipIndex(item.equip_slot);
                PlayerData.Get().EquipItemTo(eslot, index);
            }
        }

        public void UnEquipItem(ItemData item, int eslot)
        {
            if (item.type == ItemType.Equipment)
            {
                if (PlayerData.Get().CanTakeItem(item.id, 1))
                {
                    InventoryItemData invdata = PlayerData.Get().GetEquippedItemSlot(eslot);
                    if (invdata != null)
                    {
                        PlayerData.Get().UnequipItem(eslot);
                        PlayerData.Get().AddItem(item.id, 1, invdata.durability);
                    }
                }
            }
        }

        public void RemoveEquipItem(int eslot)
        {
            InventoryItemData invtem = PlayerData.Get().GetEquippedItemSlot(eslot);
            ItemData idata = ItemData.Get(invtem?.item_id);
            if (idata != null)
            {
                PlayerData.Get().UnequipItem(eslot);
                if (idata.container_data)
                    PlayerData.Get().EquipItem(eslot, idata.container_data.id, idata.container_data.durability);
            }
        }

        public bool HasItem(ItemData item)
        {
            PlayerData pdata = PlayerData.Get();
            return pdata.HasItem(item.id);
        }

        public bool HasItemInGroup(GroupData group)
        {
            PlayerData pdata = PlayerData.Get();
            foreach (KeyValuePair<int, InventoryItemData> pair in pdata.inventory)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null && pair.Value.quantity > 0)
                    {
                        if (idata.HasGroup(group))
                            return true;
                    }
                }
            }
            foreach (KeyValuePair<int, InventoryItemData> pair in pdata.equipped_items)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null)
                    {
                        if (idata.HasGroup(group))
                            return true;
                    }
                }
            }
            return false;
        }

        public void UpdateAllEquippedItemsDurability(bool weapon, float value)
        {
            //Durability
            foreach (KeyValuePair<int, InventoryItemData> pair in PlayerData.Get().equipped_items)
            {
                InventoryItemData invdata = pair.Value;
                ItemData idata = ItemData.Get(invdata?.item_id);
                if (idata != null && invdata != null && idata.weapon == weapon && idata.durability_type == DurabilityType.UsageCount)
                    invdata.durability += value;
            }
        }

        public ItemData GetFirstItemInGroup(GroupData group)
        {
            PlayerData pdata = PlayerData.Get();
            foreach (KeyValuePair<int, InventoryItemData> pair in pdata.inventory)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null && pair.Value.quantity > 0)
                    {
                        if (idata.HasGroup(group))
                            return idata;
                    }
                }
            }
            return null;
        }

        public InventoryItemData GetEquippedItem(EquipSlot slot)
        {
            return PlayerData.Get().GetEquippedItemSlot(ItemData.GetEquipIndex(slot));
        }

        public ItemData GetEquippedItemData(EquipSlot slot)
        {
            InventoryItemData invdata = GetEquippedItem(slot);
            return ItemData.Get(invdata?.item_id);
        }

        public InventoryItemData GetEquippedItemInGroup(GroupData group)
        {
            PlayerData pdata = PlayerData.Get();
            foreach (KeyValuePair<int, InventoryItemData> pair in pdata.equipped_items)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null)
                    {
                        if (idata.HasGroup(group))
                            return pair.Value;
                    }
                }
            }
            return null;
        }

        public bool HasEquippedItemInGroup(GroupData group)
        {
            return GetEquippedItemInGroup(group) != null;
        }

        //---- Crafting ----

        public bool CanCraft(CraftData item, bool skip_near=false)
        {
            CraftCostData cost = item.GetCraftCost();
            bool can_craft = true;
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                if (!PlayerData.Get().HasItem(pair.Key.id, pair.Value))
                    can_craft = false; //Dont have required items
            }

            if (!skip_near && cost.craft_near != null && !IsNearGroup(cost.craft_near) && !HasEquippedItemInGroup(cost.craft_near))
                can_craft = false; //Not near required construction

            return can_craft;
        }

        public void PayCraftingCost(CraftData item)
        {
            CraftCostData cost = item.GetCraftCost();
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                PlayerData.Get().RemoveItem(pair.Key.id, pair.Value);
                if (pair.Key.container_data)
                    PlayerData.Get().AddItem(pair.Key.container_data.id, pair.Value, pair.Key.container_data.durability);
            }
        }

        public void CraftCraftable(CraftData data)
        {
            ItemData item = data.GetItem();
            ConstructionData construct = data.GetConstruction();
            PlantData plant = data.GetPlant();
            CharacterData character = data.GetCharacter();

            if (item != null)
                CraftItem(item);
            else if (construct != null)
                CraftConstructionBuildMode(construct);
            else if (plant != null)
                CraftPlantBuildMode(plant, 0);
            else if (character != null)
                CraftCharacter(character);

            TheAudio.Get().PlaySFX("craft", data.craft_sound);
        }

        public void CraftItem(ItemData item, bool pay_craft_cost = true)
        {
            if (!pay_craft_cost || CanCraft(item))
            {
                if(pay_craft_cost)
                    PayCraftingCost(item);

                if (PlayerData.Get().CanTakeItem(item.id, item.craft_quantity))
                    PlayerData.Get().AddItem(item.id, item.craft_quantity, item.durability);
                else
                    Item.Create(item, transform.position, item.craft_quantity, item.durability);
            }
        }

        public Character CraftCharacter(CharacterData character, bool pay_craft_cost = true)
        {
            if (!pay_craft_cost || CanCraft(character))
            {
                if (pay_craft_cost)
                    PayCraftingCost(character);

                Vector3 pos = transform.position + transform.forward * 0.8f;
                Character acharacter = Character.Create(character, pos);

                return acharacter;
            }
            return null;
        }

        public Plant CraftPlant(PlantData plant, bool pay_craft_cost = true)
        {
            if (!pay_craft_cost || CanCraft(plant))
            {
                if (pay_craft_cost)
                    PayCraftingCost(plant);

                Vector3 pos = transform.position + transform.forward * 0.4f;
                Plant aplant = Plant.Create(plant, pos, 0);

                return aplant;
            }
            return null;
        }

        public void CraftPlantBuildMode(PlantData plant, int stage, bool pay_craft_cost = true, UnityAction<Buildable> callback=null)
        {
            if (!pay_craft_cost || CanCraft(plant))
            {
                CancelBuilding();

                Plant aplant = Plant.CreateBuildMode(plant, transform.position, stage);
                current_buildable = aplant.GetBuildable();
                current_buildable.StartBuild();
                clicked_build = false;
                build_pay_cost = pay_craft_cost;
                build_callback = callback;
            }
        }

        public Construction CraftConstruction(ConstructionData construct, bool pay_craft_cost = true)
        {
            if (!pay_craft_cost || CanCraft(construct))
            {
                if (pay_craft_cost)
                    PayCraftingCost(construct);

                Vector3 pos = transform.position + transform.forward * 1f;
                Construction aconstruct = Construction.Create(construct, pos);

                return aconstruct;
            }
            return null;
        }

        public void CraftConstructionBuildMode(ConstructionData item, bool pay_craft_cost=true, UnityAction<Buildable> callback = null)
        {
            if (!pay_craft_cost || CanCraft(item))
            {
                CancelBuilding();

                Construction construction = Construction.CreateBuildMode(item, transform.position); ;
                current_buildable = construction.GetBuildable();
                current_buildable.StartBuild();
                clicked_build = false;
                build_pay_cost = pay_craft_cost;
                build_callback = callback;
            }
        }

        public void CompleteBuilding(Vector3 pos)
        {
            if (current_buildable != null)
            {
                CraftData item = null;
                if (current_buildable.GetComponent<Construction>())
                    item = current_buildable.GetComponent<Construction>().data;
                if (current_buildable.GetComponent<Plant>())
                    item = current_buildable.GetComponent<Plant>().data;

                if (item != null && (!build_pay_cost || CanCraft(item, true)))
                {
                    current_buildable.SetBuildPosition(pos);
                    if (current_buildable.CheckIfCanBuild())
                    {
                        FaceTorward(pos);

                        if(build_pay_cost)
                            PayCraftingCost(item);

                        Buildable buildable = current_buildable;
                        buildable.FinishBuild();

                        UnityAction<Buildable> bcallback = build_callback;
                        current_buildable = null;
                        build_callback = null;
                        clicked_build = false;
                        auto_move = false;

                        TheUI.Get().CancelSelection();
                        TheAudio.Get().PlaySFX("craft", buildable.build_audio);

                        if (onBuild != null)
                            onBuild.Invoke(buildable);

                        if (bcallback != null)
                            bcallback.Invoke(buildable);

                        StartCoroutine(BuildRoutine(buildable));
                    }
                }
            }
        }

        private IEnumerator BuildRoutine(Buildable buildable)
        {
            is_action = true;
            yield return new WaitForSeconds(1f);
            is_action = false;
        }

        public void CancelBuilding()
        {
            if (current_buildable != null)
            {
                Destroy(current_buildable.gameObject);
                current_buildable = null;
                build_callback = null;
                clicked_build = false;
            }
        }

        //Use an item in your inventory and build it on the map
        public void BuildItem(int slot, Vector3 pos)
        {
            InventoryItemData invdata = PlayerData.Get().GetItemSlot(slot);
            ItemData idata = ItemData.Get(invdata?.item_id);
            if (invdata != null && idata != null)
            {
                ConstructionData construct = idata.construction_data;
                if (construct != null)
                {
                    PlayerData.Get().RemoveItemAt(slot, 1);
                    Construction construction = CraftConstruction(construct, false);
                    BuiltConstructionData constru = PlayerData.Get().GetConstructed(construction.GetUID());
                    if (idata.HasDurability())
                        constru.durability = invdata.durability; //Save durability
                    TheUI.Get().CancelSelection();
                    TheAudio.Get().PlaySFX("craft", construction.GetBuildable().build_audio);
                }
            }
        }

        //----- Player Orders ----------

        public void MoveTo(Vector3 pos)
        {
            auto_move = true;
            auto_move_target = pos;
            auto_move_target_next = pos;
            auto_move_select = null;
            auto_move_attack = null;
            auto_move_drop = -1;
            auto_move_drop_equip = -1;
            auto_move_timer = 0f;
            path_found = false;
            calculating_path = false;

            CalculateNavmesh();
        }

        public void UpdateMoveTo(Vector3 pos)
        {
            //Meant to be called every frame, for this reason don't do navmesh
            auto_move = true;
            auto_move_target = pos;
            auto_move_target_next = pos;
            path_found = false;
            calculating_path = false;
            auto_move_select = null;
            auto_move_attack = null;
            auto_move_drop = -1;
            auto_move_drop_equip = -1;
        }

        public void InteractWith(Selectable selectable)
        {
            auto_move = true;
            auto_move_select = selectable;
            auto_move_target = selectable.transform.position;
            auto_move_target_next = selectable.transform.position;
            auto_move_drop = -1;
            auto_move_drop_equip = -1;
            auto_move_timer = 0f;
            clicked_build = false;
            path_found = false;
            calculating_path = false;
            auto_move_attack = null;

            CalculateNavmesh();
        }

        public void InteractWith(Selectable selectable, Vector3 pos)
        {
            //Interact with selectable, but at specific position
            InteractWith(selectable);
            if (selectable.surface)
            {
                auto_move_target = pos;
                auto_move_target_next = pos;
            }
        }

        public void Attack(Destructible target)
        {
            if (CanAttack(target))
            {
                auto_move = true;
                auto_move_select = null;
                auto_move_attack = target;
                auto_move_target = target.transform.position;
                auto_move_target_next = target.transform.position;
                auto_move_drop = -1;
                auto_move_drop_equip = -1;
                auto_move_timer = 0f;
                clicked_build = false;
                path_found = false;
                calculating_path = false;

                CalculateNavmesh();
            }
        }

        public void StopMove()
        {
            auto_move = false;
        }

        public void StopAction()
        {
            auto_move = false;
            auto_move_select = null;
            auto_move_attack = null;
        }

        public void InteractWithNearest()
        {
            Selectable nearest = Selectable.GetNearestInteractable(transform.position, 4f);
            if (nearest != null)
            {
                InteractWith(nearest);
            }
        }

        public void AttackNearest()
        {
            Destructible destruct = Destructible.GetNearestAutoAttack(transform.position, 4f);
            if (destruct != null)
            {
                Attack(destruct);
            }
        }

        public void TryBuildAt(Vector3 pos)
        {
            if (!clicked_build && current_buildable != null)
            {
                bool can_build = current_buildable.CheckIfCanBuild();
                if (TheGame.IsMobile() || can_build)
                {
                    current_buildable.SetBuildPosition(pos);
                    if (can_build)
                    {
                        clicked_build = true; //Give command to build
                        MoveTo(pos);
                    }
                }
            }
        }

        public void EnableControls()
        {
            controls_enabled = true;
        }

        public void DisableControls()
        {
            controls_enabled = false;
            StopAction();
        }

        //------- Mouse Clicks --------

        private void OnClick(Vector3 pos)
        {
            if (!controls_enabled)
                return;


        }

        private void OnRightClick(Vector3 pos)
        {
            if (!controls_enabled)
                return;

            TheUI.Get().CancelSelection();
        }

        private void OnMouseHold(Vector3 pos)
        {
            if (!controls_enabled)
                return;

            if (TheGame.IsMobile())
                return; //On mobile, use joystick instead, no mouse hold

            //Only hold for normal movement, if interacting dont change while holding
            if (current_buildable == null && auto_move_select == null && auto_move_attack == null)
            {
                UpdateMoveTo(pos);
            }
        }

        private void OnClickFloor(Vector3 pos)
        {
            if (!controls_enabled)
                return;

            MoveTo(pos);

            auto_move_drop = PlayerControlsMouse.Get().GetInventorySelectedSlotIndex();
            auto_move_drop_equip = PlayerControlsMouse.Get().GetEquippedSelectedSlotIndex();

            if (clicked_build)
                CancelBuilding();
            
            TryBuildAt(pos);
        }

        private void OnClickObject(Selectable selectable, Vector3 pos)
        {
            if (!controls_enabled)
                return;

            if (IsBuildMode())
            {
                OnClickFloor(pos);
                return;
            }

            selectable.Select();

            //Attack target ?
            Destructible target = selectable.GetDestructible();
            if (target != null && CanAttack(target))
            {
                Attack(target);
            }
            else
            {
                InteractWith(selectable, pos);
            }
        }

        //---- Navmesh ----

        public void CalculateNavmesh()
        {
            if (auto_move && use_navmesh && !calculating_path)
            {
                calculating_path = true;
                path_found = false;
                path_index = 0;
                auto_move_target_next = auto_move_target; //Default
                NavMeshTool.CalculatePath(transform.position, auto_move_target, 1 << 0, FinishCalculateNavmesh);
            }
        }

        private void FinishCalculateNavmesh(NavMeshToolPath path)
        {
            calculating_path = false;
            path_found = path.success;
            nav_paths = path.path;
            path_index = 0;
        }

        //---- Getters ----

        //Check if character is near an object of that group
        public bool IsNearGroup(GroupData group)
        {
            foreach (Selectable select in Selectable.GetAllActive())
            {
                if (select.HasGroup(group))
                {
                    float dist = (select.transform.position - transform.position).magnitude;
                    if (dist < select.use_range)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public bool CanAttack(Destructible target)
        {
            return target != null && !target.IsDead() && target.CanBeAttacked() 
                && (target.required_item != null || target.attack_group != AttackGroup.Ally) //Cant attack allied unless has required item
                && (target.required_item == null || HasEquippedItemInGroup(target.required_item)); //Cannot attack unless has equipped item
        }

        public int GetAttackDamage(Destructible target)
        {
            int damage = hand_damage;

            ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                damage = equipped.damage;

            damage += Mathf.RoundToInt(damage * GetTotalBonus(BonusType.AttackBoost));

            return damage;
        }

        public float GetAttackRange(Destructible target)
        {
            ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                return equipped.range;
            return attack_range;
        }

        public int GetAttackStrikes(Destructible target)
        {
            ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        public float GetAttackStikesInterval(Destructible target)
        {
            ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        public float GetAttackWindup()
        {
            if (character_equip)
            {
                EquipItem item_equip = character_equip.GetEquippedItem(EquipSlot.Hand);
                if (item_equip != null)
                    return item_equip.attack_windup;
            }
            return attack_windup;
        }

        public float GetAttackWindout()
        {
            if (character_equip)
            {
                EquipItem item_equip = character_equip.GetEquippedItem(EquipSlot.Hand);
                if (item_equip != null)
                    return item_equip.attack_windout;
            }
            return attack_windout;
        }

        public Vector3 GetProjectileSpawnPos(ItemData weapon)
        {

            EquipAttach attach = GetEquipAttach(EquipSlot.Hand, weapon.equip_side);
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
            ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
            bool valid_ranged = equipped != null && equipped.ranged && CanWeaponAttackRanged(target);
            bool valid_melee = equipped != null && !equipped.ranged;
            return valid_melee || valid_ranged;
        }

        //Check if target is valid for ranged attack, and if enough bullets
        public bool CanWeaponAttackRanged(Destructible destruct)
        {
            if (destruct == null)
                return false;

            ItemData equipped = GetEquippedItemData(EquipSlot.Hand);
            if (equipped != null && equipped.ranged && destruct.CanAttackRanged())
            {
                ItemData projectile = GetFirstItemInGroup(equipped.projectile_group);
                if (projectile != null && HasItem(projectile))
                {
                    return true;
                }
            }
            return false;
        }

        public float GetTargetInteractRange(Selectable target)
        {
            return target.use_range;
        }

        public float GetTargetAttackRange(Destructible target)
        {
            return GetAttackRange(target) + target.hit_range;
        }

        public bool IsAttackTargetInRange()
        {
            if (auto_move_attack != null)
            {
                float dist = (auto_move_attack.transform.position - transform.position).magnitude;
                return dist < GetTargetAttackRange(auto_move_attack);
            }
            return false;
        }

        public int GetArmor()
        {
            int armor = base_armor;
            foreach (KeyValuePair<int, InventoryItemData> pair in PlayerData.Get().equipped_items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                    armor += idata.armor;
            }

            armor += Mathf.RoundToInt(armor * GetTotalBonus(BonusType.ArmorBoost));

            return armor;
        }

        public float GetTotalBonus(BonusType type)
        {
            float value = 0f;

            //Equip bonus
            foreach (KeyValuePair<int, InventoryItemData> pair in PlayerData.Get().equipped_items)
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
            value += PlayerData.Get().GetTotalTimedBonus(type);

            return value;
        }

        public float GetAttributeValue(AttributeType type)
        {
            return PlayerData.Get().GetAttributeValue(type);
        }

        public float GetAttributeMax(AttributeType type)
        {
            AttributeData adata = GetAttribute(type);
            if (adata != null)
                return adata.max_value;
            return 0f;
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

        public EquipAttach GetEquipAttach(EquipSlot slot, EquipSide side)
        {
            foreach (EquipAttach attach in equip_attachments)
            {
                if (attach.slot == slot)
                {
                    if (attach.side == EquipSide.Default || side == EquipSide.Default || attach.side == side)
                        return attach;
                }
            }
            return null;
        }

        public Buildable GetCurrentBuildable()
        {
            return current_buildable; //Can be null if not in build mode
        }

        public bool IsBuildMode()
        {
            return current_buildable != null && current_buildable.IsBuilding();
        }

        public bool IsDead()
        {
            return is_dead;
        }

        public bool IsSleeping()
        {
            return is_sleep;
        }

        public bool IsFishing()
        {
            return is_fishing;
        }

        public bool IsDoingAction()
        {
            return is_action;
        }

        public bool IsJumping()
        {
            return jump_timer > 0f;
        }

        public bool IsMoving()
        {
            Vector3 moveXZ = new Vector3(move.x, 0f, move.z);
            return moveXZ.magnitude > move_speed * move_speed_mult * 0.25f;
        }

        public bool IsControlsEnabled()
        {
            return controls_enabled;
        }

        public Vector3 GetMove()
        {
            return move;
        }

        public Vector3 GetFacing()
        {
            return facing;
        }

        public bool IsFronted()
        {
            return is_fronted;
        }

        public static PlayerCharacter Get()
        {
            return _instance;
        }
    }

}