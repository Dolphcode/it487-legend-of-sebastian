using System;
using UnityEngine;

namespace SQZL.Entity
{
    public class Hurtbox : MonoBehaviour
    {
        // Fields
        [SerializeField] private string targetTag;
        [SerializeField] private int damageAmount;

        [SerializeField] private bool doKnockback = false;
        // Components
        private Collider2D _hurtboxCollider;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _hurtboxCollider = GetComponent<Collider2D>();
#if DEBUG
            if (!_hurtboxCollider.isTrigger)
            {
                Debug.LogWarning(
                    "WARNING: The attached collider to this hurtbox is not a trigger. This script will not function correctly");
            }
#endif
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            BaseEntityDamageHandler hitbox;
            if (other.gameObject.CompareTag(targetTag) && other.TryGetComponent(out hitbox))
            {
                hitbox.TryDamage(damageAmount, doKnockback, TilebodyDirection.Down);
            }
        }
    }
}
