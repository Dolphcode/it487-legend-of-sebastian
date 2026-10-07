using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using SQZL.World.Interactable;

namespace SQZL.Entity.Enemy
{
    public class KeeseController : BaseEnemyController
    {
        private const float BOUND_CHECK_THRESHOLD = 1e-2f;
        private const float END_MOVEMENT_THRESHOLD = 1e-2f;
        
        [Header("Keese Config")] [SerializeField]
        private int unitsToTopSpeed = 10;

        [SerializeField] private float timePerTile = 0.2f;
        [SerializeField] private float stayWeight = 1.0f;
        [SerializeField] private float redirectWeight = 1.0f;
        [SerializeField] private float accelWeight = 1.0f; // Probability of decelerating
        
        // Keese movement state
        [SerializeField] private Vector2 localRoomPosition;
        private KeeseState currentState;
        private float speed;
        private Vector2 direction;

        // Bounds Min serves as a new computed offset for keese
        // Bounds size is the computed size for keese
        private Vector2 boundsSize, boundsMin;
        private float acceleration;
        private float maxSpeed;
        
        // State probabilities
        private float[] flightStateRandom;

        private Animator _animator;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
        {
            base.Start();
            
            // Compute values
            maxSpeed = 1f / timePerTile;
            acceleration = (maxSpeed * maxSpeed) / (2f * (float)unitsToTopSpeed); // Some type a kinematic
            boundsMin = new Vector2(walkableRegionOffset.x, walkableRegionOffset.y - (0.5f * _collider2D.bounds.size.y));
            boundsSize = new Vector2(walkableRegion.x * 2f - _collider2D.bounds.size.x,
                walkableRegion.y * 2f - _collider2D.bounds.size.y);
            Debug.Log($"Bounds Min: {boundsMin}, Bounds Size: {boundsSize}, Bounds Max: {boundsMin + boundsSize}, Walkable Region: {walkableRegionOffset}, Bounds: {_collider2D.bounds.size}, LocalScale: {transform.localScale}");
           
            // Initialize State
            localRoomPosition = new Vector2(transform.position.x - boundsMin.x, 
                transform.position.y - boundsMin.y);
            Debug.Log($"Starting position = {localRoomPosition}");
            currentState = KeeseState.SPEEDING;
            speed = 0f;
            direction = Vector2.zero;
            SelectDirection(); 
            
            //  Initialize Random Weights
           float[] flightStateRandomInit = { stayWeight, stayWeight + redirectWeight, stayWeight + redirectWeight + accelWeight };
           this.flightStateRandom = flightStateRandomInit;
           
           // Animator
           _animator = GetComponent<Animator>();
           
           // Start Keese state machine
           StartCoroutine(AccelerateState());
        }

        protected override void FixedUpdate()
        {
            transform.position = localRoomPosition + boundsMin;
            _animator.speed = Mathf.Clamp(speed / maxSpeed, 0f, 1f);
        }

