using UnityEngine;

namespace SQZL.Entity.Enemy
{
    public class EntityHeldItemDropper : MonoBehaviour
    {
        [SerializeField] private GameObject dropObject;

        private BaseEnemyController _enemyController;
        private Collider2D objCollider2D;
        private bool objHasCollider;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _enemyController = GetComponent<BaseEnemyController>();
            
            objHasCollider = dropObject.TryGetComponent<Collider2D>(out objCollider2D);
            if (objHasCollider) objCollider2D.enabled = false;
            
            _enemyController.OnEnemyDeath += DropObject;
        }

        private void DropObject(BaseEnemyController deadEnemy)
        {
            if (objHasCollider) objCollider2D.enabled = true;
            
            // No real need for param since we are only registering to the enemy controller this is attached to
            dropObject.transform.SetParent(transform.parent);
        }
    }
}
