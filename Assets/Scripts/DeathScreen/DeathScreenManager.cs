using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreenManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI yourScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI shutdownText;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip titleDropSFX;
    public AudioClip scoreDropSFX;
    public AudioClip highScoreDropSFX;
    public AudioClip shutdownDropSFX;
    public AudioClip scoreCountSFX;
    public AudioClip scoreCompleteSFX;
    
    [Header("Animation Settings")]
    public float dropHeight = 500f;
    public float dropDuration = 0.8f;
    public float bounceHeight = 30f;
    public float bounceDuration = 0.3f;
    public float delayBetweenTexts = 0.3f;
    
    [Header("Score Settings")]
    public int finalScore = 0;
    public float scoreCountDuration = 0.8f;
    
    private Vector3[] originalPositions;
    private Vector3 originalScoreScale; // Store original scale
    private int displayedScore = 0;
    private Coroutine scorePulseCoroutine;
    
    [Header("Loader")]
    public GameObject loader;

    void Start()
    {
        // Store original positions and scale
        originalPositions = new Vector3[4];
        originalPositions[0] = titleText.transform.localPosition;
        originalPositions[1] = yourScoreText.transform.localPosition;
        originalPositions[2] = highScoreText.transform.localPosition;
        originalPositions[3] = shutdownText.transform.localPosition;

        if (loader != null)
        {
            TransitionLoader transition = loader.GetComponent<TransitionLoader>();
            if (transition != null)
            {
                transition.AnimateTransition();
            }
        }


        // Store the original scale of the score text
        originalScoreScale = yourScoreText.transform.localScale;
        
        // Hide all texts initially by moving them above the screen
        HideAllTexts();
        
        ShowGameOverPanel(PlayerPrefs.GetInt("LastScore", 0), PlayerPrefs.GetInt("HighScore", 0));
    }
    
    private void HideAllTexts()
    {
        // Hide all texts completely initially
        titleText.gameObject.SetActive(false);
        yourScoreText.gameObject.SetActive(false);
        highScoreText.gameObject.SetActive(false);
        shutdownText.gameObject.SetActive(false);
    }
    
    public void ShowGameOverPanel(int score, int highScore)
    {
        finalScore = score;
        
        // Set high score text (while it's still hidden)
        highScoreText.text = $"High Score: {highScore}";
        
        // Reset score display to 0 for counting animation
        displayedScore = 0;
        yourScoreText.text = "Your Score: 0";
        
        StartCoroutine(GameOverSequence());
    }
    
    private IEnumerator GameOverSequence()
    {
        // Animate texts falling down one by one
        yield return StartCoroutine(AnimateTextDrop(titleText, titleDropSFX, 0));
        yield return new WaitForSeconds(delayBetweenTexts);
        
        yield return StartCoroutine(AnimateTextDrop(yourScoreText, scoreDropSFX, 1));
        yield return new WaitForSeconds(delayBetweenTexts);
        
        // Start score counting animation AFTER the score text has dropped
        yield return StartCoroutine(AnimateScoreCount());
        
        yield return StartCoroutine(AnimateTextDrop(highScoreText, highScoreDropSFX, 2));
        yield return new WaitForSeconds(delayBetweenTexts);
        
        yield return StartCoroutine(AnimateTextDrop(shutdownText, shutdownDropSFX, 3));
        
        // Start countdown
        StartCoroutine(ShutdownCountdown());
    }
    
    private IEnumerator AnimateTextDrop(TextMeshProUGUI text, AudioClip sfx, int positionIndex)
    {
        // Activate the text and position it above screen
        text.gameObject.SetActive(true);
        Vector3 startPos = originalPositions[positionIndex] + Vector3.up * dropHeight;
        Vector3 targetPos = originalPositions[positionIndex];
        Vector3 bouncePos = targetPos + Vector3.up * bounceHeight;
        
        // Set the text at the start position (above screen)
        text.transform.localPosition = startPos;
        
        // Drop animation with easing
        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dropDuration;
            
            // Ease out cubic for natural fall
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            text.transform.localPosition = Vector3.Lerp(startPos, bouncePos, t);
            yield return null;
        }
        
        // Play drop sound
        if (audioSource && sfx)
        {
            audioSource.PlayOneShot(sfx);
        }
        
        // Bounce effect
        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            
            // Ease out bounce
            t = 1f - Mathf.Pow(1f - t, 2f);
            
            text.transform.localPosition = Vector3.Lerp(bouncePos, targetPos, t);
            yield return null;
        }
        
        text.transform.localPosition = targetPos;
        
        // Scale punch effect
        StartCoroutine(ScalePunch(text.transform));
    }
    
    private IEnumerator ScalePunch(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 punchScale = originalScale * 1.2f;
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        // Scale up
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = (elapsed * 2f) / duration;
            target.localScale = Vector3.Lerp(originalScale, punchScale, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = (elapsed * 2f) / duration;
            target.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }
        
        target.localScale = originalScale;
    }
    
    private IEnumerator AnimateScoreCount()
    {
        float elapsed = 0f;
        int startScore = 0;
        
        while (elapsed < scoreCountDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scoreCountDuration;
            
            // Ease out for satisfying feel
            t = 1f - Mathf.Pow(1f - t, 2f);
            
            int currentScore = Mathf.RoundToInt(Mathf.Lerp(startScore, finalScore, t));
            
            if (currentScore != displayedScore)
            {
                displayedScore = currentScore;
                yourScoreText.text = $"Your Score: {displayedScore}";
                
                // Play tick sound with pitch ramping
                if (audioSource && scoreCountSFX && displayedScore < finalScore)
                {
                    // Pitch ramps from 0.8 to 1.5 as we get closer to the end
                    float pitchProgress = (float)displayedScore / finalScore;
                    float pitch = Mathf.Lerp(0.8f, 1.5f, pitchProgress);
                    audioSource.pitch = pitch;
                    audioSource.PlayOneShot(scoreCountSFX, 0.3f);
                }
                
                // Small scale effect during counting
                if (scorePulseCoroutine != null)
                {
                    StopCoroutine(scorePulseCoroutine);
                }
                scorePulseCoroutine = StartCoroutine(SmallScalePulse(yourScoreText.transform));
            }
            
            yield return null; // Wait for next frame instead of fixed time
        }
        
        // STOP any remaining pulse coroutines and reset scale
        if (scorePulseCoroutine != null)
        {
            StopCoroutine(scorePulseCoroutine);
            scorePulseCoroutine = null;
        }
        
        // Force reset the scale to original before any final effects
        yourScoreText.transform.localScale = originalScoreScale;
        
        // Reset pitch to normal
        if (audioSource)
        {
            audioSource.pitch = 1f;
        }
        
        // Ensure final score is set
        displayedScore = finalScore;
        yourScoreText.text = $"Your Score: {displayedScore}";
        
        // Play completion sound
        if (audioSource && scoreCompleteSFX)
        {
            audioSource.PlayOneShot(scoreCompleteSFX);
        }
        
        // Final celebration effect (using the original scale as base)
        StartCoroutine(ScalePunchWithOriginalScale(yourScoreText.transform));
    }
    
    private IEnumerator SmallScalePulse(Transform target)
    {
        Vector3 pulseScale = originalScoreScale * 1.05f;
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        // Quick pulse
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (t < 0.5f)
                target.localScale = Vector3.Lerp(originalScoreScale, pulseScale, t * 2f);
            else
                target.localScale = Vector3.Lerp(pulseScale, originalScoreScale, (t - 0.5f) * 2f);
                
            yield return null;
        }
        
        target.localScale = originalScoreScale;
    }
    
    // New method specifically for the final punch effect that uses original scale
    private IEnumerator ScalePunchWithOriginalScale(Transform target)
    {
        Vector3 punchScale = originalScoreScale * 1.2f;
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        // Scale up
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = (elapsed * 2f) / duration;
            target.localScale = Vector3.Lerp(originalScoreScale, punchScale, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = (elapsed * 2f) / duration;
            target.localScale = Vector3.Lerp(punchScale, originalScoreScale, t);
            yield return null;
        }
        
        target.localScale = originalScoreScale;
    }
    
    private IEnumerator ShutdownCountdown()
    {
        for (int i = 5; i > 0; i--)
        {
            shutdownText.text = $"Game will shutdown in {i} seconds";
            
            // Color flash effect
            StartCoroutine(ColorFlash(shutdownText));
            
            yield return new WaitForSeconds(1f);
        }
        
        shutdownText.text = "Shutting down...";
        
        // Add your shutdown logic here
        // Application.Quit(); // Uncomment for actual shutdown
    }
    
    private IEnumerator ColorFlash(TextMeshProUGUI text)
    {
        Color originalColor = text.color;
        Color flashColor = Color.red;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (t < 0.5f)
                text.color = Color.Lerp(originalColor, flashColor, t * 2f);
            else
                text.color = Color.Lerp(flashColor, originalColor, (t - 0.5f) * 2f);
                
            yield return null;
        }
        
        text.color = originalColor;
    }
}
