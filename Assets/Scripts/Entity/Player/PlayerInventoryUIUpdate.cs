using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SQZL.Entity.Player
{
    public class PlayerInventoryUIUpdate : MonoBehaviour
    {
        public PlayerInventory inv;
        public TextMeshProUGUI rupees;
        public TextMeshProUGUI keys;
        public TextMeshProUGUI bombs;
        public TextMeshProUGUI hammer;
        public Image primary;
        private AudioSource audioSource;
        public AudioClip rupeeSound;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            inv.OnConsumableUpdated += UpdateConsumable;
            audioSource = GetComponent<AudioSource>();
            rupees.text = $"{inv.GetConsumableAmount("rupee")}";
            bombs.text = $"{inv.GetConsumableAmount("bomb")}";
            keys.text = $"{inv.GetConsumableAmount("key")}";
            hammer.text = $"{inv.GetConsumableAmount("hammer")}";
        }

        void Update()
        {
            float frac = inv.DurabilityFracLeft("Sword");
            if (frac <= 0f)
            {
                primary.color = Color.darkRed;
            } else if (frac <= 0.5f)
            {
                primary.color = Color.orangeRed;
            }
            else
            {
                primary.color = Color.white;
            }
        }

        private void UpdateConsumable(string name, int curr, int change)
        {
            switch (name)
            {
                case "rupee":
                    rupees.text = $"{curr}";
                    if (change > 0) audioSource.PlayOneShot(rupeeSound);
                    break;
                case "key":
                    keys.text = $"{curr}";
                    if (change > 0) audioSource.PlayOneShot(rupeeSound);
                    break;
                case "bomb":
                    bombs.text =$"{curr}";
                    if (change > 0) audioSource.PlayOneShot(rupeeSound);
                    break;
                case "hammer":
                    hammer.text = $"{curr}";
                    if (change > 0) audioSource.PlayOneShot(rupeeSound);
                    break;
            }
        }
    }
}
