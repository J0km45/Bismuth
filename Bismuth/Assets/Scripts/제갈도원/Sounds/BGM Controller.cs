using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMController : MonoBehaviour
{
    public static BGMController Instance { get; private set; }
    
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private SFXSO _sfxso;
    
    [SerializeField] private int _currentScene = -1;

    [SerializeField] private bool _isPlayingNormalBGM = false;
    public bool IsPlayingNormalBGM {get {return _isPlayingNormalBGM;} set {_isPlayingNormalBGM = value;}}

    private BGMType _currentBGMType = (BGMType)(-1);
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayStartBGM();
    }

    private void PlayStartBGM()
    {
        _currentScene = SceneManager.GetActiveScene().buildIndex;

        BGMType nextType;

        switch (_currentScene)
        {
            case 0:
            case 1:
                nextType = BGMType.Main;
                break;

            case 2:
            case 3:
            case 4:
                nextType = BGMType.Forest;
                break;

            case 5:
            case 6:
            case 7:
                nextType = BGMType.FortTown;
                break;

            case 8:
            case 9:
            case 10:
                nextType = BGMType.Ruins;
                break;

            default:
                nextType = BGMType.Forest;
                break;
        }

        if (_currentBGMType == nextType)
            return;
        
        DebugTool.Log($"_currentScene: {_currentScene}\n" +
                      $"{_currentBGMType.ToString()}\n" +
                      $"{nextType.ToString()}", DebugType.Game, this);
        
        _currentBGMType = nextType;
        AudioManager.Instance.PlayBGM(_audioSource, _sfxso.BGMList[(int)_currentBGMType]);
    }

    public void PlayNormalBGM()
    {
        if(!_isPlayingNormalBGM)
            AudioManager.Instance.PlayBGM(_audioSource, _sfxso.BGMList[(int)_currentBGMType]);
    }

    public void PlayBossBGM()
    {
        AudioManager.Instance.PlayBGM(_audioSource, _sfxso.BGMList[(int)BGMType.Boss]);
    }
}
