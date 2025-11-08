using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGames()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGames()
    {
        Application.Quit();
    }
}
