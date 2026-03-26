using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class playerController : MonoBehaviour
{
    public static playerController Instance;

    [Header("Movement Settings")]
    public float maxSpeed = 8f;
    public float acceleration = 25f;
    public float deceleration = 15f;
    public float leftBoundary = -8f;
    public float rightBoundary = 8f;
    
    [Header("Sprite Settings")]
    public Sprite idleSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Player Head")]
    public Image playerHead; 
    
    // These will be loaded from selected character
    [Header("Character Sprites (Loaded Automatically)")]
    [SerializeField] private Sprite playerHeadImage; 
    [SerializeField] private Sprite playerHeadDamage;
    [SerializeField] private Sprite playerHeadHeal;
  
    private SpriteRenderer spriteRenderer;
    private float currentVelocity = 0f;
    private float horizontalInput;
    private MovementState currentState = MovementState.Idle;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private int invulnerableCount = 0;
    private AudioManager audioManager;
    private static CinemachineImpulseSource shakeSource;

    public enum MovementState
    {
        Idle,
        MovingLeft,
        MovingRight
    }
    
    void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        shakeSource = GetComponent<CinemachineImpulseSource>();

        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("AudioManager not found in the scene!");
        }
        
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
        }
    }
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("SimplePlayerController requires a SpriteRenderer component!");
        }
        
        if (idleSprite != null)
            spriteRenderer.sprite = idleSprite;
        
        // Load selected character sprites
        LoadSelectedCharacterSprites();
            
        Debug.Log("SimplePlayerController started!");
    }
    
    void LoadSelectedCharacterSprites()
    {
        // Try to get selected character from CharacterManager
        CharacterData selectedCharacter = null;
        
        if (CharacterManager.Instance != null)
        {
            selectedCharacter = CharacterManager.Instance.GetSelectedCharacter();
        }
        else
        {
            // Fallback: try to get from MainScript static method
            selectedCharacter = MainScript.GetSelectedCharacterData();
        }
        
        if (selectedCharacter != null && selectedCharacter.sprites != null)
        {
            // Load character sprites
            playerHeadImage = selectedCharacter.sprites.normal;
            playerHeadDamage = selectedCharacter.sprites.damage;
            playerHeadHeal = selectedCharacter.sprites.heal;
            
            // Apply the normal sprite to player head immediately
            if (playerHead != null && playerHeadImage != null)
            {
                playerHead.sprite = playerHeadImage;
                playerHead.color = Color.white;
            }
            
            Debug.Log($"Loaded character sprites for: {selectedCharacter.characterName}");
        }
        else
        {
            Debug.LogWarning("Could not load selected character sprites. Using default sprites.");
            
            // If no character data available, keep the sprites assigned in inspector
            if (playerHead != null && playerHeadImage != null)
            {
                playerHead.sprite = playerHeadImage;
                playerHead.color = Color.white;
            }
        }
    }
    
    void Update()
    {
        HandleInput();
        HandleMovement();
        UpdateSprite();
    }
    
    void HandleInput()
    {
        if (moveAction != null)
        {
            Vector2 moveValue = moveAction.ReadValue<Vector2>();
            horizontalInput = moveValue.x;
        }
        else
        {
            horizontalInput = Input.GetAxis("Horizontal");
        }
    }
    
    void HandleMovement()
    {
        float targetVelocity = horizontalInput * maxSpeed;
        
        if (Mathf.Abs(targetVelocity) > 0.1f)
        {
            currentVelocity = Mathf.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            currentVelocity = Mathf.MoveTowards(currentVelocity, 0f, deceleration * Time.deltaTime);
        }
        
        Vector3 newPosition = transform.position + Vector3.right * currentVelocity * Time.deltaTime;
        
        if (newPosition.x < leftBoundary)
        {
            newPosition.x = leftBoundary;
            currentVelocity = 0f;
        }
        else if (newPosition.x > rightBoundary)
        {
            newPosition.x = rightBoundary;
            currentVelocity = 0f;
        }
        
        transform.position = newPosition;
        UpdateMovementState();
    }
    
    void UpdateMovementState()
    {
        MovementState newState;
        
        if (Mathf.Abs(currentVelocity) < 0.5f)
        {
            newState = MovementState.Idle;
        }
        else if (currentVelocity < 0)
        {
            newState = MovementState.MovingLeft;
        }
        else
        {
            newState = MovementState.MovingRight;
        }
        
        currentState = newState;
    }
    
    void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        switch (currentState)
        {
            case MovementState.Idle:
                if (idleSprite != null)
                    spriteRenderer.sprite = idleSprite;
                break;
                
            case MovementState.MovingLeft:
                if (leftSprite != null)
                    spriteRenderer.sprite = leftSprite;
                break;
                
            case MovementState.MovingRight:
                if (rightSprite != null)
                    spriteRenderer.sprite = rightSprite;
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle") && !IsInvulnerable)
        {
            if (audioManager != null)
            {
                audioManager.PlaySFX(audioManager.damage);
            }

            if (GameManager.Instance != null)
            {
                shakeSource?.GenerateImpulse();
                GameManager.Instance.LoseLife(false);
                Time.timeScale = 0.5f;

                StartCoroutine(DamageInvulnerability());
                
                // Use loaded character damage sprite
                if (playerHead != null)
                {
                    playerHead.sprite = playerHeadDamage != null ? playerHeadDamage : playerHeadImage;
                    playerHead.color = Color.red;
                }
                
                StartCoroutine(ResetPlayerHeadAfterDelay(0.75f));
            }
            else
            {
                Debug.LogError("GameManager.Instance is null!");
            }
        }
        else if(other.CompareTag("Heal"))
        {
        
            if (GameManager.Instance != null)
            {
                Time.timeScale = 2f;
                if (audioManager != null)
                {
                    audioManager.PlaySFX(audioManager.heal);
                }

                GameManager.Instance.LoseLife(true);

                StartCoroutine(HealInvulnerability(3f));

                // Use loaded character heal sprite
                if (playerHead != null)
                {
                    playerHead.sprite = playerHeadHeal != null ? playerHeadHeal : playerHeadImage;
                    playerHead.color = Color.green;
                }

                StartCoroutine(ResetPlayerHeadAfterDelay(3f));
            }
            else
            {
                Debug.LogError("GameManager.Instance is null!");
            }
        }
    }

    private System.Collections.IEnumerator ResetPlayerHeadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Reset to normal character sprite
        if (playerHead != null)
        {
            playerHead.sprite = playerHeadImage;
            playerHead.color = Color.white;
        }
        
        Time.timeScale = 1f;
    }
    
    private bool IsInvulnerable
    {
        get { return invulnerableCount > 0; }
    }

    private System.Collections.IEnumerator DamageInvulnerability()
    {
        invulnerableCount++;
        
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            
            for (int i = 0; i < 10; i++) 
            {
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }

        invulnerableCount = Mathf.Max(0, invulnerableCount - 1);
    }

    private System.Collections.IEnumerator HealInvulnerability(float duration)
    {
        invulnerableCount++;
        yield return new WaitForSeconds(duration);
        invulnerableCount = Mathf.Max(0, invulnerableCount - 1);
    }
    
    // Method to manually reload character sprites (call this if character changes mid-game)
    public void ReloadCharacterSprites()
    {
        LoadSelectedCharacterSprites();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveValue = context.ReadValue<Vector2>();
        horizontalInput = moveValue.x;
    }
    
    void OnEnable()
    {
        if (moveAction != null)
            moveAction.Enable();
    }
    
    void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }
}
