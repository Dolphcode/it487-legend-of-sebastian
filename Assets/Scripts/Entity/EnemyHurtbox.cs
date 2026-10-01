using UnityEngine;
using Player;

public class EnemyHurtbox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("player entered???");
        if (other.CompareTag("Player"))
        {
            PlayerController ent = other.gameObject.GetComponent<PlayerController>();
            if (ent.playerInputFrozen) return; // iframes
            Health hp = other.gameObject.GetComponent<Health>();
            hp.currentHealth--;
            ent.playerInputFrozen = true;
            StartCoroutine((ent.ForcePlayerCoroutine(6, 0.2f, (PlayerController.PlayerDirection)((ent.facing + 2) % 4), true)));
        }
    }
}
