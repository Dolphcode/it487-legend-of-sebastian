using UnityEngine;
using UnityEngine.Tilemaps;

namespace SQZL.Entity.Projectile
{
    public class BoomerangProjectile : Hurtbox
    {
        [Header("Boomerang Config")] [SerializeField]
        private float initialSpeed = 10f;

        [SerializeField] private float acceleration = 5f;
        
        
        public bool IsAlive { private set; get; } = true;
        public bool Fired { private set; get; } = false;

        private float speed = 0f;
        private Vector3 direction;
        private float positionAccum = 0f;
        private bool reversing = false;

        // Update is called once per frame
        void FixedUpdate()
        {
            if (Fired && IsAlive)
            {
                transform.position += direction * speed * Time.fixedDeltaTime;
                positionAccum +=  Time.fixedDeltaTime * speed;
                speed -= acceleration * Time.fixedDeltaTime;
                if (!reversing && speed < 0f)
                {
                    reversing = true;
                    // TODO Disable collider
                }

                if (reversing && IsAlive && positionAccum < 0f)
                {
                    IsAlive = false;
                    Destroy(gameObject);
                    speed = 0f;
                }
            } 
        }

        public void FireBoomerang(TilebodyDirection direction)
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

            Fired = true;
        }
    }
}
