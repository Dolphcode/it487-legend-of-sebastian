using System;
using Codice.Client.BaseCommands;
using UnityEngine;

namespace SQZL
{
    public class Bomb : MonoBehaviour
    {
        [SerializeField] private int countdown = 3;
        [SerializeField] private GameObject explosionPrefab;

        [Header("Sounds")]
        public AudioClip placeSound;
        private AudioSource audioSource;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Invoke(nameof(Explode), countdown);
            audioSource = GetComponent<AudioSource>();
            audioSource.PlayOneShot(placeSound);
        }

        private void Explode()
        {
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab);
                Debug.Log("BOOOOOM");
            }
            Destroy(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
