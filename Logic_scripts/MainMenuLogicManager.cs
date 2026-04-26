using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLogicManager : MonoBehaviour
{
    public string gameSceneName = "SampleScene"; // Name of your main game scene

    public void PlayGame()
    {
        Debug.Log("Play button clicked. Loading scene: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exit button clicked. Quitting application.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}