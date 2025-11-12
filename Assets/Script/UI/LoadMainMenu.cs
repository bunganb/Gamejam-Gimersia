using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMainMenu : MonoBehaviour
{
    // Static variable untuk menyimpan progress selama runtime
    public static int unlockedLevel = 1;

    // Method untuk pindah ke scene MainMenu
    public void LoadMainMenuScene()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Button");
        SceneManager.LoadScene("MainMenu");
    }

    // Method untuk langsung membuka panel Levels di MainMenu
    public void LoadMainMenuWithLevelsPanel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Button");

        // Set flag untuk membuka panel Levels saat MainMenu dimuat
        PlayerPrefs.SetInt("OpenLevelsPanel", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainMenu");
    }

    // Static method untuk menyelesaikan level - DIPINDAHKAN KE SINI
    public static void CompleteLevel(int completedLevelId)
    {
        // Update unlockedLevel jika level yang diselesaikan adalah level tertinggi yang sudah di-unlock
        if (completedLevelId >= unlockedLevel)
        {
            unlockedLevel = completedLevelId + 1;
        }
    }

    // Method untuk mendapatkan level yang terkunci
    public static int GetUnlockedLevel()
    {
        return unlockedLevel;
    }

    // Method untuk reset progress
    public static void ResetProgress()
    {
        unlockedLevel = 1;
    }
}