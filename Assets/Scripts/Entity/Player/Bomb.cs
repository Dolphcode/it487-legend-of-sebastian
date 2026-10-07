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
        public AudioClip boomSound;
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
            audioSource.PlayOneShot(boomSound);
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab);
                Debug.Log("BOOOOOM");
                audioSource.PlayOneShot(boomSound);
            }
            Destroy(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
