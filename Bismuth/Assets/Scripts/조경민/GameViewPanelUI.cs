using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameViewPanelUI : MonoBehaviour
{
    [Header("━━━━ 참조 ━━━━")]
    [SerializeField] private BattleWaveRunner _battleWaveRunner;
    [SerializeField] private SFXController _sfxController;

    [Header("━━━━ 정비 UI ━━━━")]
    [SerializeField] private GameObject _skipButton;

    [Header("━━━━ 전투 UI ━━━━")]
    [SerializeField] private GameObject _fastButtonObject;
    [SerializeField] private GameObject _pauseButtonObject;

    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("웨이브(Wave 00 (00/00)")]
    [SerializeField] private TMP_Text _waveText;
    [Tooltip("정비 시간 00:00")]
    [SerializeField] private TMP_Text _preparationTimeText;

    [Header("━━━━ 버튼(이미지) ━━━━")]
    [Tooltip("배속 버튼")]
    [SerializeField] private Image _fastButtonImage;
    [Tooltip("일시정지 버튼")]
    [SerializeField] private Image _pauseButtonImage;

    [Header("━━━━ 패널 ━━━━")]
    [Tooltip("Esc 팝업")]
    [SerializeField] private GameObject _pausePopup;
    [Tooltip("일시정지 패널")]
    [SerializeField] private GameObject _pausePanel;

    [Header("━━━━ 설정 ━━━━")]
    [Tooltip("배속할 속도")]
    [SerializeField] private float _fastSpeed = 2f;
    [Tooltip("일시정지 이미지")]
    [SerializeField] private Sprite _pauseSprite;
    [Tooltip("재생 이미지")]
    [SerializeField] private Sprite _playSprite;
    [Tooltip("일반 배속 이미지")]
    [SerializeField] private Sprite _baseSpeedSprite;
    [Tooltip("가속 이미지")]
    [SerializeField] private Sprite _extraSpeedSprite;

    [Header("━━━━ 게임 종료 ━━━━")]
    [Tooltip("게임 클리어")]
    [SerializeField] private GameclearPopupUI _gameclearPopupUI;
    [Tooltip("게임 오버")]
    [SerializeField] private GameoverPopupUI _gameoverPopupUI;

    private bool _isPausePanelOpened => _pausePanel.activeSelf;
    private bool _isFast;
    private Color _originalColor;
    private int _currentWaveNumber;
    private int _currentRemainingCount;
    private int _currentTotalCount;

    private void Awake()
    {
        _originalColor = _fastButtonImage.color;
    }

    private void OnEnable()
    {
        if (_battleWaveRunner == null) return;

        _battleWaveRunner.WaveStarted += WaveStarted;
        _battleWaveRunner.WaveSpawnProgressChanged += WaveSpawnProgressChanged;
        _battleWaveRunner.IntermissionStarted += IntermissionStarted;
        _battleWaveRunner.IntermissionTimeChanged += IntermissionTimeChanged;
        _battleWaveRunner.IntermissionEnded += IntermissionEnded;
        _battleWaveRunner.BattleCompleted += BattleCompleted;
        _battleWaveRunner.BattleFailed += BattleFailed;
    }

    private void Start()
    {
        RefreshWaveText();
        // SetBattleUIActive(true);
        // SetPreparationUIActive(false);
    }

    private void OnDisable()
    {
        if (_battleWaveRunner == null) return;

        _battleWaveRunner.WaveStarted -= WaveStarted;
        _battleWaveRunner.WaveSpawnProgressChanged -= WaveSpawnProgressChanged;
        _battleWaveRunner.IntermissionStarted -= IntermissionStarted;
        _battleWaveRunner.IntermissionTimeChanged -= IntermissionTimeChanged;
        _battleWaveRunner.IntermissionEnded -= IntermissionEnded;
        _battleWaveRunner.BattleCompleted -= BattleCompleted;
        _battleWaveRunner.BattleFailed -= BattleFailed;
    }

    // Esc 버튼
    public void OnClickEsc()
    {
        _pausePopup.SetActive(true);
        TimeScaleController.Instance.SetPausePopup(true);
    }

    // 배속 버튼
    public void OnClickFast()
    {
        _isFast = !_isFast;
        UpdateFastButton();
        TimeScaleController.Instance.ToggleSpeed(_fastSpeed);
    }

    // 일시정지 버튼
    public void OnClickPause()
    {
        _pausePanel.SetActive(!_isPausePanelOpened);
        UpdatePauseButton();
        TimeScaleController.Instance.SetPausePanel(_isPausePanelOpened);
    }

    // 정비시간 스킵 버튼
    public void OnClickSkip()
    {
        if (_battleWaveRunner.IsWaitingForNextWaveStart == false) return;

        _battleWaveRunner.StartNextWave();
    }

    // 웨이브 시작때 텍스트 갱신 
    private void WaveStarted(WaveDataSO waveData)
    {
        _currentWaveNumber = waveData.WaveNumber;
        RefreshWaveText();
    }

    // 웨이브 스폰 진행 상황 텍스트 갱신
    private void WaveSpawnProgressChanged(int remainingCount, int totalCount)
    {
        _currentRemainingCount = remainingCount;
        _currentTotalCount = totalCount;
        RefreshWaveText();
    }

    // 정비 시간 시작 때 텍스트 갱신
    private void IntermissionStarted(WaveDataSO nextWave, float duration)
    {
        SetBattleUIActive(false);
        SetPreparationUIActive(true);
        UpdatePreparationTimeText(duration);
    }

    // 정비시간 텍스트, 스킵 버튼 활성화 / 시간 갱신
    private void IntermissionTimeChanged(float remainingTime)
    {
        UpdatePreparationTimeText(remainingTime);
    }

    // 정비 시간 종료 때 정비시간 텍스트, 스킵 버튼 비활성화
    private void IntermissionEnded()
    {
        SetPreparationUIActive(false);
        SetBattleUIActive(true);
    }

    // 배틀 완료 때 정비시간 텍스트, 스킵 버튼 비활성화
    private void BattleCompleted()
    {
        SetPreparationUIActive(false);

        _sfxController.OnGameVictory();
        
        _gameclearPopupUI.gameObject.SetActive(true);
        MapBattleConfigSO config = _battleWaveRunner.MapBattleConfig;
        _gameclearPopupUI.ShowResult(config.MapName, config.DifficultyName);
    }

    // 배틀 실패 때 정비시간 텍스트, 스킵 버튼 비활성화
    private void BattleFailed()
    {
        _sfxController.OnGameOver();
        
        SetPreparationUIActive(false);

        _gameoverPopupUI.gameObject.SetActive(true);
    }

    private void RefreshWaveText()
    {
        _waveText.text = $"Wave {_currentWaveNumber:D2} ({_currentRemainingCount:D2}/{_currentTotalCount:D2})";
    }

    private void UpdatePreparationTimeText(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        _preparationTimeText.text = $"{LocalizationManager.Instance.Get("SETUP_TIME")} {minutes:D2}:{seconds:D2}";
    }

    private void SetBattleUIActive(bool isActive)
    {
        _waveText.gameObject.SetActive(isActive);
        _fastButtonObject.SetActive(isActive);
        _pauseButtonObject.SetActive(isActive);
    }

    private void SetPreparationUIActive(bool isActive)
    {
        _preparationTimeText.gameObject.SetActive(isActive);
        _skipButton.SetActive(isActive);
    }

    // 배속 버튼 밝기 조정
    private void UpdateFastButton()
    {
        _fastButtonImage.sprite = _isFast ? _extraSpeedSprite : _baseSpeedSprite;
    }

    // 일시정지 버튼 이미지 조정
    private void UpdatePauseButton()
    {
        _pauseButtonImage.sprite = _isPausePanelOpened ? _playSprite : _pauseSprite;
    }
}
