using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    public void Pause()
    {
        AudioManager.Instance.PlaySFX("Button");
        pauseMenu.SetActive(true);
        Time.timeScale = 0;
    }

    public void Home()
    {
        AudioManager.Instance.PlaySFX("Button");
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1;
    }

    public void Resume()
    {
        AudioManager.Instance.PlaySFX("Button");
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
    }

    public void Restart()
    {
        AudioManager.Instance.PlaySFX("Button");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }
}
    