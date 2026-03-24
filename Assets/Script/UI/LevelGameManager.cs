using UnityEngine;

public class LevelGameManager : MonoBehaviour
{
    public int currentLevelId;

    // Dipanggil ketika player menang
    public void WinLevel()
    {
        // Unlock level berikutnya
        LoadMainMenu.CompleteLevel(currentLevelId);

        // Tampilkan UI kemenangan (bisa diimplementasikan sesuai kebutuhan)
        ShowWinScreen();
    }

    private void ShowWinScreen()
    {
        // Di sini Anda bisa menampilkan UI kemenangan
        // Contoh sederhana: langsung kembali ke main menu dengan membuka panel levels
        LoadMainMenu loadMenu = FindFirstObjectByType<LoadMainMenu>();
        if (loadMenu != null)
        {
            loadMenu.LoadMainMenuWithLevelsPanel();
        }
        else
        {
            // Jika tidak ditemukan, buat temporary loader (fallback)
            Debug.LogWarning("LoadMainMenu not found, creating temporary loader.");
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
        LoadMainMenu loadMenu = FindFirstObjectByType<LoadMainMenu>();
        if (loadMenu != null)
        {
            loadMenu.LoadMainMenuWithLevelsPanel();
        }
        else
        {
            // Fallback: muat scene MainMenu langsung tanpa membuka panel levels
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}