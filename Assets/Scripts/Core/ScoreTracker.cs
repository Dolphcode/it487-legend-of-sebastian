using UnityEngine;
using UnityEngine.SceneManagement;

namespace SQZL.Core
{
    public class ScoreTracker : MonoBehaviour
    {
        public static ScoreTracker Instance { private set; get; } = null;
        
        public float timePlaying;
        public int collectibleScore = 0;
        public int healthBonus = 0;
        
        public bool playing = true;

        [Header("Scene Management")] [SerializeField]
        private int gameScene;

        [SerializeField] private int winScene;
        [SerializeField] private int loseScene;
        
        void Awake()
        {
            if (Instance is null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Reset();
        }

        // Update is called once per frame
        void Update()
        {
            if (!playing) return;
            timePlaying += Time.deltaTime;
        }

        public void Reset()
        {
            timePlaying = 0f;
            collectibleScore = 0;
            healthBonus = 0;
        }

        public void EndGame()
        {
            playing = false;
            SceneManager.LoadScene(winScene);
        }

        public void StartGame()
        {
            SceneManager.LoadScene(gameScene);
            Reset();
            playing = true;
        }
    }
}
