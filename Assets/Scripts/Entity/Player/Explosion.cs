using UnityEngine;

namespace SQZL.Entity.Player
{
    public class Explosion : MonoBehaviour
    {
        private float explosionDuration;
        public AudioClip boomSound;
        
        //Components
        private Animator animator;
        private AudioSource audioSource;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //Get Components
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();


            explosionDuration = animator.GetCurrentAnimatorClipInfo(0).Length;
            audioSource.PlayOneShot(boomSound);
            Destroy(gameObject, explosionDuration);
        }

        private void OEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                BaseEntityDamageHandler ent = other.gameObject.GetComponent<BaseEntityDamageHandler>();
                ent.TryDamage(4, false, TilebodyDirection.Down);
            }
        }
    }
}
