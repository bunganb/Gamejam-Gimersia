using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public List<NamedAudioClip> bgmClips;
    public List<NamedAudioClip> sfxClips;
    [System.Serializable]
public class NamedAudioClip
{
    public string name;        // nama yang kamu tentukan sendiri
    public AudioClip clip;     // file audio-nya
}
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Masukkan ke dictionary pakai nama alias
        foreach (var item in bgmClips)
            bgmDict[item.name] = item.clip;

        foreach (var item in sfxClips)
            sfxDict[item.name] = item.clip;
    }

    public void PlayBGM(string name, float fadeDuration = 1.5f)
    {
        if (!bgmDict.ContainsKey(name))
        {
            Debug.LogWarning("BGM tidak ditemukan: " + name);
            return;
        }

        // Jika ada BGM lain yang sedang main, fade out dulu sebelum ganti
        if (bgmSource.isPlaying)
        {
            StartCoroutine(SwitchBGMWithFade(name, fadeDuration));
        }
        else
        {
            StartCoroutine(FadeInBGM(name, fadeDuration));
        }
    }

    public void StopBGM(float fadeDuration = 1.5f)
    {
        StartCoroutine(FadeOutBGM(fadeDuration));
    }


    public void PlaySFX(string name)
    {
        if (sfxDict.ContainsKey(name))
        {
            AudioClip clip = sfxDict[name];
            sfxSource.PlayOneShot(clip);
            Debug.Log("Play SFX: " + name);

            if (name.ToLower() == "Win" || name.ToLower() == "Lose")
            {
                // Duck BGM selama SFX main
                StartCoroutine(DuckBGMWhileSFX(clip.length, 1.5f));
            }
        }
        else
        {
            Debug.LogWarning("SFX tidak ditemukan: " + name);
        }
    }
    private IEnumerator FadeInBGM(string name, float fadeDuration)
    {
        bgmSource.clip = bgmDict[name];
        bgmSource.volume = 0f;
        bgmSource.loop = true;
        bgmSource.Play();

        float targetVolume = 1f;
        float currentTime = 0f;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, currentTime / fadeDuration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }

    private IEnumerator FadeOutBGM(float fadeDuration)
    {
        float startVolume = bgmSource.volume;
        float currentTime = 0f;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVolume;
    }

    // Ganti BGM lama ke baru dengan transisi halus
    private IEnumerator SwitchBGMWithFade(string newName, float fadeDuration)
    {
        float startVolume = bgmSource.volume;
        float currentTime = 0f;

        // Fade out dulu
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }

        // Ganti lagu
        bgmSource.clip = bgmDict[newName];
        bgmSource.Play();

        // Fade in
        currentTime = 0f;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVolume, currentTime / fadeDuration);
            yield return null;
        }

        bgmSource.volume = startVolume;
    }
    private IEnumerator DuckBGMWhileSFX(float sfxDuration, float fadeDuration)
    {
        float originalVolume = bgmSource.volume;
        float currentTime = 0f;

        // Fade out (turunkan volume pelan)
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(originalVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }

        bgmSource.volume = 0f;

        // Tunggu sampai SFX selesai + sedikit delay agar tidak terlalu tiba-tiba
        yield return new WaitForSeconds(sfxDuration + 0.5f);

        // Fade in kembali ke volume semula
        currentTime = 0f;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, originalVolume, currentTime / fadeDuration);
            yield return null;
        }

        bgmSource.volume = originalVolume;
    }

}
