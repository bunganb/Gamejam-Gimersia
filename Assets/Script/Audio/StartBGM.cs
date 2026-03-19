using UnityEngine;

public class StartBGM : MonoBehaviour
{
    [SerializeField] private string bgmName;
    [SerializeField] private float fadeDuration = 1.5f;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager instance is not found!");
            return;
        }

        if(string.IsNullOrEmpty(bgmName))
        {
            Debug.LogWarning("BGM name is not set. No BGM will be played.");
            return;
        }

        AudioManager.Instance.PlayBGM(bgmName, fadeDuration);

    }
}
