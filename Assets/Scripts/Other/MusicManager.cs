using System;
using NUnit.Framework;
using SQZL.Entity.Player;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private AudioSource audioSource;
    public PlayerHealthManager healthScript;
    private bool hasStoppedPermanently = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
        audioSource.loop = true;
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
        Debug.Log("Music stopped");
    }
}
