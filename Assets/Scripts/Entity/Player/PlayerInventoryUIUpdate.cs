using TMPro;
using UnityEngine;

namespace SQZL.Entity.Player
{
    public class PlayerInventoryUIUpdate : MonoBehaviour
    {
        public PlayerInventory inv;
        public TextMeshProUGUI rupees;
        public TextMeshProUGUI keys;
        private AudioSource audioSource;
        public AudioClip rupeeSound;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            inv.OnConsumableUpdated += UpdateConsumable;
            audioSource = GetComponent<AudioSource>();
        }

        private void UpdateConsumable(string name, int curr, int change)
        {
            if (name == "rupee")
            {
                rupees.text = $"{curr}";
                Debug.Log("Updated Rupee Value. Change Value: " + change);
                if (change > 0)
                {
                    audioSource.PlayOneShot(rupeeSound);
                }
            }
            else if (name == "key")
            {
                keys.text = $"{curr}";
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
