using System;
using UnityEngine;

namespace SQZL.Entity.Enemy.Hazard
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WallmasterGrabbox : MonoBehaviour
    {
        [SerializeField] private string target;
        
        private WallmasterController _controller;
        
        private void Start()
        {
            if (!transform.parent.TryGetComponent<WallmasterController>(out _controller))
            {
                Debug.LogError("whar? No wallmaster controller as parent of grabbox :(\nMight be in no-Godot (No-Dot?) hell");
            }

            if (!GetComponent<BoxCollider2D>().isTrigger)
            {
                Debug.LogError("didn't set this grab box to be a trigger smdh");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag(target))
            {
                _controller.TriggerGrabPlayer();
            }
        }
    }
}
