using SQZL.Entity;
using SQZL.World.Interactable;
using SQZL.Entity.Projectile;
using UnityEngine;

namespace SQZL.Entity.Enemy
{
    public class GoriyaController : TileEnemyController
    {
        [Header("Projectile Config")] [SerializeField]
        private GameObject boomerangeProj;

        [SerializeField] private float minTimeToFire;
        [SerializeField] private float maxTimeToFire;
        
        private Animator _animator;

        private int _a_Direction;
        private int _a_Damaged;
        private float timeToNextFire;
        private bool firing = false;
        private BoomerangProjectile activeProjectile = null;
        protected override void Start()
        {
            base.Start();
            _sprite = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            
            _a_Direction = Animator.StringToHash("Direction");
            _a_Damaged = Animator.StringToHash("Damaged");
            
            timeToNextFire = Random.Range(minTimeToFire, maxTimeToFire);
            _sprite.enabled = false;
            ControllerIsActive = false;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (iframes <= 0f) _animator.SetBool(_a_Damaged, false);
            if (ControllerIsActive)
            {
                if (timeToNextFire <= 0f)
                {
                    timeToNextFire = Random.Range(minTimeToFire, maxTimeToFire);
                    firing = true;
                    GameObject projectile = Instantiate(boomerangeProj, transform.position, transform.rotation);
                    activeProjectile = projectile.GetComponent<BoomerangProjectile>();
                    activeProjectile.FireBoomerang(Facing);
                }
                
                if (walkTimeLeft <= 0f) base.PickDirection();
                
                if (!firing)
                {
                    MoveEntity(moveSpeed, moveDirection.x, moveDirection.y, true);
                    _animator.SetInteger(_a_Direction, (int)Facing);
                    walkTimeLeft -= Time.fixedDeltaTime;
                    timeToNextFire -= Time.fixedDeltaTime;
                }
                else
                {
                    if ((activeProjectile is null) || !activeProjectile.IsAlive)
                    {
                        firing = false;
                    }   
                }
            }
        }

        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            if (iframes <= 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f)
                {
                    base.DisconnectSpawnManager();
                    InvokeEnemyDeath();
                    Destroy(gameObject);
                }
                else
                {
                    iframes = iframeTime;
                    _animator.SetBool(_a_Damaged, true);
                    if (knockback) StartCoroutine(base.KnockbackCoroutine(attackDir));
                }
            }
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            ControllerIsActive = true;
            _sprite.enabled = true;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _sprite.enabled = false;
            ControllerIsActive = false;
            _animator.SetBool(_a_Damaged, false);
        }
    }
}