        private IEnumerator AccelerateState()
        {
            // Accelerate + Move over time
            for (float i = maxSpeed / acceleration; i > END_MOVEMENT_THRESHOLD; i -= Time.fixedDeltaTime)
            {
                // Perform the physics
                float v0 = speed;
                float v1 = Mathf.Clamp(speed + (acceleration * ((currentState == KeeseState.SPEEDING) ? 1f : -1f)) 
                    * Time.fixedDeltaTime, 0f, maxSpeed);
                Debug.Log($"Testing {v0} - {v1} with {i} time left after taking {Time.fixedDeltaTime}");
                float dx = 0.5f * (v0 + v1) * Time.fixedDeltaTime;
                
                // Correct
                Vector2 tempRoomPosition = localRoomPosition + direction * dx;
                float corrFrac = 0f;
                bool overshot = false;
                if (tempRoomPosition.x < 0f)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.x) / dx;
                } else if (tempRoomPosition.x > boundsSize.x)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.x - boundsSize.x) / dx;
                } else if (tempRoomPosition.y < 0f)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.y) / dx;
                } else if (tempRoomPosition.y > boundsSize.y)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.y - boundsSize.y) / dx;
                }

                if (overshot)
                {
                    localRoomPosition += direction * dx * corrFrac;
                    direction = -direction;
                    localRoomPosition += direction * dx * (1f - corrFrac);
                } else localRoomPosition += direction * dx;
                
                // Update speed
                speed = v1;
                yield return new WaitForFixedUpdate();
            }
            
            // Round off Values
            speed = Mathf.Round(speed);
            localRoomPosition.x = Mathf.Round(localRoomPosition.x);
            localRoomPosition.y = Mathf.Round(localRoomPosition.y);
            
            // Pick a next action
            if (currentState == KeeseState.SLOWING)
            {
                // Wait synced to fixed update
                currentState = KeeseState.WAITING;
                for (float wait = 2.0f; wait > END_MOVEMENT_THRESHOLD; wait -= Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                }
                
                // Potentially pick a new direction
                float randSelect = Random.Range(0f, flightStateRandom[1]);
                if (randSelect < flightStateRandom[0])
                {
                    SelectDirection();
                }
                
                // Then select next state
                currentState = KeeseState.SPEEDING;
                StartCoroutine(AccelerateState());

            } else if (currentState == KeeseState.SPEEDING)
            {
                // Potentially pick a new direction
                float randSelect = Random.Range(0f, flightStateRandom[1]);
                if (randSelect >= flightStateRandom[0])
                {
                    SelectDirection();
                }
                
                currentState = KeeseState.FLYING;
                StartCoroutine(MoveState());
            }
        }

        private IEnumerator MoveState()
        {
            for (float i = timePerTile; i > END_MOVEMENT_THRESHOLD; i -= Time.fixedDeltaTime)
            {
                float dx = speed * Time.fixedDeltaTime;
                //localRoomPosition += direction * dx;
                
                // Correct
                Vector2 tempRoomPosition = localRoomPosition + direction * dx;
                float corrFrac = 0f;
                bool overshot = false;
                if (tempRoomPosition.x < 0f)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.x) / dx;
                } else if (tempRoomPosition.x > boundsSize.x)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.x - boundsSize.x) / dx;
                } else if (tempRoomPosition.y < 0f)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.y) / dx;
                } else if (tempRoomPosition.y > boundsSize.y)
                {
                    overshot = true;
                    corrFrac = (localRoomPosition.y - boundsSize.y) / dx;
                }

                if (overshot)
                {
                    localRoomPosition += direction * dx * corrFrac;
                    direction = -direction;
                    localRoomPosition += direction * dx * (1f - corrFrac);
                } else localRoomPosition += direction * dx;
                
                yield return new WaitForFixedUpdate();
            }
            
            // Round off Values
            speed = Mathf.Round(speed);
            localRoomPosition.x = Mathf.Round(localRoomPosition.x);
            localRoomPosition.y = Mathf.Round(localRoomPosition.y);

            float randValue = Random.Range(0f, flightStateRandom[2]);
            if (randValue < flightStateRandom[0])
            {
                if (!CheckInBounds(localRoomPosition + direction))
                {
                    direction = -direction;
                }
                StartCoroutine(MoveState());
            } else if (randValue < flightStateRandom[1])
            {
                SelectDirection();
                StartCoroutine(MoveState());
            }
            else
            {
                currentState = KeeseState.SLOWING;
                StartCoroutine(AccelerateState());
            }

        }
        
        private readonly Vector2[] DIRECTION_POOL =
        {
            Vector2.up, Vector2.left, Vector2.down, Vector2.right, Vector2.one, Vector2.down + Vector2.left, Vector2.up + Vector2.left, Vector2.down + Vector2.right
        };
        
        public void SelectDirection()
        {
            List<Vector2> validPool = new();
            foreach (Vector2 dir in DIRECTION_POOL)
                if (CheckInBounds(dir + localRoomPosition)) validPool.Add(dir);
            direction = validPool[Random.Range(0, validPool.Count)];
        }

        public bool CheckInBounds(Vector2 testPos)
        {
            return testPos.x > BOUND_CHECK_THRESHOLD && testPos.x < boundsSize.x - BOUND_CHECK_THRESHOLD &&
                   testPos.y > BOUND_CHECK_THRESHOLD && testPos.y < boundsSize.y - BOUND_CHECK_THRESHOLD;
        }

        private enum KeeseState
        {
            FLYING, SPEEDING, SLOWING, WAITING
        }
        
        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            if (iframes <= 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f)
                {
                    base.DisconnectSpawnManager();
                    Destroy(gameObject);
                }
                else
                {
                    iframes = iframeTime;
                }
            }
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            _sprite.enabled = true;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _sprite.enabled = false;
        }
    }
}
