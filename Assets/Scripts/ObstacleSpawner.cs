using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] obstaclePrefabs;
    public float spawnRate = 2f;
    public float spawnY = 10f;
    public float minX = -7f;
    public float maxX = 7f;
    
    [Header("Heal Spawning")]
    public GameObject healPrefab;
    public float minHealInterval = 10f;
    public float maxHealInterval = 20f;
    
    [Header("Difficulty")]
    public bool increaseDifficulty = true;
    public float difficultyIncreaseRate = 0.1f;
    public float maxSpawnRate = 0.5f;
    
    [Header("Score-Based Spawning")]
    public int scoreThreshold1 = 100; // When tier 2 obstacles (4-6) become available
    public int scoreThreshold2 = 300; // When tier 3 obstacles (7-9) become available
    public float tier2SpawnChance = 0.5f; // 50% chance for tier 2
    public float tier3SpawnChance = 0.25f; // 25% chance for tier 3
    
    [Header("Time-Based Difficulty Events")]
    public float easyPhaseLength = 30f; // 30 seconds of easy gameplay
    public float mediumPhaseLength = 60f; // Next 30 seconds (total 60s) medium difficulty
    public float insanePhaseStart = 90f; // After 1.5 minutes, insane mode
    
    [Header("Multi-Spawn Events")]
    public float doubleSpawnChance = 0f; // Starts at 0, increases over time
    public float tripleSpawnChance = 0f; // For very late game
    public float horizontalSpacing = 2f; // Space between simultaneous spawns
    
    [Header("Wave Events")]
    public float waveEventChance = 0f; // Chance for wave events
    public int waveSize = 3; // Number of obstacles in a wave
    public float waveSpacing = 1.5f; // Time between wave obstacles
    
    private float nextSpawnTime;
    private float nextHealSpawnTime;
    private float currentSpawnRate;
    private int currentScore = 0;
    private float gameStartTime;
    private bool isWaveActive = false;
    private Coroutine waveCoroutine;
    
    // Difficulty scaling variables
    private float timeDifficultyMultiplier = 1f;
    private float currentTier2Chance;
    private float currentTier3Chance;
    
    void Start()
    {
        currentSpawnRate = spawnRate;
        nextSpawnTime = Time.time + currentSpawnRate;
        gameStartTime = Time.time;
        
        // Initialize heal spawn timing
        ScheduleNextHealSpawn();
        
        // Initialize tier chances
        currentTier2Chance = tier2SpawnChance;
        currentTier3Chance = tier3SpawnChance;
        
        // Get initial score from GameManager if available
        if (GameManager.Instance != null)
        {
            currentScore = GameManager.Instance.GetScore();
        }
    }
    
    void Update()
    {
        UpdateTimeDifficulty();
        
        // Handle heal spawning (independent of obstacle spawning)
        if (healPrefab != null && Time.time >= nextHealSpawnTime)
        {
            SpawnHeal();
            ScheduleNextHealSpawn();
        }
        
        if (Time.time >= nextSpawnTime && !isWaveActive)
        {
            // Check for special events first
            if (ShouldTriggerWaveEvent())
            {
                TriggerWaveEvent();
            }
            else if (ShouldSpawnMultiple())
            {
                SpawnMultipleObstacles();
            }
            else
            {
                SpawnObstacle();
            }
            
            nextSpawnTime = Time.time + currentSpawnRate;
            
            if (increaseDifficulty)
            {
                currentSpawnRate = Mathf.Max(maxSpawnRate, currentSpawnRate - difficultyIncreaseRate * Time.deltaTime * timeDifficultyMultiplier);
            }
        }
    }
    
    void ScheduleNextHealSpawn()
    {
        float healInterval = Random.Range(minHealInterval, maxHealInterval);
        nextHealSpawnTime = Time.time + healInterval;
    }
    
    void SpawnHeal()
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(minX, maxX),
            spawnY,
            0f
        );
        
        Instantiate(healPrefab, spawnPos, Quaternion.identity);
    }
    
    void UpdateTimeDifficulty()
    {
        float elapsedTime = Time.time - gameStartTime;
        
        if (elapsedTime < easyPhaseLength)
        {
            // Easy phase (0-30s): Base difficulty
            timeDifficultyMultiplier = 1f;
            doubleSpawnChance = 0f;
            tripleSpawnChance = 0f;
            waveEventChance = 0f;
            currentTier2Chance = tier2SpawnChance;
            currentTier3Chance = tier3SpawnChance;
        }
        else if (elapsedTime < mediumPhaseLength)
        {
            // Medium phase (30-60s): Moderate increases
            float mediumProgress = (elapsedTime - easyPhaseLength) / (mediumPhaseLength - easyPhaseLength);
            timeDifficultyMultiplier = 1f + mediumProgress * 0.5f;
            doubleSpawnChance = mediumProgress * 0.15f; // Up to 15% chance
            tripleSpawnChance = 0f;
            waveEventChance = mediumProgress * 0.08f; // Up to 8% chance
            currentTier2Chance = tier2SpawnChance + mediumProgress * 0.2f; // Increase tier 2 spawns
            currentTier3Chance = tier3SpawnChance;
        }
        else if (elapsedTime < insanePhaseStart)
        {
            // Hard phase (60-90s): Significant increases
            float hardProgress = (elapsedTime - mediumPhaseLength) / (insanePhaseStart - mediumPhaseLength);
            timeDifficultyMultiplier = 1.5f + hardProgress * 0.8f;
            doubleSpawnChance = 0.15f + hardProgress * 0.15f; // 15-30% chance
            tripleSpawnChance = hardProgress * 0.1f; // Up to 10% chance
            waveEventChance = 0.08f + hardProgress * 0.07f; // 8-15% chance
            currentTier2Chance = tier2SpawnChance + 0.2f + hardProgress * 0.15f;
            currentTier3Chance = tier3SpawnChance + hardProgress * 0.2f;
        }
        else
        {
            // Insane phase (90s+): Maximum difficulty
            float insaneProgress = Mathf.Min((elapsedTime - insanePhaseStart) / 30f, 1f); // Cap at 30s of insane scaling
            timeDifficultyMultiplier = 2.3f + insaneProgress * 1.2f; // Up to 3.5x
            doubleSpawnChance = 0.3f + insaneProgress * 0.25f; // 30-55% chance
            tripleSpawnChance = 0.1f + insaneProgress * 0.2f; // 10-30% chance
            waveEventChance = 0.15f + insaneProgress * 0.15f; // 15-30% chance
            currentTier2Chance = Mathf.Min(tier2SpawnChance + 0.35f + insaneProgress * 0.2f, 0.9f);
            currentTier3Chance = Mathf.Min(tier3SpawnChance + 0.2f + insaneProgress * 0.3f, 0.7f);
        }
    }
    
    bool ShouldTriggerWaveEvent()
    {
        return Random.Range(0f, 1f) <= waveEventChance;
    }
    
    bool ShouldSpawnMultiple()
    {
        float totalMultiSpawnChance = doubleSpawnChance + tripleSpawnChance;
        return Random.Range(0f, 1f) <= totalMultiSpawnChance;
    }
    
    void TriggerWaveEvent()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
        waveCoroutine = StartCoroutine(SpawnWave());
    }
    
    IEnumerator SpawnWave()
    {
        isWaveActive = true;
        
        for (int i = 0; i < waveSize; i++)
        {
            SpawnObstacle();
            if (i < waveSize - 1) // Don't wait after the last obstacle
            {
                yield return new WaitForSeconds(waveSpacing);
            }
        }
        
        isWaveActive = false;
    }
    
    void SpawnMultipleObstacles()
    {
        int spawnCount = 2; // Default to double spawn
        
        // Determine if triple spawn
        if (Random.Range(0f, doubleSpawnChance + tripleSpawnChance) > doubleSpawnChance)
        {
            spawnCount = 3;
        }
        
        // Calculate spawn positions
        float totalWidth = (spawnCount - 1) * horizontalSpacing;
        float halfWidth = totalWidth / 2f;
        float minCenter = minX + halfWidth;
        float maxCenter = maxX - halfWidth;
        float centerX = minCenter > maxCenter
            ? (minX + maxX) * 0.5f
            : Random.Range(minCenter, maxCenter);
        
        for (int i = 0; i < spawnCount; i++)
        {
            float xPos = centerX - halfWidth + (i * horizontalSpacing);
            // Clamp to spawn boundaries
            xPos = Mathf.Clamp(xPos, minX, maxX);
            
            Vector3 spawnPos = new Vector3(xPos, spawnY, 0f);
            Instantiate(ChooseObstacleBasedOnScore(), spawnPos, Quaternion.identity);
        }
    }
    
    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0) return;
        
        GameObject obstaclePrefab = ChooseObstacleBasedOnScore();
        
        Vector3 spawnPos = new Vector3(
            Random.Range(minX, maxX),
            spawnY,
            0f
        );
        
        Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
    }
    
    GameObject ChooseObstacleBasedOnScore()
    {
        // Determine which tiers are available based on score
        bool tier2Available = currentScore >= scoreThreshold1;
        bool tier3Available = currentScore >= scoreThreshold2;
        
        // Generate random value for tier selection
        float randomValue = Random.Range(0f, 1f);
        
        // Use time-modified chances instead of base chances
        // Tier 3 (obstacles 6-8, indices 6-8) - highest priority if available
        if (tier3Available && randomValue <= currentTier3Chance && obstaclePrefabs.Length > 6)
        {
            int maxTier3Index = Mathf.Min(8, obstaclePrefabs.Length - 1);
            return obstaclePrefabs[Random.Range(6, maxTier3Index + 1)];
        }
        
        // Tier 2 (obstacles 3-5, indices 3-5)
        if (tier2Available && randomValue <= currentTier2Chance && obstaclePrefabs.Length > 3)
        {
            int maxTier2Index = Mathf.Min(5, obstaclePrefabs.Length - 1);
            return obstaclePrefabs[Random.Range(3, maxTier2Index + 1)];
        }
        
        // Tier 1 (obstacles 0-2, indices 0-2) - default/fallback
        int maxTier1Index = Mathf.Min(2, obstaclePrefabs.Length - 1);
        return obstaclePrefabs[Random.Range(0, maxTier1Index + 1)];
    }
    
    public void UpdateScore(int newScore)
    {
        currentScore = newScore;
    }
    
    public void SetSpawnRate(float newRate)
    {
        currentSpawnRate = newRate;
    }
    
    public void ResetDifficulty()
    {
        currentSpawnRate = spawnRate;
        currentScore = 0;
        gameStartTime = Time.time;
        timeDifficultyMultiplier = 1f;
        doubleSpawnChance = 0f;
        tripleSpawnChance = 0f;
        waveEventChance = 0f;
        currentTier2Chance = tier2SpawnChance;
        currentTier3Chance = tier3SpawnChance;
        
        // Reset heal spawning
        ScheduleNextHealSpawn();
        
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            isWaveActive = false;
        }
    }
    
    // Debug methods
    public string GetCurrentSpawningTier()
    {
        if (currentScore >= scoreThreshold2)
            return "Tier 1, 2, and 3 available";
        else if (currentScore >= scoreThreshold1)
            return "Tier 1 and 2 available";
        else
            return "Only Tier 1 available";
    }
    
    public string GetCurrentDifficultyPhase()
    {
        float elapsedTime = Time.time - gameStartTime;
        
        if (elapsedTime < easyPhaseLength)
            return $"Easy Phase ({elapsedTime:F1}s/{easyPhaseLength}s)";
        else if (elapsedTime < mediumPhaseLength)
            return $"Medium Phase ({elapsedTime:F1}s/{mediumPhaseLength}s)";
        else if (elapsedTime < insanePhaseStart)
            return $"Hard Phase ({elapsedTime:F1}s/{insanePhaseStart}s)";
        else
            return $"INSANE MODE ({elapsedTime:F1}s)";
    }
    
    public string GetCurrentEventChances()
    {
        return $"Double: {doubleSpawnChance:P1}, Triple: {tripleSpawnChance:P1}, Wave: {waveEventChance:P1}";
    }
    
    // Heal debug info
    public float GetTimeUntilNextHeal()
    {
        return Mathf.Max(0f, nextHealSpawnTime - Time.time);
    }
}
