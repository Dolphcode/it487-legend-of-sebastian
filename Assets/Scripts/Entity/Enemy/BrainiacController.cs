using UnityEngine;

namespace SQZL.Entity.Enemy
{
    public class BrainiacController : BaseEnemyController
    {
        [Header("State Times")] [SerializeField]
        private float invisibleMinTime;
        [SerializeField] private float invisibleMaxTime;
        [SerializeField] private float circlingTime;
        [SerializeField] private float chargeTime;
        [SerializeField] private float vulnerableTime;

        [Header("Circle Config")] [SerializeField]
        private float circleDist = 3f;
        [SerializeField] private float circlingSpeed;
        [SerializeField] private AnimationCurve circleSpeedOverTime;
        
        [Header("Charge Config")]
        [SerializeField] private float chargeSpeed;
        [SerializeField] private AnimationCurve chargeOverTime;
        
        [Header("Refs")] [SerializeField] private GameObject target;
        [SerializeField] private Collider2D hurtbox;
        private float stateTime;
        private float circleAngle;
        private Vector3 direction;
        private int circleDirection;

        private BrainiacStates currState;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
        {
            base.Start();
            currState = BrainiacStates.INVISIBLE;
            stateTime = UnityEngine.Random.Range(invisibleMinTime, invisibleMaxTime);
        }

        // Update is called once per frame
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (ControllerIsActive)
            {
                stateTime -= Time.fixedDeltaTime;
                iframes -= Time.fixedDeltaTime;
                switch (currState)
                {
                    case BrainiacStates.INVISIBLE:
                        if (stateTime <= 0f)
                        {
                            circleAngle = Vector2.Angle(target.transform.position, transform.position);
                            circleDirection = (UnityEngine.Random.Range(0, 2) == 0) ?  1 : -1;
                            currState = BrainiacStates.CIRCLING;
                            stateTime = circlingTime;
                        }
                        else
                        {
                            _collider2D.enabled = false;
                            _sprite.color = new Color(1f, 1f, 1f, 0f);
                            hurtbox.enabled = false;
                        }
                        break;
                    case BrainiacStates.CIRCLING:
                        if (stateTime <= 0f)
                        {
                            currState = BrainiacStates.CHARGING;
                            stateTime = chargeTime;
                            direction = (target.transform.position - transform.position).normalized;
                        }
                        else
                        {
                            _collider2D.enabled = false;
                            hurtbox.enabled = false;
                            circleAngle += circleSpeedOverTime.Evaluate(1f - Mathf.Clamp(stateTime / circlingTime, 0f, 1f)) * 
                                           circlingSpeed * Time.fixedDeltaTime * (float)circleDirection;
                            Vector3 dir = Quaternion.AngleAxis(circleAngle, Vector3.back) * Vector3.up;
                            transform.position = target.transform.position + dir * circleDist;

                            float T = (circlingTime + 0.25f) / circlingTime;
                            _sprite.color = new Color(1f, 1f, 1f, Mathf.Sin(2f * Mathf.PI * T * (circlingTime - stateTime)));
                        }
                        break;
                    case BrainiacStates.CHARGING:
                        if (stateTime <= 0f)
                        {
                            stateTime = vulnerableTime;
                            currState = BrainiacStates.VULNERABLE;
                        }
                        else
                        {
                            _collider2D.enabled = false;
                            hurtbox.enabled = true;
                            transform.position += direction * chargeSpeed * Time.fixedDeltaTime *
                                                  chargeOverTime.Evaluate(1f - Mathf.Clamp(stateTime/chargeTime, 0f, 1f));
                        }
                        break;
                    case BrainiacStates.VULNERABLE:
                        if (stateTime <= 0f)
                        {
                            currState = BrainiacStates.INVISIBLE;
                            stateTime = UnityEngine.Random.Range(invisibleMinTime, invisibleMaxTime);
                        }
                        else
                        {
                            _collider2D.enabled = true;
                            _sprite.color = new Color(1f, 1f, 1f, Mathf.Clamp(stateTime / vulnerableTime, 0f, 1f));
                        }
                        break;
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
                    base.InvokeEnemyDeath();
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
            currState = BrainiacStates.INVISIBLE;
            stateTime = UnityEngine.Random.Range(invisibleMinTime, invisibleMaxTime);
        }

        internal enum BrainiacStates
        {
            INVISIBLE, CIRCLING, CHARGING, VULNERABLE
        }
    }
}
