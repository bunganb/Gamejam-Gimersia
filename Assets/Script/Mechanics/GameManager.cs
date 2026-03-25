using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Wolf wolf;
    public Transform foods;          // Parent dari semua makanan
    public Volume globalVolume;      // Untuk efek vignette

    [Header("UI References")]
    public GameObject gameOverUI;
    public GameObject winUI;
    public Button NextButton;

    public int Score { get; private set; }
    private bool _gameEnded = false;
    private int _sheepMultiplier = 1;

    // Power-up state
    private Coroutine _openMapCoroutine;
    private Coroutine _howlOfFearCoroutine;
    private float _originalVignetteIntensity;
    private float _originalVignetteSmoothness;

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

        // Simpan nilai awal vignette (akan dicari ulang nanti)
        if (globalVolume != null && globalVolume.profile.TryGet(out Vignette vignette))
        {
            _originalVignetteIntensity = vignette.intensity.value;
            _originalVignetteSmoothness = vignette.smoothness.value;
        }

        if (NextButton != null)
            NextButton.onClick.AddListener(OnNextButtonClicked);

        // Cari referensi awal (untuk scene pertama)
        FindUIReferences();
        FindGameReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Perbarui referensi setiap kali scene berubah
        FindUIReferences();
        FindGameReferences();

        // Jika ini adalah scene game, reset game state
        if (scene.name.Contains("Level") || scene.buildIndex >= 1)
        {
            NewGame();
        }
    }

    // ==================== PENCARIAN REFERENSI UI ====================

    private void FindUIReferences()
    {
        // Cari semua GameObject di scene (termasuk yang tidak aktif)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (gameOverUI == null)
        {
            GameObject found = Array.Find(allObjects, go => go.name == "GameOver Ui");
            if (found != null) gameOverUI = found;
        }
        if (winUI == null)
        {
            GameObject found = Array.Find(allObjects, go => go.name == "WinUI");
            if (found != null) winUI = found;
        }
        if (NextButton == null)
        {
            GameObject found = Array.Find(allObjects, go => go.name == "NextButton");
            if (found != null) NextButton = found.GetComponent<Button>();
        }

        if (gameOverUI == null && winUI == null && NextButton == null)
            Debug.Log("UI references not found in this scene.");
        else
            Debug.Log("UI references updated.");
    }

    // ==================== PENCARIAN REFERENSI GAME OBJECT ====================

    private void FindGameReferences()
    {
        // Cari ulang foods parent
        if (foods == null)
        {
            GameObject foodsObj = GameObject.Find("Foods");
            if (foodsObj != null) foods = foodsObj.transform;
        }

        // Cari ulang wolf
        if (wolf == null)
        {
            wolf = FindFirstObjectByType<Wolf>();
        }

        // Cari ulang global volume
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
            if (globalVolume != null && globalVolume.profile.TryGet(out Vignette vignette))
            {
                _originalVignetteIntensity = vignette.intensity.value;
                _originalVignetteSmoothness = vignette.smoothness.value;
            }
        }

        if (foods == null)
            Debug.LogWarning("Foods parent not found in scene!");
        if (wolf == null)
            Debug.LogWarning("Wolf not found in scene!");
        if (globalVolume == null)
            Debug.LogWarning("Global Volume not found in scene!");
    }

    // ==================== GAME FLOW ====================

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        Score = 0;
        _gameEnded = false;

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (winUI != null) winUI.SetActive(false);

        NewRound();
    }

    private void NewRound()
    {
        if (foods == null) return;
        foreach (Transform food in foods)
            food.gameObject.SetActive(true);
        ResetState();
    }

    private void ResetState()
    {
        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        foreach (Sheep sheep in allSheep)
            sheep.ResetState();

        if (wolf != null)
            wolf.gameObject.SetActive(true);
    }

    private void GameOver()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        foreach (Sheep sheep in allSheep)
            sheep.gameObject.SetActive(false);

        if (wolf != null) wolf.gameObject.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);
        AudioManager.Instance?.PlaySFX("Lose");
        Debug.Log("<color=red>GAME OVER! No food left - Wolf wins!</color>");
    }

    public void FoodEaten(Food food)
    {
        food.gameObject.SetActive(false);
        if (!HasRemainingFoods())
            GameOver();
    }

    private bool HasRemainingFoods()
    {
        if (foods == null) return false;
        foreach (Transform food in foods)
            if (food.gameObject.activeSelf) return true;
        return false;
    }

    public void SheepEaten(Sheep sheep)
    {
        Score += sheep.points * _sheepMultiplier;
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        int livingSheep = 0;
        foreach (Sheep s in allSheep)
            if (s.gameObject.activeSelf && !s.IsDead)
                livingSheep++;

        Debug.Log($"Living sheep remaining: {livingSheep}");
        if (livingSheep == 0)
            Win();
    }

    private void Win()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        if (wolf != null) wolf.gameObject.SetActive(false);
        if (winUI != null) winUI.SetActive(true);
        AudioManager.Instance?.PlaySFX("Win");
        Debug.Log("<color=green>PLAYER WINS! All sheep have been eaten!</color>");
    }

    // ==================== LEVEL UNLOCK & MAIN MENU ====================

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
        int unlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);
        if (currentIndex >= unlocked)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentIndex + 1);
            PlayerPrefs.Save();
            Debug.Log("<color=lime>Level baru terbuka!</color>");
        }
    }

    // ==================== POWER-UPS ====================

    public void ActivateOpenMap(float duration, float targetIntensity, float targetSmoothness)
    {
        if (_openMapCoroutine != null) StopCoroutine(_openMapCoroutine);
        _openMapCoroutine = StartCoroutine(OpenMapEffect(duration, targetIntensity, targetSmoothness));
    }

    private IEnumerator OpenMapEffect(float duration, float targetIntensity, float targetSmoothness)
    {
        if (globalVolume == null || !globalVolume.profile.TryGet(out Vignette vignette))
            yield break;

        // Simpan nilai asli jika belum
        _originalVignetteIntensity = vignette.intensity.value;
        _originalVignetteSmoothness = vignette.smoothness.value;

        float stepSize = 0.1f;
        float stepDelay = 0.05f;

        // Turunkan intensitas
        while (vignette.intensity.value > targetIntensity)
        {
            vignette.intensity.value = Mathf.Max(vignette.intensity.value - stepSize, targetIntensity);
            yield return new WaitForSeconds(stepDelay);
        }
        // Turunkan smoothness
        while (vignette.smoothness.value > targetSmoothness)
        {
            vignette.smoothness.value = Mathf.Max(vignette.smoothness.value - stepSize, targetSmoothness);
            yield return new WaitForSeconds(stepDelay);
        }

        vignette.intensity.value = targetIntensity;
        vignette.smoothness.value = targetSmoothness;

        yield return new WaitForSeconds(duration);

        // Kembalikan ke asli
        while (vignette.intensity.value < _originalVignetteIntensity)
        {
            vignette.intensity.value = Mathf.Min(vignette.intensity.value + stepSize, _originalVignetteIntensity);
            yield return new WaitForSeconds(stepDelay);
        }
        while (vignette.smoothness.value < _originalVignetteSmoothness)
        {
            vignette.smoothness.value = Mathf.Min(vignette.smoothness.value + stepSize, _originalVignetteSmoothness);
            yield return new WaitForSeconds(stepDelay);
        }

        vignette.intensity.value = _originalVignetteIntensity;
        vignette.smoothness.value = _originalVignetteSmoothness;
        _openMapCoroutine = null;
    }

    public void ActivateHowlOfFear(float duration, float speedMultiplier)
    {
        if (_howlOfFearCoroutine != null) StopCoroutine(_howlOfFearCoroutine);
        _howlOfFearCoroutine = StartCoroutine(HowlOfFearEffect(duration, speedMultiplier));
    }

    private IEnumerator HowlOfFearEffect(float duration, float speedMultiplier)
    {
        Sheep[] allSheep = FindObjectsByType<Sheep>(FindObjectsSortMode.None);
        var originalSpeeds = new Dictionary<Sheep, float>();

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
        _howlOfFearCoroutine = null;
    }
}