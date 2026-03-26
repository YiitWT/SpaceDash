using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening; // Make sure you have DOTween imported

[System.Serializable]
public class CharacterSprites
{
    public Sprite normal;
    public Sprite damage;
    public Sprite heal;
}

[System.Serializable]
public class CharacterData
{
    public string characterName;
    public CharacterSprites sprites;
}

public class MainScript : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Selectable firstButton;
    [SerializeField] private Slider slider;
    [SerializeField] private MainMenuAudio mainMenuAudio;
    
    [Header("Character Selection")]
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private Image characterDisplay;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backButton; // Add this button in inspector
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    
    [Header("Animation Settings")]
    [SerializeField] private float panelFadeDuration = 0.5f;
    [SerializeField] private float characterSwitchDuration = 0.3f;
    [SerializeField] private float buttonHoverScale = 1.1f;
    [SerializeField] private float buttonHoverDuration = 0.2f;
    
    private const string SelectedCharacterKey = "selectedCharacter";
    private int currentCharacterIndex = 0;
    private bool isAnimating = false;
    
    // Cache original button scales
    private Vector3 leftArrowOriginalScale;
    private Vector3 rightArrowOriginalScale;
    private Vector3 confirmButtonOriginalScale;

    
    [Header("Loader")]
    public GameObject loader;

    void Start()
    {


        // Initialize DOTween (call this once in your game)
        DOTween.Init();
        
        // Initialize CharacterManager if it doesn't exist
        if (CharacterManager.Instance == null)
        {
            GameObject characterManagerObj = new GameObject("CharacterManager");
            CharacterManager characterManager = characterManagerObj.AddComponent<CharacterManager>();
            characterManager.allCharacters = new List<CharacterData>(characters);
        }
        else
        {
            // Update the CharacterManager's character list
            CharacterManager.Instance.allCharacters = new List<CharacterData>(characters);
        }
        
        // Cache original scales
        leftArrowOriginalScale = leftArrowButton.transform.localScale;
        rightArrowOriginalScale = rightArrowButton.transform.localScale;
        confirmButtonOriginalScale = confirmButton.transform.localScale;
        
        if (mainMenuAudio == null)
        {
            mainMenuAudio = FindFirstObjectByType<MainMenuAudio>();
        }

        // Load saved audio value
        slider.value = AudioPrefs.GetMusicVolume(1f);
        if (mainMenuAudio != null)
        {
            mainMenuAudio.SetMusicVolume(slider.value);
        }

        // Load saved character selection
        if (PlayerPrefs.HasKey(SelectedCharacterKey))
        {
            currentCharacterIndex = PlayerPrefs.GetInt(SelectedCharacterKey);
            if (currentCharacterIndex >= characters.Count)
                currentCharacterIndex = 0;
        }

        // Setup listeners
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        
        // Setup character selection buttons
        leftArrowButton.onClick.AddListener(() => { if (!isAnimating) StartCoroutine(AnimatedPreviousCharacter()); });
        rightArrowButton.onClick.AddListener(() => { if (!isAnimating) StartCoroutine(AnimatedNextCharacter()); });
        confirmButton.onClick.AddListener(() => { if (!isAnimating) StartCoroutine(AnimatedConfirmSelection()); });
        
        if (backButton != null)
            backButton.onClick.AddListener(() => { if (!isAnimating) StartCoroutine(AnimatedBackToMainMenu()); });

        // Setup button hover animations
        SetupButtonHoverAnimations();

        // Start with main menu
        ShowMainMenu();
    }

    private void SetupButtonHoverAnimations()
    {
        // Left arrow button hover
        var leftTrigger = leftArrowButton.gameObject.AddComponent<EventTrigger>();
        var leftPointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        leftPointerEnter.callback.AddListener((data) => {
            leftArrowButton.transform.DOScale(leftArrowOriginalScale * buttonHoverScale, buttonHoverDuration);
        });
        var leftPointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        leftPointerExit.callback.AddListener((data) => {
            leftArrowButton.transform.DOScale(leftArrowOriginalScale, buttonHoverDuration);
        });
        leftTrigger.triggers.Add(leftPointerEnter);
        leftTrigger.triggers.Add(leftPointerExit);

        // Right arrow button hover
        var rightTrigger = rightArrowButton.gameObject.AddComponent<EventTrigger>();
        var rightPointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        rightPointerEnter.callback.AddListener((data) => {
            rightArrowButton.transform.DOScale(rightArrowOriginalScale * buttonHoverScale, buttonHoverDuration);
        });
        var rightPointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        rightPointerExit.callback.AddListener((data) => {
            rightArrowButton.transform.DOScale(rightArrowOriginalScale, buttonHoverDuration);
        });
        rightTrigger.triggers.Add(rightPointerEnter);
        rightTrigger.triggers.Add(rightPointerExit);

        // Confirm button hover
        var confirmTrigger = confirmButton.gameObject.AddComponent<EventTrigger>();
        var confirmPointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        confirmPointerEnter.callback.AddListener((data) => {
            confirmButton.transform.DOScale(confirmButtonOriginalScale * buttonHoverScale, buttonHoverDuration);
        });
        var confirmPointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        confirmPointerExit.callback.AddListener((data) => {
            confirmButton.transform.DOScale(confirmButtonOriginalScale, buttonHoverDuration);
        });
        confirmTrigger.triggers.Add(confirmPointerEnter);
        confirmTrigger.triggers.Add(confirmPointerExit);
    }

    public void StartGame()
    {
        Debug.Log("Opening Character Selection");
        if (!isAnimating)
            StartCoroutine(AnimatedShowCharacterSelection());
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit");
        Application.Quit();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        characterSelectionPanel.SetActive(false);
        
        // Reset panel alphas
        SetPanelAlpha(mainMenuPanel, 1f);
        SetPanelAlpha(characterSelectionPanel, 0f);
        
        eventSystem.SetSelectedGameObject(firstButton.gameObject);
    }

    private IEnumerator AnimatedShowCharacterSelection()
    {
        isAnimating = true;
        
        // Activate character selection panel but make it transparent
        characterSelectionPanel.SetActive(true);
        SetPanelAlpha(characterSelectionPanel, 0f);
        
        // Fade out main menu and fade in character selection
        var fadeOut = FadePanelOut(mainMenuPanel);
        var fadeIn = FadePanelIn(characterSelectionPanel);
        
        yield return new WaitForSeconds(panelFadeDuration * 0.5f);
        
        mainMenuPanel.SetActive(false);
        UpdateCharacterDisplay();
        eventSystem.SetSelectedGameObject(confirmButton.gameObject);
        
        yield return new WaitForSeconds(panelFadeDuration * 0.5f);
        
        isAnimating = false;
    }

    private IEnumerator AnimatedBackToMainMenu()
    {
        isAnimating = true;
        
        // Activate main menu panel but make it transparent
        mainMenuPanel.SetActive(true);
        SetPanelAlpha(mainMenuPanel, 0f);
        
        // Fade out character selection and fade in main menu
        var fadeOut = FadePanelOut(characterSelectionPanel);
        var fadeIn = FadePanelIn(mainMenuPanel);
        
        yield return new WaitForSeconds(panelFadeDuration * 0.5f);
        
        characterSelectionPanel.SetActive(false);
        eventSystem.SetSelectedGameObject(firstButton.gameObject);
        
        yield return new WaitForSeconds(panelFadeDuration * 0.5f);
        
        isAnimating = false;
    }

    private IEnumerator AnimatedPreviousCharacter()
    {
        isAnimating = true;
        
        // Slide out current character
        characterDisplay.transform.DOLocalMoveX(200f, characterSwitchDuration * 0.5f);
        characterNameText.transform.DOLocalMoveX(200f, characterSwitchDuration * 0.5f);
        
        var fadeOut = characterDisplay.DOFade(0f, characterSwitchDuration * 0.5f);
        var textFadeOut = characterNameText.DOFade(0f, characterSwitchDuration * 0.5f);
        
        yield return new WaitForSeconds(characterSwitchDuration * 0.5f);
        
        // Update character
        currentCharacterIndex = (currentCharacterIndex - 1 + characters.Count) % characters.Count;
        UpdateCharacterDisplay();
        
        // Reset position and slide in new character
        characterDisplay.transform.localPosition = new Vector3(-200f, characterDisplay.transform.localPosition.y, 0f);
        characterNameText.transform.localPosition = new Vector3(-200f, characterNameText.transform.localPosition.y, 0f);
        
        characterDisplay.transform.DOLocalMoveX(0f, characterSwitchDuration * 0.5f);
        characterNameText.transform.DOLocalMoveX(0f, characterSwitchDuration * 0.5f);
        
        var fadeIn = characterDisplay.DOFade(1f, characterSwitchDuration * 0.5f);
        var textFadeIn = characterNameText.DOFade(1f, characterSwitchDuration * 0.5f);
        
        yield return new WaitForSeconds(characterSwitchDuration * 0.5f);
        
        isAnimating = false;
    }

    private IEnumerator AnimatedNextCharacter()
    {
        isAnimating = true;
        
        // Slide out current character
        characterDisplay.transform.DOLocalMoveX(-200f, characterSwitchDuration * 0.5f);
        characterNameText.transform.DOLocalMoveX(-200f, characterSwitchDuration * 0.5f);
        
        var fadeOut = characterDisplay.DOFade(0f, characterSwitchDuration * 0.5f);
        var textFadeOut = characterNameText.DOFade(0f, characterSwitchDuration * 0.5f);
        
        yield return new WaitForSeconds(characterSwitchDuration * 0.5f);
        
        // Update character
        currentCharacterIndex = (currentCharacterIndex + 1) % characters.Count;
        UpdateCharacterDisplay();
        
        // Reset position and slide in new character
        characterDisplay.transform.localPosition = new Vector3(200f, characterDisplay.transform.localPosition.y, 0f);
        characterNameText.transform.localPosition = new Vector3(200f, characterNameText.transform.localPosition.y, 0f);
        
        characterDisplay.transform.DOLocalMoveX(0f, characterSwitchDuration * 0.5f);
        characterNameText.transform.DOLocalMoveX(0f, characterSwitchDuration * 0.5f);
        
        var fadeIn = characterDisplay.DOFade(1f, characterSwitchDuration * 0.5f);
        var textFadeIn = characterNameText.DOFade(1f, characterSwitchDuration * 0.5f);
        
        yield return new WaitForSeconds(characterSwitchDuration * 0.5f);
        
        isAnimating = false;
    }

    private void UpdateCharacterDisplay()
    {
        if (characters.Count == 0) 
        {
            Debug.LogWarning("No characters in the list!");
            return;
        }

        if (currentCharacterIndex >= characters.Count)
        {
            Debug.LogWarning($"Character index {currentCharacterIndex} is out of range! Setting to 0.");
            currentCharacterIndex = 0;
        }

        CharacterData currentChar = characters[currentCharacterIndex];
        
        if (currentChar == null)
        {
            Debug.LogError($"Character at index {currentCharacterIndex} is null!");
            return;
        }

        // Update display with null checks
        if (characterDisplay != null && currentChar.sprites != null && currentChar.sprites.normal != null)
        {
            characterDisplay.sprite = currentChar.sprites.normal;
        }
        else
        {
            Debug.LogError("Missing references: " +
                $"characterDisplay={characterDisplay != null}, " +
                $"sprites={currentChar.sprites != null}, " +
                $"normal sprite={currentChar.sprites?.normal != null}");
        }

        if (characterNameText != null && !string.IsNullOrEmpty(currentChar.characterName))
        {
            characterNameText.text = currentChar.characterName;
        }
        else
        {
            Debug.LogError($"Missing character name text reference or character name is empty");
        }
        
        Debug.Log($"Selected Character: {currentChar.characterName}");
    }

    private IEnumerator AnimatedConfirmSelection()
    {
        isAnimating = true;

        // Scale up confirm button for feedback
        confirmButton.transform.DOScale(confirmButtonOriginalScale * 1.2f, 0.1f)
            .OnComplete(() => confirmButton.transform.DOScale(confirmButtonOriginalScale, 0.1f));

        // Save selected character
        PlayerPrefs.SetInt(SelectedCharacterKey, currentCharacterIndex);
        PlayerPrefs.Save();

        Debug.Log($"Character Confirmed: {characters[currentCharacterIndex].characterName}");
        Debug.Log("Starting Game...");

        // Add a brief pause for feedback
        yield return new WaitForSeconds(0.3f);

        // Fade out before loading scene
        var fadeOut = FadePanelOut(characterSelectionPanel);
        loader.GetComponent<TransitionLoader>().AnimateTransition();

        yield return new WaitForSeconds(1);


        // Load game scene
        Debug.Log("Loading Game Scene...");
        SceneManager.LoadScene("GameScene");
    }

    private Tween FadePanelOut(GameObject panel)
    {
        return SetPanelAlpha(panel, 0f, panelFadeDuration);
    }

    private Tween FadePanelIn(GameObject panel)
    {
        return SetPanelAlpha(panel, 1f, panelFadeDuration);
    }

    private Tween SetPanelAlpha(GameObject panel, float alpha, float duration = 0f)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }
        
        if (duration > 0f)
        {
            return canvasGroup.DOFade(alpha, duration);
        }
        else
        {
            canvasGroup.alpha = alpha;
            return null;
        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (mainMenuAudio != null)
        {
            mainMenuAudio.SetMusicVolume(value);
        }
        else
        {
            AudioPrefs.SetMusicVolume(value);
        }
    }

    // Public method to get selected character data (call this from other scripts)
    public static CharacterData GetSelectedCharacterData()
    {
        return CharacterManager.Instance?.GetSelectedCharacter();
    }

    private void OnDestroy()
    {
        // Kill all tweens on this object to prevent errors
        transform.DOKill();
        
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        leftArrowButton.onClick.RemoveAllListeners();
        rightArrowButton.onClick.RemoveAllListeners();
        confirmButton.onClick.RemoveAllListeners();
        
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        // ESC key to go back using new Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !isAnimating)
        {
            if (characterSelectionPanel.activeInHierarchy)
            {
                StartCoroutine(AnimatedBackToMainMenu());
            }
        }

        HandleCharacterSelectionKeyboard();
    }

    private void HandleCharacterSelectionKeyboard()
    {
        if (Keyboard.current == null || isAnimating) return;
        if (!characterSelectionPanel.activeInHierarchy) return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(AnimatedPreviousCharacter());
            return;
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(AnimatedNextCharacter());
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            StartCoroutine(AnimatedConfirmSelection());
        }
    }
}

// CharacterManager for managing character data across scenes
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;
    public List<CharacterData> allCharacters = new List<CharacterData>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public CharacterData GetSelectedCharacter()
    {
        int selectedIndex = PlayerPrefs.GetInt("selectedCharacter", 0);
        if (selectedIndex < allCharacters.Count && selectedIndex >= 0)
            return allCharacters[selectedIndex];
        
        // Fallback to first character if index is invalid
        if (allCharacters.Count > 0)
            return allCharacters[0];
            
        Debug.LogWarning("No characters available in CharacterManager!");
        return null;
    }
}
