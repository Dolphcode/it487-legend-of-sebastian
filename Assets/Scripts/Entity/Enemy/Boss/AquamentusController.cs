using UnityEngine;
using SQZL.World.Interactable;

namespace SQZL.Entity.Enemy
{
    public class AquamentusController : TileEnemyController
    {
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (ControllerIsActive)
            {
                //if (walkTimeLeft <= 0f) base.PickDirection();
                //MoveEntity(moveSpeed, moveDirection.x, moveDirection.y, true);
                //walkTimeLeft -= Time.fixedDeltaTime;
            }
        }

        public override void TryDamage(int amount)
        {
            if (iframes <= 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f)
                {
                    Destroy(gameObject);
                    foreach(RoomTransitionTrigger trigger in gates) base.DisconnectGate(trigger);
                }
            }
        }
    }
}
