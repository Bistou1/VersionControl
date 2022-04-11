using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// A projectile shot with a ranged weapon
    /// </summary>

    public class Projectile : MonoBehaviour
    {
        public int damage = 1;
        public float speed = 10f;
        public float duration = 10f;
        public float gravity = 0.2f;

        public AudioClip shoot_sound;

        [HideInInspector]
        public Vector3 dir;

        private float timer = 0f;

        void Start()
        {
            TheAudio.Get().PlaySFX("projectile", shoot_sound);
        }

        void Update()
        {
            transform.position += dir * speed * Time.deltaTime;
            dir += gravity * Vector3.down * Time.deltaTime;

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector2.up);

            timer += Time.deltaTime;
            if (timer > duration)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider collision)
        {
            Destructible destruct = collision.GetComponent<Destructible>();
            if (destruct != null && !destruct.attack_melee_only)
            {
                collision.GetComponent<Destructible>().DealDamage(damage);
                Destroy(gameObject);
            }

        }
    }

}