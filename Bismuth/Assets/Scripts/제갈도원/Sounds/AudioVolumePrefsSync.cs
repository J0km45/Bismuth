using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioVolumePrefsSync : MonoBehaviour
{
    // PlayerPrefs 저장 키
    private const string MasterKey = "volume.master";
    private const string BgmKey = "volume.bgm";
    private const string SfxKey = "volume.sfx";
    private const string UiKey = "volume.ui";

    // 중복 생성 방지
    private static bool _isCreated;

    // 현재 연결된 AudioManager
    private AudioManager _audioManager;

    // 마지막 저장값
    private Volume _lastVolume;

    // PlayerPrefs 에서 한 번이라도 불러왔는지 확인
    private bool _isLoadedFromPrefs;

    // 게임 시작 할 때 자동으로 특정 메서드를 실행 시켜주는 유니티 기능
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    
    // 씬 마다 따로 배치 하지 않아도 PlayerPrefs 동기화가 항상 동작 
    private static void CreateSingleton()
    {
        if (_isCreated)
            return;

        GameObject syncObject = new GameObject(nameof(AudioVolumePrefsSync));
        Object.DontDestroyOnLoad(syncObject);
        syncObject.AddComponent<AudioVolumePrefsSync>();

        _isCreated = true;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadVolumeFromPrefs();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadVolumeFromPrefs();
    }

    private void Update()
    {
        if (!TryFindAudioManager())
            return;

        if (!_isLoadedFromPrefs)
            LoadVolumeFromPrefs();

        Volume currentVolume = new Volume(
            _audioManager.MasterVolume,
            _audioManager.BgmVolume,
            _audioManager.SfxVolume,
            _audioManager.UIVolume
        );

        if (_lastVolume.IsApproximately(currentVolume))
            return;

        SaveVolumePrefs(currentVolume);
        _lastVolume = currentVolume;
    }

    private void LoadVolumeFromPrefs()
    {
        if (!TryFindAudioManager())
            return;

        Volume savedVolume = new Volume(
            PlayerPrefs.GetFloat(MasterKey, 1f),
            PlayerPrefs.GetFloat(BgmKey, 1f),
            PlayerPrefs.GetFloat(SfxKey, 1f),
            PlayerPrefs.GetFloat(UiKey, 1f)
        );

        _audioManager.SetMasterVolume(savedVolume.Master);
        _audioManager.SetBgmVolume(savedVolume.Bgm);
        _audioManager.SetSfxVolume(savedVolume.Sfx);
        _audioManager.SetUIVolume(savedVolume.Ui);

        _lastVolume = savedVolume;
        _isLoadedFromPrefs = true;
    }

    private void SaveVolumePrefs(Volume volume)
    {
        PlayerPrefs.SetFloat(MasterKey, volume.Master);
        PlayerPrefs.SetFloat(BgmKey, volume.Bgm);
        PlayerPrefs.SetFloat(SfxKey, volume.Sfx);
        PlayerPrefs.SetFloat(UiKey, volume.Ui);
        PlayerPrefs.Save();
    }

    private bool TryFindAudioManager()
    {
        if (_audioManager != null)
            return true;

        _audioManager = AudioManager.Instance;

        if (_audioManager == null)
            _audioManager = Object.FindFirstObjectByType<AudioManager>();

        return _audioManager != null;
    }

    private readonly struct Volume
    {
        
        private const float Epsilon = 0.0001f;

        public float Master { get; }
        public float Bgm { get; }
        public float Sfx { get; }
        public float Ui { get; }

        public Volume(float master, float bgm, float sfx, float ui)
        {
            Master = Mathf.Clamp01(master);
            Bgm = Mathf.Clamp01(bgm);
            Sfx = Mathf.Clamp01(sfx);
            Ui = Mathf.Clamp01(ui);
        }

        public bool IsApproximately(Volume other)
        {
            return Mathf.Abs(Master - other.Master) < Epsilon
                && Mathf.Abs(Bgm - other.Bgm) < Epsilon
                && Mathf.Abs(Sfx - other.Sfx) < Epsilon
                && Mathf.Abs(Ui - other.Ui) < Epsilon;
        }
    }
}