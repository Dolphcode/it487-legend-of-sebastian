using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.Entity;
using Unity.VisualScripting;

namespace SQZL.Entity.Enemy
{
    public abstract class BaseEnemyController : BaseEntityDamageHandler
    {
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

        protected Tilebody2D _tilebody2D;

        /// <summary>
        /// PLEASE DO NOT OVERRIDE AWAKE. THIS WILL BE USED BY THE BASE ENEMY CONTROLLER TO SET UP BASE ENEMY STUFF
        /// DO YOUR STUFF IN START OR CALL BASE AWAKE PLEASE
        /// </summary>
        protected virtual void Awake()
        {
            currentHP = maxHP;
            _tilebody2D = GetComponent<Tilebody2D>();
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
        public override void TryDamage(int amount)
        {
            if (iframes > 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f) Destroy(gameObject);
            }
        }

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
