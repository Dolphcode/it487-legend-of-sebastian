using TMPro;
using UnityEngine;

namespace SQZL.Entity.Player
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
                rupees.text = $"{curr}";
            } else if (name == "key")
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
