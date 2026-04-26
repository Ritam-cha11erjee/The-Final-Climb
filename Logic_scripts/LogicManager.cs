using UnityEngine;
using UnityEngine.UI;
using TMPro; // If you are using TextMeshPro
using UnityEngine.SceneManagement;

public class LogicManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverCanvas;
    public Image blackOverlay;
    public Text deathText;
    public Text deathReason;
    public RectTransform respawnButtonRect;
    public Graphic respawnButtonGraphic;
    public RectTransform exitButtonRect;
    public Graphic exitButtonGraphic;
    public RectTransform exitDesktopButtonRect;
    public Graphic exitDesktopButtonGraphic;

    [Header("Pause Screen References")]
    public GameObject pauseCanvas;
    public Image pauseBlackOverlay;
    public Text pauseText;
    public Image pauseSeparationBar;
    public Text pauseGameName;
    public Button resumeButton;
    public Button exitPauseButton;
    public Button exitDesktopButton;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.3f;
    public float scaleInDuration = 0.2f;
    public LeanTweenType easeType = LeanTweenType.easeOutBack;

    private bool isGameOver = false;
    private bool isPaused = false;
    public static LogicManager instance;
    public static bool isRespawning = false;

    void Awake()
    {
        // Singleton pattern
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);

        if (gameOverCanvas == null)
        {
            Debug.LogError("GameOverCanvas not assigned in LogicManager!");
        }
        else
        {
            gameOverCanvas.SetActive(false);
        }

        if (pauseCanvas == null)
        {
            Debug.LogError("PauseCanvas not assigned in LogicManager!");
        }
        else
        {
            pauseCanvas.SetActive(false);
            SetInitialPauseState();
        }

        SetInitialGameOverState();
    }

    void SetInitialGameOverState()
    {
        // Initialize Game Over UI alphas and scales
        SetAlpha(blackOverlay, 0f);
        SetAlpha(deathText, 0f);
        SetAlpha(deathReason, 0f);
        SetAlpha(respawnButtonGraphic, 0f);
        SetAlpha(exitButtonGraphic, 0f);


        if (deathText != null) deathText.transform.localScale = Vector3.zero;
        if (respawnButtonRect != null) respawnButtonRect.localScale = Vector3.zero;
        if (exitButtonRect != null) exitButtonRect.localScale = Vector3.zero;

    }

    void SetInitialPauseState()
    {
        // Initialize Pause UI alphas and scales
        SetAlpha(pauseBlackOverlay, 0f);
        SetAlpha(pauseText, 0f);
        SetAlpha(pauseSeparationBar, 0f);
        SetAlpha(pauseGameName, 0f);
        SetAlpha(resumeButton.GetComponent<Graphic>(), 0f);
        SetAlpha(exitPauseButton.GetComponent<Graphic>(), 0f);
        SetAlpha(exitDesktopButtonGraphic, 0f);

        if (pauseText != null) pauseText.transform.localScale = Vector3.zero;
        if (pauseGameName != null) pauseGameName.transform.localScale = Vector3.zero;
        if (resumeButton != null) resumeButton.transform.localScale = Vector3.zero;
        if (exitPauseButton != null) exitPauseButton.transform.localScale = Vector3.zero;
        if (exitDesktopButtonRect != null) exitDesktopButtonRect.localScale = Vector3.zero;
    }

    void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        pauseCanvas.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f; // Pause or unpause game time

        if (isPaused)
        {
            // Unlock the cursor when the pause menu appears
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartPauseAnimations(true); // Animate pause screen in
        }
        else
        {
            // Lock the cursor again when the pause menu disappears
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartPauseAnimations(false); // Animate pause screen out
        }
    }

    void StartPauseAnimations(bool appearing)
    {
        float targetAlpha = appearing ? 0.96f : 0f;
        Vector3 targetScale = appearing ? Vector3.one : Vector3.zero;

        // Fade in/out Black Overlay
        if (pauseBlackOverlay != null)
            LeanTween.alpha(pauseBlackOverlay.rectTransform, targetAlpha, fadeInDuration).setIgnoreTimeScale(true);

        // Fade & scale Pause Text
        if (pauseText != null)
        {
            LeanTween.alphaText(pauseText.rectTransform, appearing ? 1f : 0f, fadeInDuration).setIgnoreTimeScale(true);
            LeanTween.scale(pauseText.gameObject, targetScale, scaleInDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true);
        }

        // Fade in/out Separation Bar
        if (pauseSeparationBar != null)
            LeanTween.alpha(pauseSeparationBar.rectTransform, appearing ? 1f : 0f, fadeInDuration).setIgnoreTimeScale(true).setDelay(appearing ? 0.1f : 0f);

        // Fade & scale Game Name
        if (pauseGameName != null)
        {
            LeanTween.alphaText(pauseGameName.rectTransform, appearing ? 1f : 0f, fadeInDuration).setIgnoreTimeScale(true).setDelay(appearing ? 0.2f : 0f);
            LeanTween.scale(pauseGameName.gameObject, targetScale, scaleInDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.2f : 0f);
        }

        // Resume Button scale & fade
        if (resumeButton != null)
        {
            LeanTween.scale(resumeButton.gameObject, targetScale, scaleInDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.4f : 0f);
            LeanTween.alpha(resumeButton.GetComponent<Graphic>().rectTransform, appearing ? 1f : 0f, fadeInDuration)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.4f : 0f);
        }

        // Exit Button scale & fade
        if (exitPauseButton != null)
        {
            LeanTween.scale(exitPauseButton.gameObject, targetScale, scaleInDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.5f : 0f);
            LeanTween.alpha(exitPauseButton.GetComponent<Graphic>().rectTransform, appearing ? 1f : 0f, fadeInDuration)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.5f : 0f);
        }

        // Exit Button scale & fade
        if (exitDesktopButton != null)
        {
            LeanTween.scale(exitDesktopButton.gameObject, targetScale, scaleInDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.5f : 0f);
            LeanTween.alpha(exitDesktopButton.GetComponent<Graphic>().rectTransform, appearing ? 1f : 0f, fadeInDuration)
                     .setIgnoreTimeScale(true)
                     .setDelay(appearing ? 0.5f : 0f);
        }
    }

    public void GameOver(string reason = "Consumed By The Tsunami")
    {
        if (isGameOver) return;
        isGameOver = true;
        isPaused = false; // Ensure pause is off when game over occurs
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f; // Ensure time scale is back to normal for game over screen
        // When game over happens, we likely want the cursor to be visible for interacting with the game over UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hide gameplay UI if any
        Canvas gameplayCanvas = FindObjectOfType<Canvas>();
        if (gameplayCanvas != null) gameplayCanvas.gameObject.SetActive(false);

        // Show game over UI
        gameOverCanvas.SetActive(true);
        SetInitialGameOverState();

        // Set text values
        if (deathText != null) deathText.text = "You Died";
        if (deathReason != null) deathReason.text = reason;

        StartGameOverAnimations();
    }

    void StartGameOverAnimations()
    {
        // Fade in overlay using rectTransform
        if (blackOverlay != null)
            LeanTween.alpha(blackOverlay.rectTransform, 0.96f, fadeInDuration).setIgnoreTimeScale(true);

        // Fade & scale death text
        if (deathText != null)
        {
            LeanTween.alphaText(deathText.rectTransform, 1f, fadeInDuration).setIgnoreTimeScale(true);
            LeanTween.scale(deathText.gameObject, Vector3.one, scaleInDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true);
        }

        // Fade in death reason
        if (deathReason != null)
            LeanTween.alphaText(deathReason.rectTransform, 1f, fadeInDuration)
                     .setDelay(0.2f)
                     .setIgnoreTimeScale(true);

        // Respawn button scale & fade
        if (respawnButtonRect != null && respawnButtonGraphic != null)
        {
            LeanTween.scale(respawnButtonRect.gameObject, Vector3.one, scaleInDuration)
                     .setEase(easeType)
                     .setDelay(0.4f)
                     .setIgnoreTimeScale(true);
            LeanTween.alpha(respawnButtonGraphic.rectTransform, 1f, fadeInDuration)
                     .setDelay(0.4f)
                     .setIgnoreTimeScale(true);
        }

        // Exit button scale & fade
        if (exitButtonRect != null && exitButtonGraphic != null)
        {
            LeanTween.scale(exitButtonRect.gameObject, Vector3.one, scaleInDuration)
                     .setEase(easeType)
                     .setDelay(0.6f)
                     .setIgnoreTimeScale(true);
            LeanTween.alpha(exitButtonGraphic.rectTransform, 1f, fadeInDuration)
                     .setDelay(0.6f)
                     .setIgnoreTimeScale(true);
        }
    }

public void RestartGame()
    {
        isGameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Player Died! Attempting to respawn at checkpoint.");

        // Set the respawning flag before reloading the scene
        isRespawning = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // No need to explicitly call RespawnAtCheckpoint() here anymore
        }
        else
        {
            Debug.LogError("Could not find GameObject with tag 'Player' for respawn!");
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // Reset the flag after the scene has started loading (important for the next load)
        // We can't do this immediately as the new scene hasn't loaded yet.
        // The PlayerCheckpoint's Start() will handle its own logic based on this flag.
    }

    public void GoToMainMenu()
    {
        isGameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        // Relock the cursor when returning to the main menu (if that's the desired behavior)

        SceneManager.LoadScene("MainMenuScene");
    }

    public void ResumeGame()
    {
        TogglePause();
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game called - implement application quit here");
        Application.Quit();
    }
}