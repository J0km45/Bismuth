using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMController : MonoBehaviour
{
    public static BGMController Instance { get; private set; }
    
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private SFXSO _sfxso;
    
    [SerializeField] private int currentScene = -1;

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
        StartBGM();
    }

    private void StartBGM()
    {
        currentScene = SceneManager.GetActiveScene().buildIndex;

        BGMType nextType;

        switch (currentScene)
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
                return;
        }

        if (_currentBGMType == nextType)
            return;

        _currentBGMType = nextType;
        AudioManager.Instance.PlayBGM(_audioSource, _sfxso.BGMList[(int)nextType]);
    }

    public void BossBGM()
    {
        AudioManager.Instance.PlayBGM(_audioSource, _sfxso.BGMList[(int)BGMType.Boss]);
    }
}
