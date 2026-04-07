using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("재생")]
    [SerializeField] private float sfxVolume;

    private AudioSource _audioSource;
    // AudioManager 가 없을 때도 마지막 저장된 글로벌 볼륨(마스터*효과음)을 적용하기 위한 캐시
    private float _cachedPrefsVolume = 1f;

    private void Awake()
    {
        _cachedPrefsVolume = PlayerPrefs.GetFloat("volume.sfx", 1f) * PlayerPrefs.GetFloat("volume.master", 1f);
        AudioSource();
    }

    private void Update()
    {
        sfxVolume = AudioManager.Instance.SfxVolume;
    }

    private void AudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        AudioManager.Instance.UiSource = _audioSource;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    public void PlayAttackClip(AudioClip clip)
    {
        if (clip == null || _audioSource == null)
            return;
        _audioSource.PlayOneShot(clip, sfxVolume * GetSfxMultiplier());
    }

    public void RandomAttackUnit(UnitStat unitStat)
    {
        if (unitStat?.AttackClips == null || unitStat.AttackClips.Length == 0)
            return;
        PlayAttackClip(unitStat.AttackClips[Random.Range(0, unitStat.AttackClips.Length)]);
    }

    private float GetSfxMultiplier()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
            // AudioManager 가 존재하면 현재 설정값을 사용
            return Mathf.Clamp01(audioManager.SfxVolume * audioManager.MasterVolume);

        // 씬에 AudioManager 가 없을 때는 PlayerPrefs 에서 읽은 마지막 값 사용
        return _cachedPrefsVolume;
    }
}
