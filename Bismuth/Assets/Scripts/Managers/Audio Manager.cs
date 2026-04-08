using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private float _masterVolume = 0.5f;  // 전체
    [SerializeField] private float _bgmVolume = 0.5f;     // 배경음악
    [SerializeField] private float _sfxVolume = 0.5f;   // 특수 효과음
    [SerializeField] private float _uiVolume = 0.5f;      // UI 효과음
    
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private AudioSource _uiSource;
    
    public AudioSource BgmSource { get { return _bgmSource; } set {_bgmSource = value; } }
    public AudioSource SfxSource { get { return _sfxSource; } set {_sfxSource = value; } }
    public AudioSource UiSource { get { return _uiSource; } set {_uiSource = value; } }

    public float MasterVolume
    {
        get { return _masterVolume; }
        set
        {
            _masterVolume = value;
        }
    }

    public float BgmVolume
    {
        get { return _bgmVolume; }
        set
        {
            _bgmVolume = value;
        }
    }

    public float SfxVolume
    {
        get { return _sfxVolume; }
        set
        {
            _sfxVolume = value;
        }
    }

    public float UIVolume
    {
        get { return _uiVolume; }
        set
        {
            _uiVolume = value;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // BGM 재생
    public void PlayBGM(AudioSource source, AudioClip clip)
    {
        if (source == null) return;
        _bgmSource = source;
        source.volume = UIVolume *_masterVolume;
        source.clip = clip;
        source.Play();
    }

    // 전투 효과음 재생
    public void PlaySFX(AudioSource source, AudioClip clip)
    {
        if (source == null) return;
        source.volume = UIVolume *_masterVolume;
        source.clip = clip;
        source.PlayOneShot(source.clip);
    }

    // UI 효과음 재생
    public void PlayUI(AudioSource source, AudioClip clip)
    {
        if (source == null) return;
        _uiSource = source;
        source.volume = UIVolume *_masterVolume;
        source.clip = clip;
        source.PlayOneShot(source.clip);
    }

    public void SetMasterVolume(float value)
    {
        _masterVolume = value;
        if (_bgmSource != null)
            _bgmSource.volume = BgmVolume * _masterVolume;
        if (_uiSource != null)
            _uiSource.volume = UIVolume * _masterVolume;
        if (_sfxSource != null)
            _sfxSource.volume = SfxVolume * _masterVolume;
    }

    public void SetBgmVolume(float value)
    {
        _bgmVolume = value;
        if (_bgmSource != null)
            _bgmSource.volume = BgmVolume * _masterVolume;
    }

    public void SetSfxVolume(float value)
    {
        _sfxVolume = value;
        if (_sfxSource != null)
            _sfxSource.volume = SfxVolume * _masterVolume;
    }

    public void SetUIVolume(float value)
    {
        _uiVolume = value;
        if (_uiSource != null)
            _uiSource.volume = UIVolume * _masterVolume;
    }
}