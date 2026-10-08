using System;
using System.Collections.Generic;
using UnityEngine;
using SQZL.Entity.Enemy;

namespace SQZL.World.RoomClears
{
    public class RoomEntityTracker : MonoBehaviour
    {
        public event Action OnRoomClearedEvent;

        [SerializeField] private List<BaseEnemyController> enemies;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            foreach (BaseEnemyController enemy in enemies)
            {
                enemy.OnEnemyDeath += OnEnemyKilled;
            }
        }

        private void OnEnemyKilled(BaseEnemyController e)
        {
            enemies.Remove(e);
            if (enemies.Count <= 0)
            {
                OnRoomClearedEvent?.Invoke();
            }
        }
    }
}
