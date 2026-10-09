using System;
using UnityEngine;

namespace SQZL.Entity.Projectile
{
    public class BoomerangPlayerProjectile : MonoBehaviour
    {
        [Header("Boomerang Config")] [SerializeField]
        private float initialSpeed = 10f;

        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float distanceToDespawn = 1f;
        
        
        public bool IsAlive { private set; get; } = true;
        public bool Fired { private set; get; } = false;

        private float speed = 0f;
        private Vector3 direction;
        private float positionAccum = 0f;
        private bool reversing = false;

        private Collider2D _hurtboxCollider;

        public event Action OnBoomerangHit;
        public event Action OnBoomerangReturn;

        void Start()
        {
            _hurtboxCollider = GetComponent<Collider2D>();
#if DEBUG
            if (!_hurtboxCollider.isTrigger)
            {
                Debug.LogWarning(
                    "WARNING: The attached collider to this hurtbox is not a trigger. This script will not function correctly");
            }
#endif
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (Fired && IsAlive)
            {
                transform.position += direction * speed * Time.fixedDeltaTime;
                positionAccum +=  Time.fixedDeltaTime * speed;
                speed -= acceleration * Time.fixedDeltaTime;
                speed = Mathf.Clamp(speed, 0f, initialSpeed);
                if (!reversing && speed <= 0f)
                {
                    reversing = true;
                    acceleration = -acceleration;
                    // TODO Disable collider
                }

                if (reversing && IsAlive)
                {
                    Vector3 toVector = (returnTarget.transform.position - transform.position);
                    float distance = toVector.sqrMagnitude;
                    if (distance < distanceToDespawn * distanceToDespawn)
                    {
                        IsAlive = false;
                        OnBoomerangReturn?.Invoke();
                        Destroy(gameObject);
                        speed = 0f; 
                    }
                    else
                    {
                        direction = toVector.normalized;
                    }
                }
            } 
        }

        private GameObject returnTarget;
        
        public void FireBoomerang(TilebodyDirection direction, GameObject returnTarget)
        {
            speed = initialSpeed;
            switch (direction)
            {
                case TilebodyDirection.Down:
                    this.direction = Vector3.down;
                    break;
                case TilebodyDirection.Up:
                    this.direction = Vector3.up;
                    break;
                case TilebodyDirection.Left:
                    this.direction = Vector3.left;
                    break;
                case TilebodyDirection.Right:
                default:
                    this.direction = Vector3.right;
                    break;
            }

            this.returnTarget = returnTarget;
            Fired = true;
        }
        
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("Trying to stun other");
            StunnableEntityDamageHandler hitbox;
            Debug.Log(other.GetComponent<StunnableEntityDamageHandler>());
            if (other.gameObject.CompareTag("Enemy") && other.TryGetComponent(out hitbox))
            {
                // TODO Stun
                //hitbox.TryDamage(damageAmiount, doKnockback, _attackDirection);
                hitbox.TryStun();
            }
        }
    }
}
