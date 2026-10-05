using UnityEngine;

namespace SQZL.Entity
{
    public abstract class BaseEntityDamageHandler : MonoBehaviour
    {
        /// <summary>
        /// This function is called by a hurtbox on an intersected target hitbox. This class must be extended to implement
        /// damage handling or a health system.
        /// </summary>
        /// <param name="amount">The amount of true damage to be dealt</param>
        public abstract void TryDamage(int amount);
    }
}
