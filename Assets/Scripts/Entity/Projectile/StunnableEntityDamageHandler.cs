using System.Collections;
using UnityEngine;

namespace SQZL.Entity.Projectile
{
    public abstract class StunnableEntityDamageHandler : BaseEntityDamageHandler
    {
        [Header("Stun Config")] [SerializeField]
        protected float stunTime = 2f;

        [SerializeField] private bool stunnable = true;
        
        protected IEnumerator activeStunCoroutine;
        protected bool stunned = false;
        
        public void TryStun()
        {
            Debug.Log($"Trying to stun {gameObject.name}");
            if (stunnable)
            {
                activeStunCoroutine = StunCoroutine();
                StartCoroutine(activeStunCoroutine);
            }
        }

        protected IEnumerator StunCoroutine()
        {
            stunned = true;
            yield return new WaitForSeconds(stunTime);
            stunned = false;
            activeStunCoroutine = null;
        } 
    }
}