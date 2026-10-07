using SQZL.Entity;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SQZL.Entity.Player
{
    public class PlayerArrow : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifeTime = 5f;

        private Vector2 moveDirection;
        
        public void Initialize(Vector2 direction)
        {
            moveDirection = direction.normalized;
            Destroy(gameObject, lifeTime); //Destroy automatically just in case
        }

        private void Update()
        {
            //Move arrow forward
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Wall"))
            {
                Destroy(gameObject);
            }
            else if (other.CompareTag("Enemy"))
            {
                // I added it twin :>
                //   ||
                //  \ /
                BaseEntityDamageHandler ent = other.gameObject.GetComponent<BaseEntityDamageHandler>();
                ent.TryDamage(/*new Vector2Int(Mathf.RoundToInt(moveDirection.x), Mathf.RoundToInt(moveDirection.y)), */1, false, TilebodyDirection.Down);
                
                Destroy(gameObject);
                //add logic for hurting enemy here? idk lol
            }
        }
    }
}