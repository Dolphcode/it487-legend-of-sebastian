using System;
using UnityEngine;
using Player;
using SQZL.Entity;
using SQZL.Entity.Player;

[Obsolete("Simply use the Hurtbox generic class from now on")]
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
            PlayerEntityController ent = other.gameObject.GetComponent<PlayerEntityController>();
            PlayerHealthManager hp = other.gameObject.GetComponent<PlayerHealthManager>();
            hp.TryDamage(1);
        }
    }
}
