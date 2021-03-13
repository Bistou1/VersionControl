using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Main character script, contains code for movement and for player controls/commands
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(PlayerCharacterCombat))]
    [RequireComponent(typeof(PlayerCharacterAttribute))]
    [RequireComponent(typeof(PlayerCharacterInventory))]
    [RequireComponent(typeof(PlayerCharacterCraft))]
    public class PlayerCharacter : MonoBehaviour
    {
        public int player_id = 0;

        [Header("Movement")]
        public float move_speed = 4f;
        public float move_accel = 8;
        public float rotate_speed = 180f;
        public float fall_speed = 20f;
        public float slope_angle_max = 45f;
        public float ground_detect_dist = 0.1f;
        public LayerMask ground_layer = ~0;
        public bool use_navmesh = false;

        [Header("Jump")]
        public bool can_jump = false;
        public float jump_power = 10f;
        public float jump_gravity = 20f;
        public float jump_duration = 0.5f;

        public UnityAction onJump;
        public UnityAction<string, float> onTriggerAnim;

        private Rigidbody rigid;
        private CapsuleCollider collide;
        private PlayerCharacterAttribute character_attr;
        private PlayerCharacterCombat character_combat;
        private PlayerCharacterCraft character_craft;
        private PlayerCharacterInventory character_inventory;

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
        private InventoryData auto_move_drop_inventory;
        private float auto_move_timer = 0f;
        private float jump_timer = 0f;

        private Vector3 ground_normal = Vector3.up;
        private bool controls_enabled = true;

        private bool is_grounded = false;
        private bool is_fronted = false;
        private bool is_action = false;
        private bool is_sleep = false;
        private bool is_fishing = false;

        private ActionSleep sleep_target = null;

        private Vector3[] nav_paths = new Vector3[0];
        private int path_index = 0;
        private bool calculating_path = false;
        private bool path_found = false;

        private static PlayerCharacter player_first = null;
        private static List<PlayerCharacter> players_list = new List<PlayerCharacter>();

        void Awake()
        {
            if (player_first == null || player_id < player_first.player_id)
                player_first = this;

            players_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            collide = GetComponentInChildren<CapsuleCollider>();
            character_attr = GetComponent<PlayerCharacterAttribute>();
            character_combat = GetComponent<PlayerCharacterCombat>();
            character_craft = GetComponent<PlayerCharacterCraft>();
            character_inventory = GetComponent<PlayerCharacterInventory>();
            facing = transform.forward;
            prev_pos = transform.position;
            jump_vect = Vector3.down * fall_speed;
        }

        private void OnDestroy()
        {
            players_list.Remove(this);
        }

        private void Start()
        {
            PlayerControlsMouse mouse_controls = PlayerControlsMouse.Get();
            mouse_controls.onClickFloor += OnClickFloor;
            mouse_controls.onClickObject += OnClickObject;
            mouse_controls.onClick += OnClick;
            mouse_controls.onRightClick += OnRightClick;
            mouse_controls.onHold += OnMouseHold;
            mouse_controls.onRelease += OnMouseRelease;

            if (player_id < 0)
                Debug.LogError("Player ID should be 0 or more: -1 is reserved to indicate neutral (no player)");
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
            {
                rigid.velocity = Vector3.zero;
                return;
            }

            if (IsDead())
                return;

            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mcontrols = PlayerControlsMouse.Get();
            Vector3 tmove = Vector3.zero;

            //Update auto move for moving targets
            GameObject auto_move_obj = null;
            if (auto_move_select != null && auto_move_select.type == SelectableType.Interact)
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
                    CalculateNavmesh(); //Recalculate navmesh because target moved
                }
            }

            //Navmesh calculate next path
            if (auto_move && use_navmesh && path_found && path_index < nav_paths.Length)
            {
                auto_move_target_next = nav_paths[path_index];
                Vector3 move_dir_total = auto_move_target_next - transform.position;
                move_dir_total.y = 0f;
                if (move_dir_total.magnitude < 0.2f)
                    path_index++;
            }

            //AUTO Moving (after mouse click)
            auto_move_timer += Time.fixedDeltaTime;
            if (auto_move && auto_move_timer > 0.02f) //auto_move_timer to let the navmesh time to calculate a path
            {
                Vector3 move_dir_total = auto_move_target - transform.position;
                Vector3 move_dir_next = auto_move_target_next - transform.position;
                Vector3 move_dir = move_dir_next.normalized * Mathf.Min(move_dir_total.magnitude, 1f);
                move_dir.y = 0f;

                float move_dist = Mathf.Min(move_speed * character_attr.GetSpeedMult(), move_dir.magnitude * 10f);
                tmove = move_dir.normalized * move_dist;
            }
            //Keyboard/gamepad moving
            else if(controls_enabled)
            {
                Vector3 cam_move = TheCamera.Get().GetRotation() * controls.GetMove();
                if (mcontrols.IsJoystickActive() && !character_craft.IsBuildMode())
                {
                    Vector2 joystick = mcontrols.GetJoystickDir();
                    cam_move = TheCamera.Get().GetRotation() * new Vector3(joystick.x, 0f, joystick.y);
                }
                tmove = cam_move * move_speed * character_attr.GetSpeedMult();
            }

            //Stop moving if doing action
            if (is_action)
                tmove = Vector3.zero;

            //Check if grounded
            DetectGrounded();

            //Add Falling to the move vector
            if (!is_grounded || jump_timer > 0f)
            {
                if (jump_timer <= 0f)
                    jump_vect = Vector3.MoveTowards(jump_vect, Vector3.down * fall_speed, jump_gravity * Time.fixedDeltaTime);
                tmove += jump_vect;
            }
            //Add slope angle
            else if(is_grounded)
            {
                tmove = Vector3.ProjectOnPlane(tmove.normalized, ground_normal).normalized * tmove.magnitude;
            }

            //Apply the move calculated previously
            move = Vector3.Lerp(move, tmove, move_accel * Time.fixedDeltaTime);
            rigid.velocity = move;

            //Calculate Facing
            if (!is_action && IsMoving())
            {
                facing = new Vector3(move.x, 0f, move.z).normalized;
            }

            //Rotate character with right joystick when not in free rotate mode
            bool freerotate = TheCamera.Get().IsFreeRotation();
            if (!is_action && !freerotate && controls.IsGamePad())
            {
                Vector2 look = controls.GetFreelook();
                Vector3 look3 = TheCamera.Get().GetRotation() * new Vector3(look.x, 0f, look.y);
                if(look3.magnitude > 0.5f)
                    facing = look3.normalized;
            }

            //Apply the facing
            Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);
            rigid.MoveRotation(Quaternion.RotateTowards(rigid.rotation, targ_rot, rotate_speed * Time.fixedDeltaTime));

            //Fronted (need to be done after facing to avoid issues)
            DetectFronted();

            //Check the average traveled movement (allow to check if character is stuck)
            Vector3 last_frame_travel = transform.position - prev_pos;
            move_average = Vector3.MoveTowards(move_average, last_frame_travel, 1f * Time.fixedDeltaTime);
            prev_pos = transform.position;

            //Stop auto move
            bool stuck_somewhere = move_average.magnitude < 0.02f && auto_move_timer > 1f;
            if (stuck_somewhere)
                auto_move = false;

            //Stop the click auto move when moving with keyboard/joystick/gamepad
            if (controls.IsMoving() || mcontrols.IsJoystickActive() || mcontrols.IsDoubleTouch())
                StopAutoAction();
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            //Save position
            Data.position = transform.position;

            //Controls
            PlayerControls controls = PlayerControls.Get();
            jump_timer -= Time.deltaTime;

            //Stop sleep
            if (is_action || IsMoving() || sleep_target == null)
                StopSleep();

            //Activate Selectable when near
            Vector3 move_dir = auto_move_target - transform.position;
            if (auto_move && !is_action && auto_move_select != null && move_dir.magnitude < auto_move_select.use_range)
            {
                auto_move = false;
                auto_move_select.Use(this, auto_move_target);
                auto_move_select = null;
            }

            //Finish construction when near clicked spot
            Buildable current_buildable = character_craft.GetCurrentBuildable();
            if (auto_move && !is_action && character_craft.ClickedBuild() && current_buildable != null && move_dir.magnitude < current_buildable.build_distance)
            {
                auto_move = false;
                character_craft.StartCraftBuilding(auto_move_target);
            }

            //Stop move & drop when near clicked spot
            if (auto_move && !is_action && move_dir.magnitude < 0.35f)
            {
                auto_move = false;
                character_inventory.DropItem(auto_move_drop_inventory, auto_move_drop);
            }

            //Stop attacking if target cant be attacked anymore (tool broke, or target died...)
            if (!character_combat.CanAttack(auto_move_attack))
                auto_move_attack = null;

            //Controls
            if (controls_enabled && !is_action) {

                //Check if panel is focused
                KeyControlsUI ui_controls = KeyControlsUI.Get();
                bool panel_focus = controls.gamepad_controls && ui_controls != null && ui_controls.IsPanelFocus();
                if (!panel_focus)
                {
                    //Press Action button
                    if (controls.IsPressAction())
                    {
                        if (character_craft.CanBuild())
                        {
                            character_craft.StartCraftBuilding();
                        }
                        else if (!panel_focus)
                        {
                            InteractWithNearest();
                        }
                    }

                    //Press attack
                    if (Combat.can_attack && controls.IsPressAttack())
                    {
                        AttackNearest();
                    }

                    //Press jump
                    if (can_jump && controls.IsPressJump())
                    {
                        Jump();
                    }
                }

                if (controls.IsPressUISelect())
                {
                    if (character_craft.CanBuild())
                    {
                        character_craft.StartCraftBuilding();
                    }
                }
            }
        }

        //Detect if character is on the floor
        private void DetectGrounded()
        {
            Vector3 scale = transform.lossyScale;
            float hradius = collide.height * scale.y * 0.5f + ground_detect_dist; //radius is half the height minus offset
            float radius = collide.radius * (scale.x + scale.y) * 0.5f;
            Vector3 center = GetColliderCenter();
            Vector3 root = transform.position;

            float gdist; Vector3 gnormal;
            is_grounded = PhysicsTool.DetectGround(root, center, hradius, radius, ground_layer, out gdist, out gnormal);
            ground_normal = gnormal;

            float slope_angle = Vector3.Angle(ground_normal, Vector3.up);
            is_grounded = is_grounded && slope_angle <= slope_angle_max;
        }

        //Detect if there is an obstacle in front of the character
        private void DetectFronted()
        {
            Vector3 scale = transform.lossyScale;
            float hradius = collide.height * scale.y * 0.5f - 0.02f; //radius is half the height minus offset
            float radius = collide.radius * (scale.x + scale.y) * 0.5f + 0.5f;

            Vector3 center = GetColliderCenter();
            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.up * hradius;
            Vector3 p3 = center + Vector3.down * hradius;

            RaycastHit h1, h2, h3;
            bool f1 = PhysicsTool.RaycastCollision(p1, facing * radius, out h1);
            bool f2 = PhysicsTool.RaycastCollision(p2, facing * radius, out h2);
            bool f3 = PhysicsTool.RaycastCollision(p3, facing * radius, out h3);

            is_fronted = f1 || f2 || f3;

            //Debug.DrawRay(p1, facing * radius);
            //Debug.DrawRay(p2, facing * radius);
            //Debug.DrawRay(p3, facing * radius);
        }

        //--- Generic Actions ----

        //Call animation directly
        public void TriggerAnim(string anim, float duration = 0f)
        {
            if (onTriggerAnim != null)
                onTriggerAnim.Invoke(anim, duration);
        }

        //Just animate the character for X seconds, and prevent it from doing other things, then callback
        public void TriggerAction(string animation, Vector3 face_at, float duration, UnityAction callback = null)
        {
            if (!is_action)
            {
                FaceTorward(face_at);
                TriggerAnim(animation, duration);
                StartCoroutine(RunActionRoutine(duration, callback));
            }
        }

        public void TriggerAction(float duration, UnityAction callback = null)
        {
            if (!is_action)
            {
                StartCoroutine(RunActionRoutine(duration, callback));
            }
        }

        private IEnumerator RunActionRoutine(float action_duration, UnityAction callback=null)
        {
            is_action = true;
            yield return new WaitForSeconds(action_duration);
            is_action = false;
            if (callback != null)
                callback.Invoke();
        }

        public void SetDoingAction(bool action)
        {
            is_action = action;
        }

        //---- Special actions

        public void Sleep(ActionSleep sleep_target)
        {
            if (!is_sleep)
            {
                this.sleep_target = sleep_target;
                is_sleep = true;
                auto_move = false;
                auto_move_attack = null;
                TheGame.Get().SetGameSpeedMultiplier(sleep_target.sleep_speed_mult);
            }
        }

        public void StopSleep()
        {
            if (is_sleep)
            {
                is_sleep = false;
                sleep_target = null;
                TheGame.Get().SetGameSpeedMultiplier(1f);
            }
        }

        //Fish item from a fishing spot
        public void FishItem(ItemProvider source, int quantity)
        {
            if (source != null && source.HasItem() && character_inventory.CanTakeItem(source.item, quantity))
            {
                is_fishing = true;

                if (source != null)
                    FaceTorward(source.transform.position);

                TriggerAction(0.4f, () =>
                {
                    StartCoroutine(FishRoutine(source, quantity));
                });
            }
        }

        private IEnumerator FishRoutine(ItemProvider source, int quantity)
        {
            is_fishing = true;

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
                character_inventory.GainItem(source.item, quantity);
            }

            is_fishing = false;
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

        //----- Player Orders ----------

        public void MoveTo(Vector3 pos)
        {
            auto_move = true;
            auto_move_target = pos;
            auto_move_target_next = pos;
            auto_move_select = null;
            auto_move_attack = null;
            auto_move_drop = -1;
            auto_move_drop_inventory = null;
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
            auto_move_drop_inventory = null;
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

        public void InteractWith(Selectable selectable, Vector3 pos)
        {
            bool can_interact = selectable.CanBeInteracted();
            bool surface = selectable.type == SelectableType.InteractSurface || !can_interact;

            auto_move_select = can_interact ? selectable : null;
            auto_move_target = surface ? pos : selectable.transform.position;
            auto_move_target_next = surface ? pos : selectable.transform.position;

            auto_move = true;
            auto_move_drop = -1;
            auto_move_drop_inventory = null;
            auto_move_timer = 0f;
            path_found = false;
            calculating_path = false;
            auto_move_attack = null;

            character_craft.CancelCrafting();
            CalculateNavmesh();
        }

        public void Attack(Destructible target)
        {
            if (character_combat.CanAttack(target))
            {
                auto_move = true;
                auto_move_select = null;
                auto_move_attack = target;
                auto_move_target = target.transform.position;
                auto_move_target_next = target.transform.position;
                auto_move_drop = -1;
                auto_move_drop_inventory = null;
                auto_move_timer = 0f;
                path_found = false;
                calculating_path = false;

                character_craft.CancelCrafting();
                CalculateNavmesh();
            }
        }

        //Shoot arrow in facing direction
        public void AttackRanged()
        {
            Combat.DoAttackNoTarget();
        }

        public void InteractWithNearest()
        {
            Selectable nearest = Selectable.GetNearestAutoInteract(transform.position, 4f);
            if (nearest != null)
            {
                InteractWith(nearest, nearest.GetClosestInteractPoint(transform.position));
            }
        }

        public void AttackNearest()
        {
            Destructible destruct = Destructible.GetNearestAutoAttack(this, transform.position, 4f);
            if (Combat.HasRangedProjectile())
            {
                AttackRanged();
            }
            else if (destruct != null)
            {
                Attack(destruct);
            }
        }

        public void StopMove()
        {
            StopAutoAction();
            move = Vector3.zero;
            rigid.velocity = Vector3.zero;
        }

        public void StopAutoAction()
        {
            auto_move = false;
            auto_move_select = null;
            auto_move_attack = null;
        }

        public void Kill()
        {
            character_combat.Kill();
        }

        public void EnableControls()
        {
            controls_enabled = true;
        }

        public void DisableControls()
        {
            controls_enabled = false;
            StopAutoAction();
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

            PlayerUI.Get(player_id)?.CancelSelection();
        }

        private void OnMouseHold(Vector3 pos)
        {
            if (!controls_enabled)
                return;

            if (TheGame.IsMobile())
                return; //On mobile, use joystick instead, no mouse hold

            //Only hold for normal movement, if interacting dont change while holding
            if (character_craft.GetCurrentBuildable() == null && auto_move_select == null && auto_move_attack == null)
            {
                UpdateMoveTo(pos);
            }
        }

        private void OnMouseRelease(Vector3 pos)
        {
            if (TheGame.IsMobile())
            {
                character_craft.TryBuildAt(pos);
            }
        }

        private void OnClickFloor(Vector3 pos)
        {
            if (!controls_enabled)
                return;

            //Cancel previous build
            if (character_craft.ClickedBuild())
                character_craft.CancelCrafting();

            //Build mode
            if (character_craft.IsBuildMode())
            {
                if(!TheGame.IsMobile()) //On mobile, will build on mouse release
                    character_craft.TryBuildAt(pos);
            }
            //Move to clicked position
            else
            {
                MoveTo(pos);

                PlayerUI ui = PlayerUI.Get(player_id);
                auto_move_drop = ui != null ? ui.GetSelectedSlotIndex() : -1;
                auto_move_drop_inventory = ui != null ? ui.GetSelectedSlotInventory() : null;
            }
        }

        private void OnClickObject(Selectable selectable, Vector3 pos)
        {
            if (!controls_enabled)
                return;

            if (selectable == null)
                return;

            if (character_craft.IsBuildMode())
            {
                OnClickFloor(pos);
                return;
            }

            selectable.Select();

            //Attack target ?
            Destructible target = selectable.GetDestructible();
            if (target != null && character_combat.CanAutoAttack(target))
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
            Selectable group_select = Selectable.GetNearestGroup(group, transform.position);
            return group_select != null && group_select.IsInUseRange(transform.position);
        }

        public ActionSleep GetSleepTarget()
        {
            return sleep_target;
        }

        public Destructible GetAutoAttackTarget(){
            return auto_move_attack;
        }

        public bool IsDead()
        {
            return character_combat.IsDead();
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
            return moveXZ.magnitude > move_speed * character_attr.GetSpeedMult() * 0.25f;
        }

        public Vector3 GetMove()
        {
            return move;
        }

        public Vector3 GetFacing()
        {
            return facing;
        }

        public Vector3 GetColliderCenter()
        {
            Vector3 scale = transform.lossyScale;
            return collide.transform.position + Vector3.Scale(collide.center, scale);
        }

        public bool IsFronted()
        {
            return is_fronted;
        }

        public bool IsGrounded()
        {
            return is_grounded;
        }

        public bool IsControlsEnabled()
        {
            return controls_enabled;
        }

        public PlayerCharacterCombat Combat
        {
            get { return character_combat; }
        }

        public PlayerCharacterAttribute Attributes
        {
            get {return character_attr;}
        }

        public PlayerCharacterCraft Crafting
        {
            get { return character_craft; }
        }

        public PlayerCharacterInventory Inventory
        {
            get { return character_inventory; }
        }

        public PlayerCharacterData Data //Keep for compatibility with other versions, same than SaveData
        {
            get { return PlayerCharacterData.Get(player_id); }
        }

        public PlayerCharacterData SaveData
        {
            get { return PlayerCharacterData.Get(player_id); }
        }

        public InventoryData InventoryData
        {
            get { return character_inventory.InventoryData; }
        }

        public InventoryData EquipData
        {
            get { return character_inventory.EquipData; }
        }

        public static PlayerCharacter GetFirst()
        {
            return player_first;
        }

        public static PlayerCharacter Get(int player_id=0)
        {
            foreach (PlayerCharacter player in players_list)
            {
                if (player.player_id == player_id)
                    return player;
            }
            return null;
        }

        public static List<PlayerCharacter> GetAll()
        {
            return players_list;
        }
    }

}