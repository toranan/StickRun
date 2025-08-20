using UnityEngine;
using UnityEngine.SceneManagement; // Required for SceneManager

namespace BananaRun.Runner
{
    public class StartGameButton : MonoBehaviour
    {
        // This method will be called when the UI Button is clicked
        public void LoadRunnerSampleScene()
        {
            // Load the scene by its name
            SceneManager.LoadScene("RunnerSample");
            Debug.Log("Loading RunnerSample scene...");
        }
    }
}
