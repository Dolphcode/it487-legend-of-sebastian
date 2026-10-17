using System;
using UnityEngine;
using SQZL.Entity.Player;
using UnityEngine.SocialPlatforms.Impl;

namespace SQZL.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class WinBox : MonoBehaviour
    {

        [SerializeField] private PlayerEntityController playerRef;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                ScoreTracker.Instance.healthBonus = playerRef.GetComponent<PlayerHealthManager>().currentHealth;
                ScoreTracker.Instance.EndGame();
            } 
        }
    }
}
