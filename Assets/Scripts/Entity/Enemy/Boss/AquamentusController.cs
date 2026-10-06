using UnityEngine;
using SQZL.World.Interactable;
using SQZL.Entity.VFX;

namespace SQZL.Entity.Enemy
{
    public class AquamentusController : TileEnemyController
    {
        [Header("Aquamentus Walking Config")] [SerializeField] private int unitsToWalk = 6;
        [SerializeField] private float walkTime = 2f;
        [SerializeField] private int startUnit = 3;
        [SerializeField] private bool startLeft;

        [Header("Aquamentus Attack Config")] [SerializeField]
        private GameObject projectilePrefab;

        [SerializeField] private GameObject spawnPos;

        [SerializeField] private GameObject target;

        [SerializeField] private float rechargeTime = 6f;

        private Animator _animator;
        
        private float walkTimeLeft;
        private float rechargeTimeLeft;
        private int dX;

        private int _a_Damaged;

        protected override void Start()
        {
            base.Start();
            
            // Setup
            walkTimeLeft = ((float)startUnit / (float)unitsToWalk) * walkTime;
            dX = (startLeft) ? -1 : 1;
            moveSpeed = (unitsToWalk / 2) / walkTime;
            rechargeTimeLeft = rechargeTime;
            
            // References
            _sprite = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            
            // get the animator
            _a_Damaged = Animator.StringToHash("Damaged");
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (iframes <= 0f) _animator.SetBool(_a_Damaged, false);
            
            if (ControllerIsActive)
            {
                // Flip direction
                if (walkTimeLeft <= 0f)
                {
                    dX = (dX == -1) ? 1 : -1;
                    walkTimeLeft = walkTime;
                }
                
                // Fire projectile
                if (rechargeTimeLeft <= 0f)
                {
                    SpawnProjectiles();
                    rechargeTimeLeft = rechargeTime;
                }
                
                // Move entity
                MoveEntity(moveSpeed, dX, 0, true);
                
                walkTimeLeft -= Time.fixedDeltaTime;
                rechargeTimeLeft -= Time.fixedDeltaTime;
            }
        }

        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            if (iframes <= 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f)
                {
                    Destroy(gameObject);
                    foreach(RoomTransitionTrigger trigger in gates) base.DisconnectGate(trigger);
                }
                else
                {
                    _animator.SetBool(_a_Damaged, true);
                    iframes = iframeTime;
                }
            }
        }

        private void SpawnProjectiles()
        {
            Vector3 commonDir = (target.transform.position - spawnPos.transform.position);
            commonDir.Normalize();
            for (int i = -1; i <= 1; i++)
            {
                GameObject proj = Instantiate(projectilePrefab, spawnPos.transform.position, Quaternion.identity);
                AquamentusFireball fireballComp = proj.GetComponent<AquamentusFireball>();
                fireballComp._separationDirection = Vector3.up * i;
                fireballComp._travelDirection = commonDir;
            }
        }

        public override void OnSpawn()
        {
            ControllerIsActive = true;
            _sprite.enabled = true;
            // TODO RAISE GATES/BLOCKERS?
        }

        public override void OnDespawn()
        {
            ControllerIsActive = false;
            _sprite.enabled = false;
            _animator.SetBool(_a_Damaged, false);
        }
    }
}
