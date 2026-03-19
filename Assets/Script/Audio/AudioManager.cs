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

    [System.Serializable]
    public class NamedAudioClip
    {
        public string name;
        public AudioClip clip;
    }

    private Dictionary<string, AudioClip> _bgmDict = new();
    private Dictionary<string, AudioClip> _sfxDict = new();

    // Nama BGM yang sedang aktif (untuk cegah replay lagu yang sama)
    private string _currentBGMName = "";

    // Simpan referensi coroutine aktif agar bisa di-stop sebelum mulai yang baru
    private Coroutine _bgmCoroutine;

    // Properti target volume BGM setelah fade selesai
    private float TargetBGMVolume => masterVolume * bgmVolume;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

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

    /// <summary>
    /// Memainkan BGM dengan nama tertentu. Jika sudah playing, tidak diulang.
    /// </summary>
    public void PlayBGM(string name, float fadeDuration = 1.5f)
    {
        if (!_bgmDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] BGM tidak ditemukan: '{name}'");
            return;
        }

        // Hindari restart jika lagu yang sama sedang main
        if (_currentBGMName == name && bgmSource.isPlaying) return;

        _currentBGMName = name;

        // Hentikan coroutine fade yang mungkin masih berjalan
        StopBGMCoroutine();

        if (bgmSource.isPlaying)
            _bgmCoroutine = StartCoroutine(SwitchBGMWithFade(clip, fadeDuration));
        else
            _bgmCoroutine = StartCoroutine(FadeInBGM(clip, fadeDuration));
    }

    /// <summary>
    /// Menghentikan BGM dengan fade out.
    /// </summary>
    public void StopBGM(float fadeDuration = 1.5f)
    {
        StopBGMCoroutine();
        _currentBGMName = "";
        _bgmCoroutine = StartCoroutine(FadeOutBGM(fadeDuration, stopAfterFade: true));
    }

    /// <summary>
    /// Pause BGM (berguna saat pause menu).
    /// </summary>
    public void PauseBGM() => bgmSource.Pause();

    /// <summary>
    /// Resume BGM setelah di-pause.
    /// </summary>
    public void ResumeBGM() => bgmSource.UnPause();

    // ─────────────────────────────────────────────
    //  SFX
    // ─────────────────────────────────────────────

    /// <summary>
    /// Memainkan SFX sekali. Gunakan stopBGM = true untuk menghentikan BGM (contoh: scene end).
    /// </summary>
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
            _bgmCoroutine = StartCoroutine(FadeOutBGM(fadeDuration: 1.5f, stopAfterFade: true));
        }
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
        // Hanya update volume jika tidak sedang dalam proses fade
        if (_bgmCoroutine == null)
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
            bgmSource.volume = TargetBGMVolume; // Reset untuk play berikutnya
        }

        _bgmCoroutine = null;
    }

    private IEnumerator SwitchBGMWithFade(AudioClip newClip, float fadeDuration)
    {
        // Fade out lagu lama
        float startVol = bgmSource.volume;
        yield return FadeVolume(bgmSource, startVol, 0f, fadeDuration);

        // Ganti clip dan play
        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();

        // Fade in lagu baru ke target volume
        yield return FadeVolume(bgmSource, 0f, TargetBGMVolume, fadeDuration);
        _bgmCoroutine = null;
    }

    /// <summary>
    /// Utilitas fade volume reusable — menghindari duplikasi kode di setiap coroutine.
    /// </summary>
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
            elapsed += Time.unscaledDeltaTime; // unscaledDeltaTime agar bekerja saat Time.timeScale = 0 (pause)
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