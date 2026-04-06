using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("재생")]
    [SerializeField] private float sfxVolume;

    private AudioSource _audioSource;

    private void Awake()
    {
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
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    public void PlayAttackClip(AudioClip clip)
    {
        if (clip == null || _audioSource == null)
            return;
        _audioSource.PlayOneShot(clip, sfxVolume);
    }

    // 지금 공격하는 타워의 유닛스탯을 넘김
    public void RandomAttackUnit(UnitStat unitStat)
    {
        if (unitStat?.AttackClips == null || unitStat.AttackClips.Length == 0)
            return;
        PlayAttackClip(unitStat.AttackClips[Random.Range(0, unitStat.AttackClips.Length)]);
    }
}
