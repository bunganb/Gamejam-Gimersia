using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public GameObject levelsPanel;    // Panel Levels
    public GameObject mainMenuPanel;  // Panel Main Menu biasa

    void Start()
    {
        // CEK FLAG saat scene MainMenu dimulai
        if (PlayerPrefs.GetInt("OpenLevelsPanel") == 1)
        {
            // AKTIFKAN panel Levels
            levelsPanel.SetActive(true);
            // NONAKTIFKAN panel Main Menu biasa
            mainMenuPanel.SetActive(false);

            // RESET flag agar tidak terbuka terus
            PlayerPrefs.SetInt("OpenLevelsPanel", 0);
            PlayerPrefs.Save();
        }
        else
        {
            // Mode normal: panel Main Menu aktif, Levels tidak
            levelsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}