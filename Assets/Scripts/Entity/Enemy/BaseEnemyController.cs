using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.World;

namespace SQZL.Entity.Enemy
{
    public abstract class BaseEnemyController : BaseEntityDamageHandler
    {
        
        [Header("Movement Config")] [SerializeField]
        protected float moveSpeed = 5f;
        [SerializeField] [Range(0f, 1.0e-4f)] protected float blockTestThreshold = 1.0e-5f;
        [SerializeField] protected float motionBias = 0.1f;

        [Header("Collision Config")] [SerializeField]
        protected ContactFilter2D contactFilter;

        [Header("Spawn Config")]
        [SerializeField] protected bool startActive = false;

        [Header("Entity Health Config")] [SerializeField]
        protected float maxHP;
        [SerializeField] protected float iframeTime;

        #region WALKABLE_FIELDS
        [Header("Walkable Region Config")]
        [SerializeField, HideInInspector] protected Vector2Int walkableRegion;

        [SerializeField, HideInInspector] protected Vector2Int walkableRegionOffset;
        [SerializeField, HideInInspector] protected bool[] validTiles;
        [SerializeField, HideInInspector] protected List<Tilemap> wallTilemaps;
        [SerializeField, HideInInspector] protected bool preCached = false;
        #endregion

        protected float currentHP;
        protected float iframes = 0f;
        protected Vector3 startPosition;
        
        protected Tilebody2D _tilebody2D;
        protected BoxCollider2D _collider2D;
        protected SpriteRenderer _sprite;
        private RoomSpawnManager _spawnManager;

        public bool ControllerIsActive { protected set; get; } = true;

        public event Action<BaseEnemyController> OnEnemyDeath;

        /// <summary>
        /// So that derived classes can invoke on enemy death
        /// </summary>
        protected void InvokeEnemyDeath()
        {
            OnEnemyDeath?.Invoke(this);
        }

        /// <summary>
        /// PLEASE DO NOT OVERRIDE AWAKE. THIS WILL BE USED BY THE BASE ENEMY CONTROLLER TO SET UP BASE ENEMY STUFF
        /// DO YOUR STUFF IN START OR CALL BASE AWAKE PLEASE
        /// </summary>
        protected virtual void Awake()
        {
            currentHP = maxHP;
            startPosition = transform.position;
            _tilebody2D = GetComponent<Tilebody2D>();
            _collider2D = GetComponent<BoxCollider2D>();
            _sprite = GetComponent<SpriteRenderer>();
            
            // Should we start active
            ControllerIsActive = startActive;
        }

        protected virtual void Start()
        {
            if (!transform.parent.TryGetComponent<RoomSpawnManager>(out _spawnManager))
            {
                Debug.LogError($"An enemy was created that was not a child of a spawn manager: {gameObject.name}");
            }
            
            Debug.Log($"I am connecting my functions to {_spawnManager.gameObject.name}");
            _spawnManager.OnLoaded += OnRoomLoad;
            _spawnManager.OnUnloaded += OnRoomUnload;
        }

        protected virtual void FixedUpdate()
        {
            // I-Frame eval
            if (iframes > 0f)
            {
                iframes -= Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// Basic try damage function, some enemies may need to override this to implement features i.e. Knockback
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="knockback"></param>
        /// <param name="attackDir"></param>
        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            if (iframes <= 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f)
                {
                    DisconnectSpawnManager();
                    Destroy(gameObject);
                }
            }
        }
        
        #region GATE_AND_SPAWNING

        /// <summary>
        /// Called when the player enters the room and thus the enemy "Spawns"
        /// </summary>
        public virtual void OnSpawn()
        {
            transform.position = startPosition;
        }

        /// <summary>
        /// Called when the player leaves the room and thus the enemy "Despawns"
        /// </summary>
        public virtual void OnDespawn()
        {
            
        }

        protected void DisconnectSpawnManager()
        {
            _spawnManager.OnLoaded -= OnRoomLoad;
            _spawnManager.OnUnloaded -= OnRoomUnload;
        }
        
        private void OnRoomUnload(RoomSpawnManager roomSpawnManager)
        {
            if (ControllerIsActive)
            {
                ControllerIsActive = false;
                OnDespawn();
            }
        }

        private void OnRoomLoad(RoomSpawnManager roomSpawnManager)
        {
            if (!ControllerIsActive)
            {
                ControllerIsActive = true;
                transform.position = startPosition; // TODO maybe don't do this???
                OnSpawn();
            }
        }
        #endregion
        
        public bool[] GetValidTiles()
        {
            bool[] validTiles = new bool[walkableRegion.x * walkableRegion.y];
            for (int y = 0; y < walkableRegion.y; y++)
            {
                for (int x = 0; x < walkableRegion.x; x++)
                {
                    Vector3 testCell = new Vector3(x * 2 + walkableRegionOffset.x, y * 2 + walkableRegionOffset.y, 0f);
                    foreach (Tilemap t in wallTilemaps)
                    {
                        if (t.HasTile(t.WorldToCell(testCell)))
                        {
                            validTiles[y * walkableRegion.x + x] = false;
                            goto LOOPEND;
                        }
                    }

                    validTiles[y * walkableRegion.x + x] = true;
                    LOOPEND: ;
                }
            }

            return validTiles;
        }

    }
}
