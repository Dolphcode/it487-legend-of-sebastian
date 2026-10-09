using System.Collections;
using SQZL.Entity.Player;
using TMPro;
using UnityEngine;

namespace SQZL.World.Interactable
{
    public class WorldTextDisplayer : MonoBehaviour
    {
        [SerializeField] private RoomTransitionTrigger displayToggleTrigger;
        [SerializeField] private TextMeshProUGUI textDisplay;
        [SerializeField] private float timePerChar = 0.1f;
        [SerializeField] private PlayerEntityController playerRef;
        
        private string textUnfiltered;
        private void Start()
        {
            textUnfiltered = textDisplay.text;
            displayToggleTrigger.PostTransition += TriggerText;
        }

        private void TriggerText()
        {
            playerRef.playerInputFrozen = true;
            StartCoroutine(EnumerateText());
        }

        private IEnumerator EnumerateText()
        {
            for (int i = 0; i <= textUnfiltered.Length; i++)
            {
                string pre = textUnfiltered.Substring(0, i);
                string post = textUnfiltered.Substring(i, textUnfiltered.Length - i);
                textDisplay.text = $"<color=white>{pre}</color>{post}";
                yield return new WaitForSeconds(timePerChar);
            }

            playerRef.playerInputFrozen = false;
        }
    }
}