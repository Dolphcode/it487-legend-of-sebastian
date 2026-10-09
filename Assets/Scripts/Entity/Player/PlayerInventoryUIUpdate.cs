using TMPro;
using UnityEngine;

namespace SQZL.Entity.Player
{
    public class PlayerInventoryUIUpdate : MonoBehaviour
    {
        public PlayerInventory inv;
        public TextMeshProUGUI rupees;
        public TextMeshProUGUI keys;
        public TextMeshProUGUI bombs;
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
                    break;
                case "bomb":
                    bombs.text =$"{curr}";
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
