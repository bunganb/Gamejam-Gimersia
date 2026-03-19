using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    public enum VolumeType
    {
        Master,
        BGM,
        SFX
    }


    [SerializeField] Slider volumeSlider;
    [SerializeField] VolumeType volumeType = VolumeType.Master;

    private string PrefKey => volumeType switch
    {
        VolumeType.Master => "masterVolume",
        VolumeType.BGM => "bgmVolume",
        VolumeType.SFX => "sfxVolume",
        _ => "masterVolume"
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(PrefKey, 1f);
        volumeSlider.value = saved;

        ApplyVolume(saved);

        volumeSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(PrefKey, value);
    }

    private void ApplyVolume(float value)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[AudioSlider] AudioManager tidak ditemukan!");
            return;
        }

        switch (volumeType)
        {
            case VolumeType.Master:
                AudioManager.Instance.SetMasterVolume(value);
                break;
            case VolumeType.BGM:
                AudioManager.Instance.SetBGMVolume(value);
                break;
            case VolumeType.SFX:
                AudioManager.Instance.SetSFXVolume(value);
                break;
        }
    }
}

