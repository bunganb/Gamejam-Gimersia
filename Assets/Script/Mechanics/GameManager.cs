using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Wolf wolf;
    public Transform foodsParent;          // ⭐ ubah nama agar konsisten

    [Header("Visual Effects")]
    public Volume globalVolume;

    [Header("UI References")]
    public GameObject gameOverUI;
    public GameObject winUI;
    public Button NextButton;

    public event Action<Transform> OnFoodEaten;   // event untuk AI

    public int Score { get; private set; }

    private bool gameEnded = false;

    private Coroutine openMapCoroutine;
    private Coroutine howlOfFearCoroutine;
    private float originalVignetteIntensity;
    private float originalVignetteSmoothness;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameManager Instance created!");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (globalVolume != null && globalVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteSmoothness = vignette.smoothness.value;
        }

        if (NextButton != null)
            NextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        Score = 0;
        gameEnded = false;

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (winUI != null) winUI.SetActive(false);

        NewRound();
    }

    private void NewRound()
    {
        foreach (Transform food in foodsParent)
            food.gameObject.SetActive(true);
        ResetState();
    }

    private void ResetState()
    {
        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        foreach (Sheep sheep in allSheep)
            sheep.gameObject.SetActive(true);

        if (wolf != null) wolf.gameObject.SetActive(true);
    }

    private void GameOver()
    {
        if (gameEnded) return;
        gameEnded = true;

        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        foreach (Sheep sheep in allSheep) sheep.gameObject.SetActive(false);
        if (wolf != null) wolf.gameObject.SetActive(false);

        if (gameOverUI != null) gameOverUI.SetActive(true);
        AudioManager.Instance?.PlaySFX("Lose");
        Debug.Log("<color=red>GAME OVER! No food left - Wolf wins!</color>");
    }

    public void FoodEaten(Food food)
    {
        food.gameObject.SetActive(false);
        OnFoodEaten?.Invoke(food.transform);
        if (!HasRemainingFoods())
            GameOver();
    }

    private bool HasRemainingFoods()
    {
        foreach (Transform food in foodsParent)
            if (food.gameObject.activeSelf) return true;
        return false;
    }

    // ==================== WIN HANDLING ====================

    public void SheepEaten(Sheep sheep)
    {
        Score += sheep.points;
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        int livingSheep = 0;
        foreach (Sheep s in allSheep)
            if (s.gameObject.activeSelf && !s.IsDead) livingSheep++;

        Debug.Log($"Living sheep remaining: {livingSheep}");
        if (livingSheep == 0) Win();
    }

    private void Win()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (wolf != null) wolf.gameObject.SetActive(false);
        if (winUI != null) winUI.SetActive(true);
        AudioManager.Instance?.PlaySFX("Win");
        Debug.Log("<color=green>PLAYER WINS! All sheep have been eaten!</color>");
    }

    private void OnNextButtonClicked()
    {
        UnlockNewLevel();
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator ReturnToMainMenu()
    {
        AsyncOperation loadScene = SceneManager.LoadSceneAsync("MainMenu");
        yield return new WaitUntil(() => loadScene.isDone);
        yield return new WaitForEndOfFrame();

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        Transform mainMenu = canvas.transform.Find("MainMenu");
        Transform levelsPanel = canvas.transform.Find("LevelsPanel");

        if (mainMenu != null) mainMenu.gameObject.SetActive(false);
        if (levelsPanel != null) levelsPanel.gameObject.SetActive(true);
    }

    private void UnlockNewLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int reached = PlayerPrefs.GetInt("ReachedIndex", 1);
        if (currentIndex >= reached)
        {
            PlayerPrefs.SetInt("ReachedIndex", currentIndex + 1);
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 1) + 1);
            PlayerPrefs.Save();
            Debug.Log("<color=lime>Level baru terbuka!</color>");
        }
    }

    // ==================== POWER-UPS ====================

    public void ActivateOpenMap(float duration, float targetIntensity, float targetSmoothness)
    {
        if (openMapCoroutine != null) StopCoroutine(openMapCoroutine);
        openMapCoroutine = StartCoroutine(OpenMapEffect(duration, targetIntensity, targetSmoothness));
    }

    private IEnumerator OpenMapEffect(float duration, float targetIntensity, float targetSmoothness)
    {
        if (globalVolume == null || !globalVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
            yield break;

        float stepSize = 0.1f;
        float stepDelay = 0.05f;

        while (vignette.intensity.value > targetIntensity)
        {
            vignette.intensity.value = Mathf.Max(vignette.intensity.value - stepSize, targetIntensity);
            yield return new WaitForSeconds(stepDelay);
        }
        while (vignette.smoothness.value > targetSmoothness)
        {
            vignette.smoothness.value = Mathf.Max(vignette.smoothness.value - stepSize, targetSmoothness);
            yield return new WaitForSeconds(stepDelay);
        }

        vignette.intensity.value = targetIntensity;
        vignette.smoothness.value = targetSmoothness;

        yield return new WaitForSeconds(duration);

        while (vignette.intensity.value < originalVignetteIntensity)
        {
            vignette.intensity.value = Mathf.Min(vignette.intensity.value + stepSize, originalVignetteIntensity);
            yield return new WaitForSeconds(stepDelay);
        }
        while (vignette.smoothness.value < originalVignetteSmoothness)
        {
            vignette.smoothness.value = Mathf.Min(vignette.smoothness.value + stepSize, originalVignetteSmoothness);
            yield return new WaitForSeconds(stepDelay);
        }

        vignette.intensity.value = originalVignetteIntensity;
        vignette.smoothness.value = originalVignetteSmoothness;

        openMapCoroutine = null;
    }

    public void ActivateHowlOfFear(float duration, float speedMultiplier)
    {
        if (howlOfFearCoroutine != null) StopCoroutine(howlOfFearCoroutine);
        howlOfFearCoroutine = StartCoroutine(HowlOfFearEffect(duration, speedMultiplier));
    }

    private IEnumerator HowlOfFearEffect(float duration, float speedMultiplier)
    {
        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        var originalSpeeds = new System.Collections.Generic.Dictionary<Sheep, float>();

        foreach (Sheep sheep in allSheep)
        {
            if (sheep != null && sheep.gameObject.activeSelf)
            {
                SheepAI ai = sheep.GetComponent<SheepAI>();
                if (ai != null)
                {
                    originalSpeeds[sheep] = ai.normalSpeed;
                    ai.normalSpeed *= speedMultiplier;
                }
            }
        }

        yield return new WaitForSeconds(duration);

        foreach (var pair in originalSpeeds)
        {
            if (pair.Key != null)
            {
                SheepAI ai = pair.Key.GetComponent<SheepAI>();
                if (ai != null) ai.normalSpeed = pair.Value;
            }
        }

        howlOfFearCoroutine = null;
    }
}