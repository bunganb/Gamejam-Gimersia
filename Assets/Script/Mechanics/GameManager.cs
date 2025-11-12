using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // untuk tombol Cancel

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Wolf wolf;
    public Transform foods;

    [Header("Visual Effects")]
    public Volume globalVolume;

    [Header("UI References")]
    public GameObject gameOverUI;
    public GameObject winUI;
    public Button NextButton; // tombol cancel di UI Win

    private int sheepMultiplier = 1;
    public int score { get; private set; }
    public int lives { get; private set; }

    private bool gameEnded = false;

    // Power-up state
    private Coroutine openMapCoroutine;
    private Coroutine howlOfFearCoroutine;
    private float originalVignetteIntensity;
    private float originalVignetteSmoothness;

    private void Awake()
    {
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

        if (globalVolume != null && globalVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteSmoothness = vignette.smoothness.value;
        }

        // pastikan cancelButton dikaitkan di inspector
        if (NextButton != null)
            NextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        SetScore(0);
        gameEnded = false;

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (winUI != null) winUI.SetActive(false);

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
        Sheep[] allSheep = FindObjectsOfType<Sheep>();
        foreach (Sheep sheep in allSheep)
        {
            sheep.gameObject.SetActive(true);
        }

        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(true);
        }
    }

    private void GameOver()
    {
        if (gameEnded) return;
        gameEnded = true;

        Sheep[] allSheep = FindObjectsOfType<Sheep>();
        foreach (Sheep sheep in allSheep)
        {
            sheep.gameObject.SetActive(false);
        }

        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(false);
        }

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        AudioManager.Instance.PlaySFX("Lose");
        Debug.Log("<color=red>GAME OVER! No food left - Wolf wins!</color>");
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
            GameOver();
        }
    }

    // === WIN HANDLING ===
    void Win()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(false);
        }

        if (winUI != null)
        {
            winUI.SetActive(true);
        }
        AudioManager.Instance.PlaySFX("Win");
        Debug.Log("<color=green>PLAYER WINS! All sheep have been eaten!</color>");
    }

    // dipanggil dari tombol cancel (UI Win)
    private void OnNextButtonClicked()
    {
        Debug.Log("Cancel clicked ? kembali ke MainMenu dan buka Level 2");

        // buka akses level baru
        UnlockNewLevel();

        // muat scene MainMenu
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator ReturnToMainMenu()
    {
        AsyncOperation loadScene = SceneManager.LoadSceneAsync("MainMenu");
        yield return new WaitUntil(() => loadScene.isDone);

        // Tunggu 1 frame agar Canvas dan panel teraktifkan
        yield return new WaitForEndOfFrame();

        // Cari Canvas dulu, baru cari anak-anaknya
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas tidak ditemukan di scene MainMenu!");
            yield break;
        }

        Transform mainMenu = canvas.transform.Find("MainMenu");
        Transform levelsPanel = canvas.transform.Find("LevelsPanel");

        if (mainMenu == null)
            Debug.LogWarning("Panel MainMenu tidak ditemukan di Canvas!");
        if (levelsPanel == null)
            Debug.LogWarning("Panel LevelsPanel tidak ditemukan di Canvas!");

        if (mainMenu != null)
            mainMenu.gameObject.SetActive(false);
        if (levelsPanel != null)
            levelsPanel.gameObject.SetActive(true);

        Debug.Log("<color=yellow>MainMenu loaded - LevelsPanel aktif</color>");
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
    // === END WIN HANDLING ===

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

        Invoke(nameof(CheckWinCondition), 0.1f);
    }

    void CheckWinCondition()
    {
        Sheep[] allSheep = FindObjectsOfType<Sheep>();

        int livingSheep = 0;
        foreach (Sheep s in allSheep)
        {
            // HANYA hitung Sheep yang BUKAN mati dan masih aktif
            if (s.gameObject.activeSelf && !s.IsDead)
            {
                livingSheep++;
            }
        }

        Debug.Log($"Living sheep remaining: {livingSheep}");

        if (livingSheep == 0)
        {
            Win();
        }
    }

    // === Power-ups tetap sama ===
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
            yield break;
        }

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
        if (howlOfFearCoroutine != null)
        {
            StopCoroutine(howlOfFearCoroutine);
        }

        howlOfFearCoroutine = StartCoroutine(HowlOfFearEffect(duration, speedMultiplier));
    }

    private IEnumerator HowlOfFearEffect(float duration, float speedMultiplier)
    {
        Sheep[] allSheep = FindObjectsOfType<Sheep>();
        System.Collections.Generic.Dictionary<Sheep, float> originalSpeeds = new System.Collections.Generic.Dictionary<Sheep, float>();

        foreach (Sheep sheep in allSheep)
        {
            if (sheep != null && sheep.gameObject.activeSelf)
            {
                SheepAI sheepAI = sheep.GetComponent<SheepAI>();
                if (sheepAI != null)
                {
                    originalSpeeds[sheep] = sheepAI.normalSpeed;
                    sheepAI.normalSpeed *= speedMultiplier;
                }
            }
        }

        yield return new WaitForSeconds(duration);

        foreach (var pair in originalSpeeds)
        {
            Sheep sheep = pair.Key;
            float originalSpeed = pair.Value;

            if (sheep != null)
            {
                SheepAI sheepAI = sheep.GetComponent<SheepAI>();
                if (sheepAI != null)
                {
                    sheepAI.normalSpeed = originalSpeed;
                }
            }
        }

        howlOfFearCoroutine = null;
    }
}
