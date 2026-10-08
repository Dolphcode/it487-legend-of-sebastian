using System;
using System.Collections.Generic;
using UnityEngine;

namespace SQZL.Entity.Enemy.Hazard
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class HazardTripBox : MonoBehaviour
    {
        [SerializeField] private string targetTag;
        
        [SerializeField] private List<BaseEnemyController> potentialHazards;

        private List<IHazard> boundHazards = new();
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            foreach (BaseEnemyController tracked in potentialHazards)
            {
                IHazard h = tracked as IHazard;
                if (!(h is null))
                {
                    h.OnHazardDestroyed += OnBoundHazardDestroyed;
                    boundHazards.Add(h);
                    Debug.Log("Tracking a hazard");
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("whoa");
            if (other.gameObject.CompareTag(targetTag))
            {
                foreach (IHazard tracked in boundHazards)
                {
                    tracked.TripHazard(this);
                }
            }
        }

        private void OnBoundHazardDestroyed(IHazard hazard)
        {
            if (!(hazard is null) && boundHazards.Contains(hazard))
            {
                hazard.OnHazardDestroyed -= OnBoundHazardDestroyed;
                boundHazards.Remove(hazard);
            }
        }
    }
}
