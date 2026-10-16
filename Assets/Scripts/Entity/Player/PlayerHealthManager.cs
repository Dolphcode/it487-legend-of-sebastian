using SQZL.World;
using UnityEngine;
using UnityEngine.UI;

namespace SQZL.Entity.Player
{
    public class PlayerHealthManager : BaseEntityDamageHandler
    {
        //Going with ints instead of floats for the sake of using a switch statement to handle the UI.
        //This means that half a heart = 1 health and 1 full heart = 2 health. Keep this in mind when doing damage values.
        public int maxHealth;
        public int currentHealth;
        [SerializeField] public AudioClip hurtSound;
        private AudioSource audioSource;
        [SerializeField] private PlayerEntityController pcScript;

        [Header("Heart UI")] [SerializeField] Image Heart1;
        [SerializeField] Image Heart2;
        [SerializeField] Image Heart3;

        [Header("Heart Sprites")] [SerializeField]
        Sprite emptyHeart;

        [SerializeField] Sprite halfHeart;
        [SerializeField] Sprite fullHeart;

        private Animator _animator;
        private int _a_Damaged;
        private int _a_Dead;
        
        void Start()
        {
            _animator = GetComponent<Animator>();
            _a_Damaged = Animator.StringToHash("Damaged");
            _a_Dead = Animator.StringToHash("Dead");
            
            //Planning ahead since max health can be increased later via heart containers,
            //so it's best to create a max health and current health variable.
            maxHealth = 6;
            currentHealth = maxHealth;
            audioSource = GetComponent<AudioSource>();
        }

        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            if (pcScript.IFrames <= 0f)
            {
                currentHealth -= amount;
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

                if (currentHealth == 0)
                {
                    pcScript.playerInputFrozen = true;
                    RoomManager._Instance.UnloadRoom();
                    _animator.SetTrigger(_a_Dead);
                    return;
                }
                _animator.SetBool(_a_Damaged, true);
                audioSource.PlayOneShot(hurtSound);

                pcScript.TriggerIframes();
                if (knockback) pcScript.StartCoroutine(pcScript.KnockbackCoroutine());
            }
        }

        void Update()
        {
            if (pcScript.IFrames <= 0f)
            {
                _animator.SetBool(_a_Damaged, false);
            }
            //Updates the Heart UI to reflect the current health.
            //May need to be updated down the line to account for increases in max health.
            switch (currentHealth)
            {
                case 0:
                    Heart1.sprite = emptyHeart;
                    Heart2.sprite = emptyHeart;
                    Heart3.sprite = emptyHeart;
                    //pcScript.GameOver();
                    break;
                case 1:
                    Heart1.sprite = halfHeart;
                    Heart2.sprite = emptyHeart;
                    Heart3.sprite = emptyHeart;
                    break;
                case 2:
                    Heart1.sprite = fullHeart;
                    Heart2.sprite = emptyHeart;
                    Heart3.sprite = emptyHeart;
                    break;
                case 3:
                    Heart1.sprite = fullHeart;
                    Heart2.sprite = halfHeart;
                    Heart3.sprite = emptyHeart;
                    break;
                case 4:
                    Heart1.sprite = fullHeart;
                    Heart2.sprite = fullHeart;
                    Heart3.sprite = emptyHeart;
                    break;
                case 5:
                    Heart1.sprite = fullHeart;
                    Heart2.sprite = fullHeart;
                    Heart3.sprite = halfHeart;
                    break;
                case 6:
                    Heart1.sprite = fullHeart;
                    Heart2.sprite = fullHeart;
                    Heart3.sprite = fullHeart;
                    break;
            }
        }

        public void Heal(int amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        /// <summary>
        /// TODO IMPLEMENT, JUST MAXES OUT HEALTH RN
        /// The function itself should be simple but we need to adapt the UI
        /// </summary>
        /// <param name="amount">amount to add</param>
        public void AddMaxHealth(int amount)
        {
            // maxHealth += amount;
            currentHealth = maxHealth;
        }
    }
}
