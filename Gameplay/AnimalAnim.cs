using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Script that manages Animal animations
    /// </summary>

    [RequireComponent(typeof(Animal))]
    public class AnimalAnim : MonoBehaviour
    {
        private Animal animal;
        private Selectable select;
        private Animator animator;
        private Animator animator_outline;

        void Start()
        {
            select = GetComponent<Selectable>();
            animal = GetComponent<Animal>();
            animator = GetComponentInChildren<Animator>();

            GameObject outline = GetComponent<Selectable>().outline;
            if (outline != null)
                animator_outline = outline.GetComponentInChildren<Animator>();

            animal.onAttack += OnAttack;
            animal.onDamaged += OnDamaged;
            animal.onDeath += OnDeath;

            if (animator == null)
                enabled = false;
        }

        void Update()
        {
            bool paused = TheGame.Get().IsPaused();
            animator.enabled = !paused && select.AreScriptsActive();

            if (animator.enabled)
            {
                animator.SetBool("Move", animal.IsMoving() && animal.IsActive());
                animator.SetBool("Run", animal.IsRunning() && animal.IsActive());

                if (animator_outline != null)
                {
                    animator_outline.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }
            }
        }

        void OnAttack()
        {
            animator.SetTrigger("Attack");
        }

        void OnDamaged()
        {
            animator.SetTrigger("Damaged");
        }

        void OnDeath()
        {
            animator.SetTrigger("Death");
        }
    }

}