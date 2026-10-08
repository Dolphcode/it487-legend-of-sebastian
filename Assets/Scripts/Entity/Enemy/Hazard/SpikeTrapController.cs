using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SQZL.Entity.Enemy.Hazard
{
    public class SpikeTrapController : BaseEnemyController, IHazard
    {
        [SerializeField] private List<HazardTripEntry> hazardTripEntries;
        [SerializeField] private float fastTimePerTile = 0.2f;
        [SerializeField] private float slowTimePerTile = 0.4f;
        
        private bool trapActive = false;
        public event Action<IHazard> OnHazardTripped;
        public event Action<IHazard> OnHazardDestroyed;
        void IHazard.TripHazard(HazardTripBox trigger)
        {
            if (trapActive) return;
            
            // Pick the hazard
            Vector3 direction = Vector3.zero;
            int units = 0;
            bool itemFound = false;
            foreach (HazardTripEntry entry in hazardTripEntries)
            {
                if (entry.trigger == trigger)
                {
                    itemFound = true;
                    units = entry.unitsToTravel;
                    direction = entry.direction;
                    break;
                }
            }
            if (!itemFound) return;
            trapActive = true;
            StartCoroutine(TripTrap(direction, units));
        }

        private IEnumerator TripTrap(Vector3 direction, int units)
        {
            // Move step
            float moveSpeed = 1f / fastTimePerTile;
            for (float timeToStop = fastTimePerTile * units; timeToStop > 0f; timeToStop -= Time.fixedDeltaTime)
            {
                transform.position += direction * moveSpeed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            transform.position = new Vector3(Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z);
            
            // Move step
            moveSpeed = 1f / slowTimePerTile;
            for (float timeToStop = slowTimePerTile * units; timeToStop > 0f; timeToStop -= Time.fixedDeltaTime)
            {
                transform.position += -direction * moveSpeed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            transform.position = new Vector3(Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z);

            trapActive = false;
        }

        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            // Spike trap does not damage 
        }
        
        [System.Serializable]
        struct HazardTripEntry
        {
            [SerializeField] internal HazardTripBox trigger;
            [SerializeField] internal Vector3 direction;
            [SerializeField] internal int unitsToTravel;
        }
    }
}
