using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public List<NamedAudioClip> bgmClips;
    public List<NamedAudioClip> sfxClips;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Ducking Settings")]
    [Tooltip("Nama-nama SFX yang akan menurunkan volume BGM sementara (ducking)")]
    public List<string> importantSFXNames = new List<string>() { "Win", "Lose" };
    [Range(0f, 1f)] public float duckedVolume = 0.1f; // Volume BGM saat ducking
    public float duckFadeDuration = 0.5f; // Durasi fade in/out ducking

    [System.Serializable]
    public class NamedAudioClip
    {
        public string name;
        public AudioClip clip;
    }

    private Dictionary<string, AudioClip> _bgmDict = new();
    private Dictionary<string, AudioClip> _sfxDict = new();

    private string _currentBGMName = "";
    private Coroutine _bgmCoroutine;
    private Coroutine _duckCoroutine;
    private float _originalBGMVolume = 1f;

    private float TargetBGMVolume => masterVolume * bgmVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionaries();
    }

    private void BuildDictionaries()
    {
        _bgmDict.Clear();
        _sfxDict.Clear();

        foreach (var item in bgmClips)
        {
            if (item.clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM clip '{item.name}' is null, skipping.");
                continue;
            }
            _bgmDict[item.name] = item.clip;
        }

        foreach (var item in sfxClips)
        {
            if (item.clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip '{item.name}' is null, skipping.");
                continue;
            }
            _sfxDict[item.name] = item.clip;
        }
    }

    // ─────────────────────────────────────────────
    //  BGM
    // ─────────────────────────────────────────────

    public void PlayBGM(string name, float fadeDuration = 1.5f)
    {
        if (!_bgmDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] BGM tidak ditemukan: '{name}'");
            return;
        }

        if (_currentBGMName == name && bgmSource.isPlaying) return;

        _currentBGMName = name;

        StopBGMCoroutine();

        if (bgmSource.isPlaying)
            _bgmCoroutine = StartCoroutine(SwitchBGMWithFade(clip, fadeDuration));
        else
            _bgmCoroutine = StartCoroutine(FadeInBGM(clip, fadeDuration));
    }

    public void StopBGM(float fadeDuration = 1.5f)
    {
        StopBGMCoroutine();
        _currentBGMName = "";
        _bgmCoroutine = StartCoroutine(FadeOutBGM(fadeDuration, stopAfterFade: true));
    }

    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();

    // ─────────────────────────────────────────────
    //  SFX
    // ─────────────────────────────────────────────

    public void PlaySFX(string name, bool stopBGM = false)
    {
        if (!_sfxDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] SFX tidak ditemukan: '{name}'");
            return;
        }

        sfxSource.volume = masterVolume * sfxVolume;
        sfxSource.PlayOneShot(clip);
        Debug.Log($"[AudioManager] Play SFX: {name}");

        if (stopBGM)
        {
            StopBGMCoroutine();
            _bgmCoroutine = StartCoroutine(FadeOutBGM(1.5f, stopAfterFade: true));
        }
        else if (importantSFXNames.Contains(name))
        {
            // Lakukan ducking untuk SFX penting, dengan durasi clip
            StartDucking(clip.length);
        }
    }

    // ─────────────────────────────────────────────
    //  DUCKING
    // ─────────────────────────────────────────────

    private void StartDucking(float duration)
    {
        if (_duckCoroutine != null)
            StopCoroutine(_duckCoroutine);

        _duckCoroutine = StartCoroutine(DuckBGM(duration));
    }

    private IEnumerator DuckBGM(float duration)
    {
        // Simpan volume asli jika belum disimpan
        _originalBGMVolume = bgmSource.volume;

        // Fade down ke duckedVolume
        yield return FadeVolume(bgmSource, _originalBGMVolume, duckedVolume, duckFadeDuration);

        // Tunggu sampai SFX selesai (durasi clip)
        yield return new WaitForSecondsRealtime(duration);

        // Fade up kembali ke volume asli
        yield return FadeVolume(bgmSource, duckedVolume, _originalBGMVolume, duckFadeDuration);

        _duckCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  VOLUME CONTROL
    // ─────────────────────────────────────────────

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        sfxSource.volume = masterVolume * sfxVolume;
    }

    private void ApplyVolumes()
    {
        if (_bgmCoroutine == null && _duckCoroutine == null)
            bgmSource.volume = TargetBGMVolume;
    }

    // ─────────────────────────────────────────────
    //  COROUTINES (PRIVATE)
    // ─────────────────────────────────────────────

    private IEnumerator FadeInBGM(AudioClip clip, float fadeDuration)
    {
        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.loop = true;
        bgmSource.Play();

        yield return FadeVolume(bgmSource, 0f, TargetBGMVolume, fadeDuration);
        _bgmCoroutine = null;
    }

    private IEnumerator FadeOutBGM(float fadeDuration, bool stopAfterFade)
    {
        float startVol = bgmSource.volume;
        yield return FadeVolume(bgmSource, startVol, 0f, fadeDuration);

        if (stopAfterFade)
        {
            bgmSource.Stop();
            bgmSource.volume = TargetBGMVolume;
        }

        _bgmCoroutine = null;
    }

    private IEnumerator SwitchBGMWithFade(AudioClip newClip, float fadeDuration)
    {
        float startVol = bgmSource.volume;
        yield return FadeVolume(bgmSource, startVol, 0f, fadeDuration);

        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();

        yield return FadeVolume(bgmSource, 0f, TargetBGMVolume, fadeDuration);
        _bgmCoroutine = null;
    }

    private IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        source.volume = to;
    }

    private void StopBGMCoroutine()
    {
        if (_bgmCoroutine != null)
        {
            StopCoroutine(_bgmCoroutine);
            _bgmCoroutine = null;
        }
    }
}