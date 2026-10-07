using UnityEngine;
using SQZL.World.Interactable;

namespace SQZL.Entity.Enemy
{
    public class StalfosController : TileEnemyController
    {
        private Animator _animator;
        private int _a_Damaged;
        
        protected override void Start()
        {
            base.Start();
            _sprite = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _a_Damaged = Animator.StringToHash("Damaged");
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (iframes <= 0f) _animator.SetBool(_a_Damaged, false);
            if (ControllerIsActive)
            {
                if (walkTimeLeft <= 0f) base.PickDirection();
                MoveEntity(moveSpeed, moveDirection.x, moveDirection.y, true);
                walkTimeLeft -= Time.fixedDeltaTime;
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
            _sprite.enabled = true;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _sprite.enabled = false;
        }
    }
}