using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public Sheep[] sheep;
    public Wolf wolf;
    public Transform foods;
    
    [Header("Visual Effects")]
    public Volume globalVolume;
    
    private int sheepMultiplier = 1;
    public int score { get; private set; }
    public int lives { get; private set; }
    
    // Power-up state
    private Coroutine openMapCoroutine;
    private Coroutine howlOfFearCoroutine;
    private float originalVignetteIntensity;
    private float originalVignetteSmoothness;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager Instance created!");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Store original vignette settings
        if (globalVolume != null && globalVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteSmoothness = vignette.smoothness.value;
            Debug.Log($"Original Vignette - Intensity: {originalVignetteIntensity}, Smoothness: {originalVignetteSmoothness}");
        }
    }
    
    private void Start()
    {
        NewGame();
    }
    
    private void NewGame()
    {
        SetScore(0);
        NewRound();
    }
    
    private void NewRound()
    {
        foreach (Transform food in this.foods)
        {
            food.gameObject.SetActive(true);
        }
        ResetState();
    }
    
    private void ResetState()
    {
        for (int i = 0; i < this.sheep.Length; i++)
        {
            this.sheep[i].gameObject.SetActive(true);
        }
        
        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(true);
        }
    }
    
    private void GameOver()
    {
        for (int i = 0; i < this.sheep.Length; i++)
        {
            this.sheep[i].gameObject.SetActive(false);
        }
        
        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(false);
        }
    }
    
    private void SetScore(int score)
    {
        this.score = score;
    }
    
    public void FoodEaten(Food food)
    {
        food.gameObject.SetActive(false);
        
        if (!HasRemainingFoods())
        {
            if (wolf != null)
            {
                wolf.gameObject.SetActive(false);
            }
            Invoke(nameof(NewRound), 3f);
        }
    }
    
    void CheckWinCondition()
    {
        int activeFoods = 0;
        foreach (Transform f in foods)
        {
            if (f.gameObject.activeSelf)
                activeFoods++;
        }
        if (activeFoods == 0) SheepWin();
    }
    
    void SheepWin()
    {
        Debug.Log("Sheep win!");
    }
    
    private bool HasRemainingFoods()
    {
        foreach (Transform food in foods)
        {
            if (food.gameObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }
    
    public void SheepEaten(Sheep sheep)
    {
        int points = sheep.points * sheepMultiplier;
        SetScore(score + points);
        Debug.Log($"<color=red>Sheep eaten! +{points} points. Total: {score}</color>");
    }

    public void ActivateOpenMap(float duration, float targetIntensity, float targetSmoothness)
    {
        if (openMapCoroutine != null)
        {
            StopCoroutine(openMapCoroutine);
        }
        
        openMapCoroutine = StartCoroutine(OpenMapEffect(duration, targetIntensity, targetSmoothness));
    }
    
    private IEnumerator OpenMapEffect(float duration, float targetIntensity, float targetSmoothness)
    {
        if (globalVolume == null || !globalVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
        {
            Debug.LogWarning("Global Volume or Vignette not found!");
            yield break;
        }
        
        float stepSize = 0.1f; // Decrease by 0.1 each step
        float stepDelay = 0.05f; // Time between each step (20 steps per second)
        
        // Gradually decrease intensity
        while (vignette.intensity.value > targetIntensity)
        {
            vignette.intensity.value = Mathf.Max(vignette.intensity.value - stepSize, targetIntensity);
            yield return new WaitForSeconds(stepDelay);
        }
        
        // Gradually decrease smoothness
        while (vignette.smoothness.value > targetSmoothness)
        {
            vignette.smoothness.value = Mathf.Max(vignette.smoothness.value - stepSize, targetSmoothness);
            yield return new WaitForSeconds(stepDelay);
        }
        
        // Ensure values are set to target
        vignette.intensity.value = targetIntensity;
        vignette.smoothness.value = targetSmoothness;
        
        Debug.Log($"<color=cyan>Map opened! Vignette reduced for {duration} seconds</color>");
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Gradually increase intensity back to original
        while (vignette.intensity.value < originalVignetteIntensity)
        {
            vignette.intensity.value = Mathf.Min(vignette.intensity.value + stepSize, originalVignetteIntensity);
            yield return new WaitForSeconds(stepDelay);
        }
        
        // Gradually increase smoothness back to original
        while (vignette.smoothness.value < originalVignetteSmoothness)
        {
            vignette.smoothness.value = Mathf.Min(vignette.smoothness.value + stepSize, originalVignetteSmoothness);
            yield return new WaitForSeconds(stepDelay);
        }
        
        // Restore original values
        vignette.intensity.value = originalVignetteIntensity;
        vignette.smoothness.value = originalVignetteSmoothness;
        
        Debug.Log("<color=cyan>Map closed - vignette restored</color>");
        
        openMapCoroutine = null;
    }

    public void ActivateHowlOfFear(float duration, float speedMultiplier)
    {
        // Stop previous Howl of Fear effect if active
        if (howlOfFearCoroutine != null)
        {
            StopCoroutine(howlOfFearCoroutine);
        }
        
        howlOfFearCoroutine = StartCoroutine(HowlOfFearEffect(duration, speedMultiplier));
    }
    
    private IEnumerator HowlOfFearEffect(float duration, float speedMultiplier)
    {
        // Store original speeds
        float[] originalSpeeds = new float[sheep.Length];
        
        // Apply fear effect to all sheep
        for (int i = 0; i < sheep.Length; i++)
        {
            if (sheep[i] != null && sheep[i].gameObject.activeSelf)
            {
                SheepAI sheepAI = sheep[i].GetComponent<SheepAI>();
                if (sheepAI != null)
                {
                    originalSpeeds[i] = sheepAI.normalSpeed;
                    sheepAI.normalSpeed *= speedMultiplier;
                    
                    // Update movement speed if in eating state
                    if (sheep[i].movement != null)
                    {
                        sheep[i].movement.speedMultiplier *= speedMultiplier;
                    }
                }
            }
        }
        
        Debug.Log($"<color=orange>Howl of Fear active! All sheep slowed to {speedMultiplier * 100}% speed</color>");
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Restore original speeds
        for (int i = 0; i < sheep.Length; i++)
        {
            if (sheep[i] != null)
            {
                SheepAI sheepAI = sheep[i].GetComponent<SheepAI>();
                if (sheepAI != null)
                {
                    sheepAI.normalSpeed = originalSpeeds[i];
                    
                    // Restore movement speed if in eating state
                    if (sheep[i].movement != null && sheep[i].gameObject.activeSelf)
                    {
                        sheep[i].movement.speedMultiplier = originalSpeeds[i];
                    }
                }
            }
        }
        
        Debug.Log("<color=orange>Howl of Fear ended - sheep speed restored</color>");
        
        howlOfFearCoroutine = null;
    }
}