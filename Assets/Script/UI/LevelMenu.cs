using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    public Button[] buttons;
    public GameObject mainMenuPanel;
    public GameObject levelsButtonsPanel;

    private void Start()
    {
        // Cek apakah perlu membuka panel Levels secara otomatis
        if (PlayerPrefs.GetInt("OpenLevelsPanel", 0) == 1)
        {
            ShowLevelsPanel();
            PlayerPrefs.SetInt("OpenLevelsPanel", 0);
            PlayerPrefs.Save();
        }

        UpdateUnlockedLevels();

        // Setup button listeners
        for (int i = 0; i < buttons.Length; i++)
        {
            int levelIndex = i + 1;
            buttons[i].onClick.AddListener(() => OpenLevel(levelIndex));
        }
    }

    private void UpdateUnlockedLevels()
    {
        // Gunakan static variable dari LoadMainMenu untuk menentukan button mana yang interactable
        int currentUnlocked = LoadMainMenu.GetUnlockedLevel();

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = (i + 1 <= currentUnlocked);
        }
    }

    public void ShowLevelsPanel()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (levelsButtonsPanel != null)
            levelsButtonsPanel.SetActive(true);

        // Update status unlock setiap kali panel dibuka
        UpdateUnlockedLevels();
    }

    public void ShowMainMenuPanel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Button");

        if (levelsButtonsPanel != null)
            levelsButtonsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void OpenLevel(int levelId)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Button");
        string levelName = "Level" + levelId;
        SceneManager.LoadScene(levelName);
    }

    // Method untuk reset progress (bisa dipanggil dari tombol)
    public void ResetProgressButton()
    {
        LoadMainMenu.ResetProgress();
        UpdateUnlockedLevels();
    }

    public void buttonClikedSFX()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Button");
    }
}