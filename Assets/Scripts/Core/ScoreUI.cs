using UnityEngine;
using System;
using TMPro;

namespace SQZL.Core
{
    public class ScoreUI : MonoBehaviour
    {

        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI collText;
        [SerializeField] private TextMeshProUGUI hpText;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ScoreTracker t = ScoreTracker.Instance;
            TimeSpan span = TimeSpan.FromSeconds(t.timePlaying);
            timeText.text = $"Time: {string.Format("{0:D2}:{1:D2}", span.Minutes,span.Seconds)}";

            collText.text = $"Coll: {t.collectibleScore}";
            hpText.text = $"HP Bonus: {t.healthBonus}";

            // TODO Grading mechanic
            
            
        }

        public void OnClickStartGame()
        {
            ScoreTracker.Instance.StartGame();
        }
    }
    
}
