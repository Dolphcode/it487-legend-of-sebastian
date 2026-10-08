using UnityEngine;

namespace SQZL.Entity.Enemy
{
    public class GelController : TileEnemyController
    {
        [SerializeField] private float waitBetweenSteps = 0.5f;
        private Animator _animator;
        private int _a_Damaged;
        
        protected override void Start()
        {
            base.Start();
            _sprite = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _a_Damaged = Animator.StringToHash("Damaged");

            _sprite.enabled = false;
            ControllerIsActive = false;
        }

        private float waitTime = 0f;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (iframes <= 0f) _animator.SetBool(_a_Damaged, false);
            if (ControllerIsActive)
            {
                if (walkTimeLeft <= 0f)
                {
                    if (waitTime <= 0f)
                    {
                        waitTime = waitBetweenSteps;
                        base.PickDirection(1);
                    }
                    else
                    {
                        transform.position = new Vector3(
                            Mathf.Round(transform.position.x),
                            Mathf.Round(transform.position.y),
                            transform.position.z
                        );
                        waitTime -= Time.fixedDeltaTime;
                    }
                }
                else
                {
                    MoveEntity(moveSpeed, moveDirection.x, moveDirection.y, true);
                    walkTimeLeft -= Time.fixedDeltaTime;
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
                    Destroy(gameObject);
                }
                else
                {
                    iframes = iframeTime;
                    _animator.SetBool(_a_Damaged, true);
                    //if (knockback) StartCoroutine(base.KnockbackCoroutine(attackDir));
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
            ControllerIsActive = false;
            _sprite.enabled = false;
        }

    }
}
