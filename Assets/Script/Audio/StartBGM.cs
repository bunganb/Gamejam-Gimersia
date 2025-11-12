using UnityEngine;

public class StartBGM : MonoBehaviour
{
    public string bgmName;

    private void Start()
    {
        AudioManager.Instance.PlayBGM(bgmName);
    }
}
