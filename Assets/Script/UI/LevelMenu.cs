using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMenu : MonoBehaviour
{
    public void OpenLevel(int levelId)
    {
        string levelName = "Level1" + levelId;
        SceneManager.LoadScene(levelName);
    }
}
