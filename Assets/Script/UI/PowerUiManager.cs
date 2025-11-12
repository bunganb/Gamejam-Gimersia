using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager Instance;

    [Header("UI Slider")]
    [SerializeField] private Slider powerUpSlider;

    [Header("Power-Up Icons")]
    [SerializeField] private GameObject openMapIcon;
    [SerializeField] private GameObject howlOfFearIcon;

    private void Awake()
    {
        Instance = this;
        powerUpSlider.gameObject.SetActive(false);

        // Pastikan semua icon nonaktif di awal
        openMapIcon.SetActive(false);
        howlOfFearIcon.SetActive(false);
    }

    /// <summary>
    /// Menampilkan slider dan icon sesuai jenis power-up.
    /// </summary>
    public void ShowPowerUpDuration(float duration, PowerUpType type)
    {
        StopAllCoroutines();
        StartCoroutine(UpdatePowerUpSlider(duration, type));
    }

    private IEnumerator UpdatePowerUpSlider(float duration, PowerUpType type)
    {
        // Tampilkan ikon sesuai tipe power-up
        openMapIcon.SetActive(type == PowerUpType.OpenMap);
        howlOfFearIcon.SetActive(type == PowerUpType.HowlOfFear);

        // Atur slider
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

        // Sembunyikan setelah selesai
        powerUpSlider.gameObject.SetActive(false);
        openMapIcon.SetActive(false);
        howlOfFearIcon.SetActive(false);
    }
}
    