using SQZL.Entity.Player;
using UnityEngine;

namespace SQZL.Core
{
    public class MusicManager : MonoBehaviour
    {
        private AudioSource audioSource;
        public PlayerHealthManager healthScript;

        private bool hasStoppedPermanently = false;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.Play();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (healthScript.currentHealth == 0 && !hasStoppedPermanently)
            {
                StopMusicPermanently();
            }
        }

        private void StopMusicPermanently()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            hasStoppedPermanently = true;
        }

        public void StopMusic()
        {
            audioSource.Stop();
        }
        
        public void SetMusic(AudioClip clip)
        {
            audioSource.clip = clip;
            if (hasStoppedPermanently) return;
            audioSource.Play();
        }
    }
}
