using UnityEngine;

public class LevelGameManager : MonoBehaviour
{
    public int currentLevelId;

    // Dipanggil ketika player menang
    public void WinLevel()
    {
        // Unlock level berikutnya menggunakan static method di LoadMainMenu
        LoadMainMenu.CompleteLevel(currentLevelId);

        // Tampilkan UI kemenangan atau langsung kembali ke main menu
        ShowWinScreen();
    }

    private void ShowWinScreen()
    {
        // Di sini Anda bisa menampilkan UI kemenangan
        // Contoh sederhana: langsung kembali ke main menu dengan membuka panel levels
        LoadMainMenu loadMenu = FindObjectOfType<LoadMainMenu>();
        if (loadMenu != null)
        {
            loadMenu.LoadMainMenuWithLevelsPanel();
        }
        else
        {
            // Fallback: buat GameObject baru dengan script LoadMainMenu
            GameObject tempObj = new GameObject("TempLoader");
            LoadMainMenu tempLoader = tempObj.AddComponent<LoadMainMenu>();
            tempLoader.LoadMainMenuWithLevelsPanel();
        }
    }

    // Untuk testing - bisa dipanggil dari button
    public void TestWinLevel()
    {
        WinLevel();
    }

    // Method untuk tombol yang melakukan dua fungsi sekaligus
    public void CompleteLevelAndLoadMainMenu()
    {
        // 1. Selesaikan level
        LoadMainMenu.CompleteLevel(currentLevelId);

        // 2. Pindah ke MainMenu
        LoadMainMenu loadMenu = FindObjectOfType<LoadMainMenu>();
        if (loadMenu != null)
        {
            loadMenu.LoadMainMenuWithLevelsPanel();
        }
    }
}