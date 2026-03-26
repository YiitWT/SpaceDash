using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int maxLives = 3;
    public int scorePerSecond;

    [Header("UI - Assign These!")]
    public TextMeshProUGUI scoreDisplay;
    public TextMeshProUGUI highScoreDisplay;
    public Image[] heartImages;

    [Header("Save Settings")]
    public bool saveHighScore = true;
    public bool saveLastScore = true;


    private int currentScore = 0;
    private int currentLives = 3;
    private bool gameRunning = true;
    private float scoreTimer = 0f;
    private int highScore = 0;


    private const string HIGH_SCORE_KEY = "HighScore";
    private const string LAST_SCORE_KEY = "LastScore";


    public static GameManager Instance;

    [Header("Loader")]
    public GameObject loader;


    private ObstacleSpawner obstacleSpawner;
    private AudioManager audioManager;
    private TransitionLoader transitionLoader;
    private Animator loaderAnimator;

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    

    void Start()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();

        Time.timeScale = 1f;

        if (loader != null)
        {
            transitionLoader = loader.GetComponent<TransitionLoader>();
            transitionLoader?.AnimateTransition();
            loaderAnimator = loader.GetComponent<Animator>();
        }

        QualitySettings.vSyncCount = 0; // Disable VSync
        Application.targetFrameRate = 120;

        scorePerSecond = Random.Range(2, 8);
        currentLives = maxLives;
        currentScore = 0;
        gameRunning = true;


        LoadGameData();


        obstacleSpawner = ObstacleSpawner.FindFirstObjectByType<ObstacleSpawner>();


        UpdateScore();
        UpdateHearts();
        UpdateHighScoreDisplay();

    }

    void Update()
    {
        if (!gameRunning) return;

        scoreTimer += Time.deltaTime;
        if (scoreTimer >= 1)
        {
            currentScore += scorePerSecond;
            UpdateScore();


            if (obstacleSpawner != null)
            {
                obstacleSpawner.UpdateScore(currentScore);
            }

            scoreTimer = 0f;
        }
    }

    void LoadGameData()
    {

        if (saveHighScore)
        {
            highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        }



    }

    void SaveGameData()
    {

        if (saveLastScore)
        {
            PlayerPrefs.SetInt(LAST_SCORE_KEY, currentScore);
        }


        if (saveHighScore && currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            Debug.Log($"New High Score: {highScore}!");
        }


        PlayerPrefs.Save();
        Debug.Log($"Game data saved! Score: {currentScore}, High Score: {highScore}");
    }

    void UpdateScore()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.text = "Score: " + currentScore;
        }
        else
        {
            Debug.LogWarning("Score display is not assigned!");
        }


        if (currentScore > highScore)
        {
            UpdateHighScoreDisplay();
        }
    }

    void UpdateHighScoreDisplay()
    {
        if (highScoreDisplay != null)
        {
            int displayHighScore = Mathf.Max(highScore, currentScore);
            highScoreDisplay.text = "Best: " + displayHighScore;
        }
    }

    void UpdateHearts()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogWarning("Heart images not assigned!");
            return;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                bool showHeart = i < currentLives;
                heartImages[i].gameObject.SetActive(showHeart);
            }
        }
    }

    public void LoseLife(bool heal)
    {
        if (!gameRunning) return;

        if (heal)
        {
            if (currentLives >= maxLives) currentLives = maxLives;
            else currentLives++;
        }
        else currentLives--;

        UpdateHearts();

        if (currentLives <= 0)
        {
            if (audioManager != null)
            {
                audioManager.StopMusic();
                audioManager.PlaySFX(audioManager.gameOver);
            }

            if (playerController.Instance != null)
            {
                playerController.Instance.StopAllCoroutines();
            }
            GameOver();
        }
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScore();


        if (obstacleSpawner != null)
        {
            obstacleSpawner.UpdateScore(currentScore);
        }

    }

    void GameOver()
    {
        gameRunning = false;
        Time.timeScale = 1f;
        if (playerController.Instance != null)
        {
            playerController.Destroy(playerController.Instance.gameObject);
        }

        SaveGameData();
        StartCoroutine(GameOverTransition());
    }

    private IEnumerator GameOverTransition()
    {
        if (loaderAnimator != null)
        {
            loaderAnimator.Play("SceneTransStart");
        }
        
        // Wait for the transition animation to complete
        // Adjust this duration to match your animation length
        yield return new WaitForSeconds(2f);
        
        SceneManager.LoadScene("DeathScreen");
    }

    void ExitGame()
    {
        Debug.Log("Exiting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    public int GetSavedHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public int GetSavedLastScore()
    {
        return PlayerPrefs.GetInt(LAST_SCORE_KEY, 0);
    }

    public void ResetSavedData()
    {
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
        PlayerPrefs.DeleteKey(LAST_SCORE_KEY);
        PlayerPrefs.Save();
        highScore = 0;
        UpdateHighScoreDisplay();
        Debug.Log("All saved data reset!");
    }


    public void SaveCurrentProgress()
    {
        SaveGameData();
    }


    public int GetScore() { return currentScore; }
    public int GetLives() { return currentLives; }
    public bool IsGameRunning() { return gameRunning; }
    public int GetHighScore() { return highScore; }


    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGameData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveGameData();
        }
    }
    
}
