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
        get { return _masterVolume * _bgmVolume; }
        set
        {
            _bgmVolume = value;
        }
    }

    public float SfxVolume
    {
        get { return _masterVolume * _sfxVolume; }
        set
        {
            _sfxVolume = value;
        }
    }

    public float UIVolume
    {
        get { return _masterVolume * _uiVolume; }
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
        source.volume = UIVolume;
        source.clip = clip;
        source.Play();
    }

    // 전투 효과음 재생
    public void PlaySFX(AudioSource source, AudioClip clip)
    {
        if (source == null) return;
        source.volume = UIVolume;
        source.clip = clip;
        source.PlayOneShot(source.clip);
    }

    // UI 효과음 재생
    public void PlayUI(AudioSource source, AudioClip clip)
    {
        if (source == null) return;
        _uiSource = source;
        source.volume = UIVolume;
        source.clip = clip;
        source.PlayOneShot(source.clip);
    }

    public void SetMasterVolume(float value)
    {
        _masterVolume = value;
        _bgmSource.volume = BgmVolume;
        _uiSource.volume = UIVolume;
    }

    public void SetBgmVolume(float value)
    {
        _bgmVolume = value;
        _bgmSource.volume = BgmVolume;
    }

    public void SetSfxVolume(float value)
    {
        _sfxVolume = value;
    }

    public void SetUIVolume(float value)
    {
        _uiVolume = value;
        _uiSource.volume = UIVolume;
    }
}