﻿using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Animal behavior script for wandering, escaping, or chasing the player
    /// </summary>

    public enum AnimalState
    {
        Wander = 0,
        Alerted = 2,
        Escape = 4,
        Attack = 6,
        MoveTo = 10,
        Dead = 20,
    }

    public enum AnimalBehavior
    {
        None = 0,   //Custom behavior from another script
        Escape = 5,  //Escape on sight
        PassiveEscape = 10,  //Escape if attacked 
        PassiveDefense = 15, //Attack if attacked
        Aggressive = 20, //Attack on sight, goes back after a while
        VeryAggressive = 25, //Attack on sight, will not stop following
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    public class Animal : MonoBehaviour
    {
        [Header("Move")]
        public float wander_speed = 2f;
        public float run_speed = 5f;
        public float wander_range = 10f;
        public float wander_interval = 10f;

        [Header("Vision")]
        public float detect_range = 5f;
        public float detect_angle = 360f;
        public float detect_360_range = 1f;
        public float alerted_duration = 0.5f; //How fast it detects threats

        [Header("Actions")]
        public float action_duration = 10f; //How long will it attack/escape targets

        [Header("Behavior")]
        public AnimalBehavior behavior;

        public UnityAction onAttack;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        private AnimalState state;
        private Character character;
        private Destructible destruct;

        private Vector3 start_pos;

        private PlayerCharacter player_target = null;
        private Destructible attack_target = null;
        private Vector3 wander_target;

        private bool is_running = false;
        private float state_timer = 0f;
        private bool is_active = false;

        private float lure_interest = 8f;
        private bool force_action = false;

        void Awake()
        {
            character = GetComponent<Character>();
            destruct = GetComponent<Destructible>();
            start_pos = transform.position;

            character.onAttack += OnAttack;
            destruct.onDamaged += OnTakeDamage;
            destruct.onDeath += OnKill;
            state_timer = 99f; //Find wander right away

            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Optimization, dont run if too far
            float dist = (PlayerCharacter.Get().transform.position - transform.position).magnitude;
            is_active = (state != AnimalState.Wander && state != AnimalState.Dead) || dist < Mathf.Max(detect_range * 2f, 20f);

            if (!is_active && character.IsMoving())
                character.Stop();
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (state == AnimalState.Dead)
                return;

            if (!is_active)
                return;

            if (behavior == AnimalBehavior.None)
                return;

            state_timer += Time.deltaTime;

            if(state != AnimalState.MoveTo)
                is_running = (state == AnimalState.Escape || state == AnimalState.Attack);

            character.move_speed = is_running ? run_speed : wander_speed;

            //States
            if (state == AnimalState.Wander)
            {
                if (behavior != AnimalBehavior.None)
                {
                    if (state_timer > wander_interval)
                    {
                        state_timer = Random.Range(-1f, 1f);
                        FindWanderTarget();
                        character.MoveTo(wander_target);
                    }

                    if (behavior == AnimalBehavior.Aggressive || behavior == AnimalBehavior.VeryAggressive || behavior == AnimalBehavior.Escape)
                    {
                        DetectThreat();

                        if (GetTarget() != null)
                        {
                            character.Stop();
                            ChangeState(AnimalState.Alerted);
                        }
                    }
                }
            }

            if (state == AnimalState.Alerted)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    character.Stop();
                    ChangeState(AnimalState.Wander);
                    return;
                }

                character.FaceTorward(target.transform.position);

                if (state_timer > alerted_duration)
                {
                    ReactToThreat();
                }
            }

            if (state == AnimalState.Escape)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    StopAction();
                    return;
                }

                if (!force_action && state_timer > action_duration)
                {
                    Vector3 targ_dir = (target.transform.position - transform.position);
                    targ_dir.y = 0f;

                    if (targ_dir.magnitude > detect_range && state_timer > action_duration)
                    {
                        ChangeState(AnimalState.Wander);
                        character.Stop();
                    }
                }

            }

            if (state == AnimalState.Attack)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    StopAction();
                    return;
                }

                if (!force_action && behavior != AnimalBehavior.VeryAggressive && state_timer > action_duration)
                {
                    Vector3 targ_dir = target.transform.position - transform.position;
                    Vector3 start_dir = start_pos - transform.position;

                    if (targ_dir.magnitude > detect_range || start_dir.magnitude > detect_range)
                    {
                        StopAction();
                    }
                }
            }

            if (state == AnimalState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction();
            }
        }

        //Detect if the player is in vision
        private void DetectThreat()
        {
            PlayerCharacter character = PlayerCharacter.Get();
            Vector3 char_dir = (character.transform.position - transform.position);
            float min_dist = char_dir.magnitude;
            if (min_dist < detect_range)
            {
                float dangle = detect_angle / 2f; // /2 for each side
                float angle = Vector3.Angle(transform.forward, char_dir.normalized);
                if (angle < dangle || char_dir.magnitude < detect_360_range)
                {
                    player_target = character;
                    attack_target = null;
                }
            }

            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - transform.position);
                    if (dir.magnitude < detect_range && dir.magnitude < min_dist)
                    {
                        Destructible destruct = selectable.GetComponent<Destructible>();
                        if (destruct && (destruct.attack_group == AttackGroup.Ally || destruct.attack_group == AttackGroup.Enemy))
                        {
                            if (destruct.attack_group == AttackGroup.Ally || destruct.team_group != this.destruct.team_group)
                            {
                                attack_target = destruct;
                                player_target = null;
                                min_dist = dir.magnitude;
                            }
                        }
                    }
                }
            }
        }

        //React to player if seen by animal
        private void ReactToThreat()
        {
            GameObject target = GetTarget();

            if (target == null)
                return;

            if (behavior == AnimalBehavior.Escape || behavior == AnimalBehavior.PassiveEscape)
            {
                ChangeState(AnimalState.Escape);
                character.Escape(target);
            }
            else if (behavior == AnimalBehavior.Aggressive || behavior == AnimalBehavior.VeryAggressive || behavior == AnimalBehavior.PassiveDefense)
            {
                ChangeState(AnimalState.Attack);
                if (player_target)
                    character.Attack(player_target);
                else if (attack_target)
                    character.Attack(attack_target);
            }
        }

        private GameObject GetTarget()
        {
            GameObject target = null;
            if (player_target != null)
                target = player_target.gameObject;
            else if (attack_target != null)
                target = attack_target.gameObject;
            return target;
        }

        private void FindWanderTarget()
        {
            float range = Random.Range(0f, wander_range);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 pos = start_pos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;
            wander_target = pos;

            Lure lure = Lure.GetNearestInRange(transform.position);
            if (lure != null)
            {
                Vector3 dir = lure.transform.position - transform.position;
                dir.y = 0f;

                Vector3 center = transform.position + dir.normalized * dir.magnitude * 0.5f;
                if (lure_interest < 4f)
                    center = lure.transform.position;

                float range2 = Mathf.Clamp(lure_interest, 1f, wander_range);
                Vector3 pos2 = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range2;
                wander_target = pos2;

                lure_interest = lure_interest * 0.5f;
                if (lure_interest <= 0.2f)
                    lure_interest = 8f;
            }
        }

        public void AttackTarget(PlayerCharacter target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Attack);
                this.player_target = target;
                this.attack_target = null;
                force_action = true;
                character.Attack(target);
            }
        }

        public void EscapeTarget(PlayerCharacter target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Escape);
                this.player_target = target;
                this.attack_target = null;
                force_action = true;
                character.Escape(target.gameObject);
            }
        }

        public void AttackTarget(Destructible target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Attack);
                this.attack_target = target;
                this.player_target = null;
                force_action = true;
                character.Attack(target);
            }
        }

        public void EscapeTarget(Destructible target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Escape);
                this.attack_target = target;
                this.player_target = null;
                force_action = true;
                character.Escape(target.gameObject);
            }
        }

        public void MoveToTarget(Vector3 pos, bool run)
        {
            is_running = run;
            force_action = true;
            ChangeState(AnimalState.MoveTo);
            character.MoveTo(pos);
        }

        public void StopAction()
        {
            character.Stop();
            is_running = false;
            force_action = false;
            player_target = null;
            attack_target = null;
            ChangeState(AnimalState.Wander);
        }

        public void ChangeState(AnimalState state)
        {
            this.state = state;
            state_timer = 0f;
            lure_interest = 8f;
        }

        private void OnAttack()
        {
            if (onAttack != null)
                onAttack.Invoke();
        }

        private void OnTakeDamage()
        {
            if (IsDead())
                return;

            if (state == AnimalState.Wander || state == AnimalState.Alerted || state_timer > action_duration){
                DetectThreat();
                ReactToThreat();
            }

            if (onDamaged != null)
                onDamaged.Invoke();
        }

        private void OnKill()
        {
            state = AnimalState.Dead;

            if (onDeath != null)
                onDeath.Invoke();
        }

        public bool IsDead()
        {
            return character.IsDead();
        }

        public bool IsActive()
        {
            return is_active;
        }

        public bool IsMoving()
        {
            return character.IsMoving();
        }

        public bool IsRunning()
        {
            return character.IsMoving() && is_running;
        }

        public string GetUID()
        {
            return character.GetUID();
        }
    }

}
