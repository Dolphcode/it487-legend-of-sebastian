using UnityEngine;
using UnityEngine.Tilemaps;

namespace Player
{
    public class Arrow : MonoBehaviour
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
            if (other.GetComponent<TilemapCollider2D>() != null || other.CompareTag("Wall"))
            {
                Destroy(gameObject);
            }
            else if (other.CompareTag("Enemy"))
            {
                Destroy(gameObject);
                //add logic for hurting enemy here? idk lol
            }
        }
    }
}