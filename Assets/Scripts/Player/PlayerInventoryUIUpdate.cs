using TMPro;
using UnityEngine;

namespace Player
{
    public class PlayerInventoryUIUpdate : MonoBehaviour
    {
        public PlayerInventory inv;
        public TextMeshProUGUI rupees;
        public TextMeshProUGUI keys;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            inv.OnConsumableUpdated += UpdateConsumable;
        }

        private void UpdateConsumable(string name, int curr, int change)
        {
            if (name == "rupee")
            {
                rupees.text = $"x{curr}";
            } else if (name == "key")
            {
                keys.text = $"x{curr}";
            }
        }
        
        // Update is called once per frame
        void Update()
        {

        }
    }
}
