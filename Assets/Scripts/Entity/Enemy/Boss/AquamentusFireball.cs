using System;
using UnityEngine;

namespace SQZL.Entity.Enemy
{
    public class AquamentusFireball : Hurtbox
    {
        [SerializeField] private float spawnTime = 0.2f;
        [SerializeField] private float totalLifeTime = 2f;
        [SerializeField] private float separationVelocity = 2f;
        [SerializeField] private float travelVelocity = 5f;

        internal Vector3 _travelDirection;
        internal Vector3 _separationDirection;

        private float lifetime = 0f;
        private Vector3 velocity;

        private void FixedUpdate()
        {
            if (lifetime > totalLifeTime)
            {
                Destroy(gameObject);
            }
            else if (lifetime > spawnTime)
            {
                velocity = _separationDirection * separationVelocity + _travelDirection * travelVelocity;
            }
            else
            {
                velocity = _separationDirection * separationVelocity;
            }

            transform.position += velocity * Time.fixedDeltaTime;
            lifetime += Time.fixedDeltaTime;
        }
    }
}
