using UnityEngine;
using UnityEngine.SceneManagement;

namespace SQZL.Core
{
    public class GameOver : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Continue()
        {
            ScoreTracker.Instance.StartGame();
        }

        public void Quit()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
