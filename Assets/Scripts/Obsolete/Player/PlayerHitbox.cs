using System;
using UnityEngine;

namespace Player
{
    [Obsolete("See SQZL.Entity.Player for new implementation of Player")]
    public class PlayerHitbox : MonoBehaviour
    {
        private PlayerController player;

        private void Start()
        {
            player = GetComponentInParent<PlayerController>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                BaseEntityController ent = other.gameObject.GetComponent<BaseEntityController>();
                ent.OnHit(player.GetFacingDirection(), 1);
            }
        }
    }
}
