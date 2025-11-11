using UnityEngine;
using System.Collections.Generic;

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

    public void PlayBGM(string name)
    {
        if (bgmDict.ContainsKey(name))
        {
            bgmSource.clip = bgmDict[name];
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("BGM tidak ditemukan: " + name);
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySFX(string name)
    {
        if (sfxDict.ContainsKey(name))
        {
            sfxSource.PlayOneShot(sfxDict[name]);
            Debug.Log("Play SFX: " + name);
        }
        else
        {
            Debug.LogWarning("SFX tidak ditemukan: " + name);
        }
    }
}
