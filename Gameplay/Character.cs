using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary> 
    /// Characters are allies or npc that can be given orders to move or perform actions
    /// </summary> 

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(UniqueID))]
    public class Character : MonoBehaviour
    {
        [Header("Character")]
        public CharacterData data;

        [Header("Move")]
        public bool move_enabled = true;
        public float move_speed = 2f;
        public float rotate_speed = 250f;
        public bool avoid_obstacles = true;
        public bool use_navmesh = false;

        [Header("Ground/Falling")]
        public float fall_speed = 20f;
        public float ground_detect_dist = 0.1f;
        public LayerMask ground_layer = ~0;

        [Header("Attack")]
        public bool attack_enabled = true;
        public int attack_damage = 10;
        public float attack_range = 1f;
        public float attack_cooldown = 3f;
        public float attack_windup = 0.5f;
        public float attack_duration = 1f;
        public AudioClip attack_audio;

        [Header("Action")]
        public float follow_distance = 3f;

        public UnityAction onAttack;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        [HideInInspector]
        public bool was_spawned = false; //If true, means it was created by the player

        private Rigidbody rigid;
        private Selectable selectable;
        private Destructible destruct;
        private UniqueID unique_id;
        private Collider[] colliders;
        private Vector3 bounds_extent;
        private Vector3 bounds_center_offset;

        private Vector3 start_pos;
        private Vector3 moving;
        private Vector3 facing;

        private GameObject target = null;
        private Destructible attack_target = null;
        private PlayerCharacter attack_player = null;
        private Vector3 move_target;
        private Vector3 current_move_target;
        private Vector3 current_move_target_next;

        private float attack_timer = 0f;
        private bool is_moving = false;
        private bool is_escaping = false;
        private bool is_attacking = false;
        private bool attack_hit = false;

        private bool is_grounded = false;
        private bool is_fronted = false;
        private bool is_fronted_center = false;
        private bool is_fronted_left = false;
        private bool is_fronted_right = false;
        private float front_dist = 0f;
        private float grounded_dist = 0f;
        private float grounded_dist_average = 0f;
        private float avoid_angle = 0f;
        private float avoid_side = 1f;

        private Vector3[] nav_paths = new Vector3[0];
        private Vector3 path_destination;
        private int path_index = 0;
        private bool follow_path = false;
        private bool calculating_path = false;

        private static List<Character> character_list = new List<Character>();

        void Awake()
        {
            character_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            selectable = GetComponent<Selectable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            colliders = GetComponentsInChildren<Collider>();
            start_pos = transform.position;
            avoid_side = Random.value < 0.5f ? 1f : -1f;
            facing = transform.forward;
            use_navmesh = move_enabled && use_navmesh;

            move_target = transform.position;
            current_move_target = transform.position;
            current_move_target_next = transform.position;

            selectable.onUse += OnUse;
            selectable.onDestroy += OnDeath;

            foreach (Collider collide in colliders)
            {
                float size = collide.bounds.extents.magnitude;
                if (size > bounds_extent.magnitude)
                {
                    bounds_extent = collide.bounds.extents;
                    bounds_center_offset = collide.bounds.center - transform.position;
                }
            }
        }

        void OnDestroy()
        {
            character_list.Remove(this);
        }

        void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID())) {
                Destroy(gameObject);
                return;
            }

            //Remove scene object and replace by spawned object so we can keep track of position and rotation in save file
            TrainedCharacterData cdata = PlayerData.Get().GetCharacter(GetUID());
            if (cdata == null && data != null && HasUID())
            {
                PlayerData.Get().AddCharacterUID(GetUID(), data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation);
                was_spawned = true;
            }
            else if(cdata != null && cdata.scene == SceneNav.GetCurrentScene())
            {
                transform.position = cdata.pos;
                transform.rotation = cdata.rot;
                was_spawned = true;
            }
        }

        private void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Detect obstacles and ground
            is_grounded = DetectGrounded();
            is_fronted = DetectFronted();

            if (!move_enabled)
                return;

            Vector3 tmove = Vector3.zero;

            if (!IsDead())
            {
                //Navmesh
                current_move_target_next = current_move_target;
                if (use_navmesh && follow_path && is_moving && path_index < nav_paths.Length)
                {
                    current_move_target_next = nav_paths[path_index];
                    Vector3 dir_total = current_move_target_next - transform.position;
                    dir_total.y = 0f;
                    if (dir_total.magnitude < 0.2f)
                        path_index++;
                }

                //Navmesh
                if (use_navmesh && is_moving)
                {
                    Vector3 path_dir = path_destination - transform.position;
                    Vector3 nav_move_dir = current_move_target - transform.position;
                    float dot = Vector3.Dot(path_dir.normalized, nav_move_dir.normalized);
                    if (dot < 0.7f)
                        CalculateNavmesh();
                }

                //Rotation
                Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);
                Quaternion nrot = Quaternion.RotateTowards(rigid.rotation, targ_rot, rotate_speed * Time.fixedDeltaTime);
                rigid.MoveRotation(nrot);

                //Moving
                if (is_moving)
                {
                    Vector3 move_dir_total = current_move_target - transform.position;
                    Vector3 move_dir_next = current_move_target_next - transform.position;
                    Vector3 move_dir = move_dir_next.normalized * Mathf.Min(move_dir_total.magnitude, 1f);
                    //move_dir.y = 0f;

                    tmove = move_dir.normalized * Mathf.Min(move_dir.magnitude, 1f) * move_speed;

                    if (move_dir.magnitude > 0.1f)
                    {
                        facing = new Vector3(tmove.x, 0f, tmove.z);
                        facing.Normalize();
                    }
                }
            }

            //Falling
            if (!is_grounded && fall_speed > 0.01f)
                tmove += Vector3.down * fall_speed;
            if (is_grounded && tmove.y < 0f)
                tmove.y = 0f;

            //Ground distance average
            if (is_grounded)
                grounded_dist_average = Mathf.MoveTowards(grounded_dist_average, grounded_dist, 5f * Time.fixedDeltaTime);

            //Adjust ground
            if (is_grounded && grounded_dist_average > 0.01f)
                transform.position = Vector3.MoveTowards(transform.position, transform.position + Vector3.up * grounded_dist, 1f * Time.fixedDeltaTime);

            moving = Vector3.Lerp(moving, tmove, 10f * Time.fixedDeltaTime);
            rigid.velocity = moving;

            //Add an offset to escape path when fronted
            if (avoid_obstacles)
            {
                if (is_fronted_left && !is_fronted_right)
                    avoid_side = 1f;
                if (is_fronted_right && !is_fronted_left)
                    avoid_side = -1f;

                //When fronted on all sides, use target to influence which side to go
                if (is_fronted_center && is_fronted_left && is_fronted_right && target)
                {
                    Vector3 dir = target.transform.position - transform.position;
                    dir = dir * (is_escaping ? -1f : 1f);
                    float dot = Vector3.Dot(dir.normalized, transform.right);
                    if(Mathf.Abs(dot) > 0.5f)
                        avoid_side = Mathf.Sign(dot);
                }

                float angle = avoid_side * 90f;
                float far_val = is_fronted ? 1f - (front_dist / destruct.hit_range) : Mathf.Abs(angle) / 90f; //1f = close, 0f = far
                float angle_speed = far_val * 150f + 50f;
                avoid_angle = Mathf.MoveTowards(avoid_angle, is_fronted ? angle : 0f, angle_speed * Time.fixedDeltaTime);
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            attack_timer += Time.deltaTime;

            //Save position
            TrainedCharacterData cdata = PlayerData.Get().GetCharacter(unique_id.unique_id);
            if (cdata != null)
            {
                cdata.pos = transform.position;
                cdata.rot = transform.rotation;
            }

            //Stop moving
            if (is_moving && !HasTarget() && HasReachedMoveTarget())
                Stop();

            //Stop attacking
            if (is_attacking && !HasAttackTarget())
                Stop();

            //Following
            if (target != null)
            {
                Vector3 targ_dir = (target.transform.position - transform.position);
                targ_dir.y = 0f;

                if (is_moving && !is_attacking)
                {
                    if (is_escaping)
                    {
                        Vector3 targ_pos = transform.position - targ_dir.normalized * 4f;
                        move_target = targ_pos;
                    }
                    else
                    {
                        move_target = target.transform.position;

                        //Stop following
                        if (attack_target != null && targ_dir.magnitude < GetAttackTargetHitRange() * 0.8f)
                        {
                            move_target = transform.position;
                            is_moving = false;
                        }

                        //Stop following
                        if (attack_target == null && HasReachedMoveTarget(follow_distance))
                        {
                            move_target = transform.position;
                            is_moving = false;
                        }
                    }
                }

                //Start following again
                if (!is_moving && !is_attacking)
                {
                    if (targ_dir.magnitude > GetAttackTargetHitRange())
                    {
                        is_moving = true;
                    }
                }
            }

            current_move_target = move_target;

            //Avoiding
            if(is_moving && avoid_obstacles && !HasReachedMoveTarget(1f))
                current_move_target = FindAvoidMoveTarget(move_target);

            //Attacking
            if (HasAttackTarget() && attack_enabled) {

                if (!is_attacking)
                {
                    if (attack_timer > attack_cooldown)
                    {
                        Vector3 targ_dir = (target.transform.position - transform.position);
                        targ_dir.y = 0f;

                        if (targ_dir.magnitude < GetAttackTargetHitRange())
                        {
                            is_attacking = true;
                            is_moving = false;
                            attack_hit = false;
                            attack_timer = 0f;

                            if (onAttack != null)
                                onAttack.Invoke();
                        }
                    }
                }

                if (is_attacking)
                {
                    move_target = transform.position;
                    current_move_target = transform.position;
                    FaceTorward(target.transform.position);

                    if (!attack_hit && attack_timer > attack_windup)
                    {
                        float range = (target.transform.position - transform.position).magnitude;
                        if (range < GetAttackTargetHitRange())
                        {
                            if(attack_target != null)
                                attack_target.DealDamage(attack_damage);
                            if (attack_player != null)
                                attack_player.DealDamage(attack_damage);
                        }
                        attack_hit = true;

                        if(selectable.IsNearCamera(20f))
                            TheAudio.Get().PlaySFX("character", attack_audio);
                    }

                    if (attack_timer > attack_duration)
                    {
                        is_attacking = false;
                        attack_timer = 0f;
                        is_moving = true;

                        if (attack_target != null)
                            Attack(attack_target);
                        if (attack_player != null)
                            Attack(attack_player);
                    }
                }

                if (attack_target != null && attack_target.IsDead())
                    Stop();

                if (attack_player != null && attack_player.IsDead())
                    Stop();
            }
        }

        public void MoveTo(Vector3 pos)
        {
            move_target = pos;
            current_move_target = pos;
            target = null;
            attack_target = null;
            attack_player = null;
            is_escaping = false;
            is_moving = true;
            CalculateNavmesh();
        }

        public void Follow(GameObject target)
        {
            if (target != null)
            {
                this.target = target.gameObject;
                this.attack_target = null;
                this.attack_player = null;
                move_target = target.transform.position;
                is_escaping = false;
                is_moving = true;
                CalculateNavmesh();
            }
        }

        public void Escape(GameObject target)
        {
            this.target = target;
            this.attack_target = null;
            this.attack_player = null;
            Vector3 dir = target.transform.position - transform.position;
            move_target = transform.position - dir;
            is_escaping = true;
            is_moving = true;
        }

        public void Attack(Destructible target)
        {
            if (attack_enabled && target != null && target.CanBeAttacked())
            {
                this.target = target.gameObject;
                this.attack_target = target;
                this.attack_player = null;
                move_target = target.transform.position;
                is_escaping = false;
                is_moving = true;
                CalculateNavmesh();
            }
        }

        public void Attack(PlayerCharacter target)
        {
            if (attack_enabled && target != null && !target.IsDead())
            {
                this.target = target.gameObject;
                this.attack_target = null;
                this.attack_player = target;
                move_target = target.transform.position;
                is_escaping = false;
                is_moving = true;
                CalculateNavmesh();
            }
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

        public void Stop()
        {
            target = null;
            attack_target = null;
            attack_player = null;
            rigid.velocity = Vector3.zero;
            move_target = transform.position;
            is_moving = false;
            is_attacking = false;
        }

        public void Kill()
        {
            if (destruct != null)
                destruct.Kill();
            else
                selectable.Destroy();
        }

        private void CalculateNavmesh()
        {
            if (use_navmesh && !calculating_path)
            {
                calculating_path = true;
                path_index = 0;
                NavMeshTool.CalculatePath(transform.position, move_target, 1 << 0, FinishCalculateNavmesh);
                path_destination = move_target;
            }
        }

        private void FinishCalculateNavmesh(NavMeshToolPath path)
        {
            calculating_path = false;
            follow_path = path.success;
            nav_paths = path.path;
            path_index = 0;
        }

        //Check if touching the ground
        private bool DetectGrounded()
        {
            float radius = (bounds_extent.x + bounds_extent.z) * 0.5f;
            float center_offset = bounds_extent.y;
            float hradius = center_offset + ground_detect_dist;

            Vector3 center = transform.position + bounds_center_offset;
            center.y = transform.position.y + center_offset;

            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.left * radius;
            Vector3 p3 = center + Vector3.right * radius;
            Vector3 p4 = center + Vector3.forward * radius;
            Vector3 p5 = center + Vector3.back * radius;

            RaycastHit h1, h2, h3, h4, h5;
            bool f1 = Physics.Raycast(p1, Vector3.down, out h1, hradius, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f2 = Physics.Raycast(p2, Vector3.down, out h2, hradius, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f3 = Physics.Raycast(p3, Vector3.down, out h3, hradius, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f4 = Physics.Raycast(p4, Vector3.down, out h4, hradius, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f5 = Physics.Raycast(p5, Vector3.down, out h5, hradius, ground_layer.value, QueryTriggerInteraction.Ignore);

            bool grounded = f1 || f2 || f3 || f4 || f5;

            //Find ground distance
            if (grounded)
            {
                Vector3 hit_center = transform.position;
                hit_center += f1 ? h1.point : transform.position;
                hit_center += f2 ? h2.point : transform.position;
                hit_center += f3 ? h3.point : transform.position;
                hit_center += f4 ? h4.point : transform.position;
                hit_center += f5 ? h5.point : transform.position;
                hit_center = hit_center / 6f;
                grounded_dist = (hit_center - transform.position).y;
            }

            //Debug.DrawRay(p1, Vector3.down * hradius);
            //Debug.DrawRay(p2, Vector3.down * hradius);
            //Debug.DrawRay(p3, Vector3.down * hradius);
            //Debug.DrawRay(p4, Vector3.down * hradius);
            //Debug.DrawRay(p5, Vector3.down * hradius);

            return grounded;
        }

        //Detect if there is an obstacle in front of the character
        private bool DetectFronted()
        {
            float radius = destruct.hit_range * 2f;

            Vector3 center = destruct.GetCenter();
            Vector3 dir = current_move_target_next - transform.position;
            Vector3 dirl = Quaternion.AngleAxis(-45f, Vector3.up) * dir.normalized;
            Vector3 dirr = Quaternion.AngleAxis(45f, Vector3.up) * dir.normalized;

            RaycastHit h, hl, hr;
            bool fc = Physics.Raycast(center, dir.normalized, out h, radius, ~0, QueryTriggerInteraction.Ignore);
            bool fl = Physics.Raycast(center, dirl.normalized, out hl, radius, ~0, QueryTriggerInteraction.Ignore);
            bool fr = Physics.Raycast(center, dirr.normalized, out hr, radius, ~0, QueryTriggerInteraction.Ignore);
            is_fronted_center = fc && (target == null || h.collider.gameObject != target);
            is_fronted_left = fl && (target == null || hl.collider.gameObject != target);
            is_fronted_right = fr && (target == null || hr.collider.gameObject != target);

            int front_count = (fc ? 1 : 0) + (fl ? 1 : 0) + (fr ? 1 : 0);
            front_dist = (fc ? h.distance : 0f) + (fl ? hl.distance : 0f) + (fr ? hr.distance : 0f);
            if (front_count > 0) front_dist = front_dist / (float)front_count;

            return is_fronted_center || is_fronted_left || is_fronted_right;
        }

        private void OnUse(PlayerCharacter character)
        {
            //Use

        }

        private void OnDeath()
        {
            rigid.velocity = Vector3.zero;
            moving = Vector3.zero;
            rigid.isKinematic = true;
            target = null;
            attack_target = null;
            move_target = transform.position;
            is_moving = false;

            foreach (Collider coll in colliders)
                coll.enabled = false;

            if (onDeath != null)
                onDeath.Invoke();

            PlayerData.Get().RemoveCharacter(GetUID());
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID());
        }

        //Find new move target while trying to avoid obstacles
        private Vector3 FindAvoidMoveTarget(Vector3 target)
        {
            Vector3 targ_dir = (target - transform.position);
            targ_dir = Quaternion.AngleAxis(avoid_angle, Vector3.up) * targ_dir; //Rotate if obstacle in front
            return transform.position + targ_dir;
        }

        public bool HasReachedMoveTarget(float distance=0.11f)
        {
            Vector3 diff = move_target - transform.position;
            return (diff.magnitude < distance);
        }

        public bool HasTarget()
        {
            return target != null;
        }

        public bool HasAttackTarget()
        {
            return target != null && GetAttackTarget() != null;
        }

        public GameObject GetAttackTarget()
        {
            GameObject target = null;
            if (attack_player != null)
                target = attack_player.gameObject;
            else if (attack_target != null)
                target = attack_target.gameObject;
            return target;
        }

        public float GetAttackTargetHitRange()
        {
            if (attack_target != null)
                return attack_range + attack_target.hit_range;
            return attack_range;
        }

        public bool IsDead()
        {
            return destruct.IsDead();
        }

        public bool IsMoving()
        {
            Vector3 moveXZ = new Vector3(moving.x, 0f, moving.z);
            return is_moving && moveXZ.magnitude > 0.2f;
        }

        public Vector3 GetMove()
        {
            return moving;
        }

        public Vector3 GetFacing()
        {
            return facing;
        }

        public bool IsGrounded()
        {
            return is_grounded;
        }

        public bool IsFronted()
        {
            return is_fronted;
        }

        public bool IsFrontedLeft()
        {
            return is_fronted_left;
        }

        public bool IsFrontedRight()
        {
            return is_fronted_right;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public Destructible GetDestructible()
        {
            return destruct;
        }

        public static Character GetNearest(Vector3 pos, float range = 999f)
        {
            Character nearest = null;
            float min_dist = range;
            foreach (Character unit in character_list)
            {
                float dist = (unit.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = unit;
                }
            }
            return nearest;
        }

        public static Character GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Character unit in character_list)
                {
                    if (unit.GetUID() == uid)
                        return unit;
                }
            }
            return null;
        }

        public static List<Character> GetAll()
        {
            return character_list;
        }

        //Spawn an existing one in the save file (such as after loading)
        public static Character Spawn(string uid, Transform parent = null)
        {
            TrainedCharacterData tcdata = PlayerData.Get().GetCharacter(uid);
            if (tcdata != null)
            {
                CharacterData cdata = CharacterData.Get(tcdata.character_id);
                if (cdata != null)
                {
                    GameObject cobj = Instantiate(cdata.character_prefab, tcdata.pos, tcdata.rot);
                    cobj.transform.parent = parent;

                    Character character = cobj.GetComponent<Character>();
                    character.data = cdata;
                    character.was_spawned = true;
                    character.unique_id.unique_id = uid;
                    return character;
                }
            }
            return null;
        }

        //Create a totally new one that will be added to save file
        public static Character Create(CharacterData data, Vector3 pos)
        {
            Quaternion rot = Quaternion.Euler(0f, 180f, 0f);
            TrainedCharacterData ditem = PlayerData.Get().AddCharacter(data.id, SceneNav.GetCurrentScene(), pos, rot);
            GameObject build = Instantiate(data.character_prefab, pos, rot);
            Character unit = build.GetComponent<Character>();
            unit.data = data;
            unit.was_spawned = true;
            unit.unique_id.unique_id = ditem.uid;
            return unit;
        }

        //Create a totally new one that will be added to save file
        public static Character Create(CharacterData data, Vector3 pos, Quaternion rot)
        {
            Character unit = Create(data, pos);
            unit.transform.rotation = rot;
            return unit;
        }
    }

}