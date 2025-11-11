using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager Instance;

    [SerializeField] private Slider powerUpSlider;

    private void Awake()
    {
        Instance = this;
        powerUpSlider.gameObject.SetActive(false);
    }

    public void ShowPowerUpDuration(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(UpdatePowerUpSlider(duration));
    }

    private IEnumerator UpdatePowerUpSlider(float duration)
    {
        powerUpSlider.gameObject.SetActive(true);
        powerUpSlider.maxValue = duration;
        powerUpSlider.value = duration;

        float timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            powerUpSlider.value = timer;
            yield return null;
        }

        powerUpSlider.gameObject.SetActive(false);
    }
}
