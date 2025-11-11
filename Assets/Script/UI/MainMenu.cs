using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    public void PlayGames()
    {
        AudioManager.Instance.PlaySFX("Button");
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGames()
    {
        AudioManager.Instance.PlaySFX("Button");
        Application.Quit();
    }
}
