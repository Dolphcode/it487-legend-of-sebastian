using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SQZL.Entity.Enemy
{
    public class EnemyRandomDrop : MonoBehaviour
    {
        [Header("Drop Table")]
        [SerializeField][Range(0f, 1f)] private float noDropChance = 0.5f;
        [Tooltip("Weights will be normalized into 1 - dropChance")]
        [SerializeField] private EnemyRandomDropEntry[] dropTable;
        
        private BaseEnemyController _enemyController;
        private Collider2D objCollider2D;
        private bool objHasCollider;

        private float[] dropOddsTable;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Create the drop table
            int i;
            float sum = 0f;
            float remainder = 1f - noDropChance;
            dropOddsTable = new float[dropTable.Length + 1];
            dropOddsTable[i = 0] = noDropChance;
            foreach (EnemyRandomDropEntry entry in dropTable)
                sum += entry.probability;
            for (i = 1; i < dropOddsTable.Length; i++)
            {
                dropOddsTable[i] = dropTable[i - 1].probability / sum * remainder + dropOddsTable[i - 1]; // CDF essentially
            }
            
            _enemyController = GetComponent<BaseEnemyController>();
            _enemyController.OnEnemyDeath += DropObject;
        }

        private void DropObject(BaseEnemyController deadEnemy)
        {
            float drop = Random.Range(0f, 1f);
            Debug.Log($"Drop Value for dead {gameObject.name}: {drop} for drop tabke {dropOddsTable}");
            int dropIndex = -1;
            for (int i = 0; i < dropOddsTable.Length; i++)
            {
                if (drop < dropOddsTable[i])
                {
                    dropIndex = i - 1;
                    break;
                }
            }

            if (dropIndex < 0) return; // Don't drop
            GameObject instance = Instantiate(dropTable[dropIndex].prefab, transform.parent);
            instance.transform.position = transform.position;
        }

        [Serializable]
        struct EnemyRandomDropEntry
        {
            [SerializeField] internal GameObject prefab;
            [SerializeField] internal float probability;
        }
    }
}

