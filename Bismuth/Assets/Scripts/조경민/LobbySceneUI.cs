using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbySceneUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("전투")]
    [SerializeField] private TMP_Text _combatText;
    [Tooltip("전투시작")]
    [SerializeField] private TMP_Text _startText;
    [Tooltip("난이도 정보")]
    [SerializeField] private TMP_Text _infoText;
    [Tooltip("게임 방법")]
    [SerializeField] private TMP_Text _howToPlayText;
    [Tooltip("도감")]
    [SerializeField] private TMP_Text _unitsText;

    [Header("━━━━ 테두리 ━━━━")]
    // [Tooltip("전투 버튼")]
    // [SerializeField] private GameObject _combatOutline;
    [Tooltip("쉬움")]
    [SerializeField] private GameObject _easyOutline;
    [Tooltip("보통")]
    [SerializeField] private GameObject _normalOutline;
    [Tooltip("어려움")]
    [SerializeField] private GameObject _hardOutline;

    [Header("━━━━ 패널 ━━━━")]
    [Tooltip("환경 설정 팝업")]
    [SerializeField] private GameObject _settingsPopup;
    [Tooltip("맵 선택 스크롤뷰")]
    [SerializeField] private GameObject _mapScrollView;
    [Tooltip("난이도")]
    [SerializeField] private GameObject _difficultyPanel;
    [Tooltip("게임 방법")]
    [SerializeField] private GameObject _howToPlayPanel;
    [Tooltip("게임 방법")]
    [SerializeField] private GameObject _encyclopdiaPanel;

    [Header("━━━━ 버튼 ━━━━")]
    [SerializeField] private GameObject _startButton;

    // 현재 선택된 맵과 난이도 저장용 (임시)
    private int _currentMapIndex = -1;
    private Difficulty _currentDifficulty = Difficulty.None;

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    private void RefreshText()
    {
        _combatText.text = LocalizationManager.Instance.Get("COMBAT");
        _startText.text = LocalizationManager.Instance.Get("START");
        _howToPlayText.text = LocalizationManager.Instance.Get("HOW_TO_PLAY");
        _unitsText.text = LocalizationManager.Instance.Get("ENCYCLOPEDIA");

        RefreshInfoText();
    }

    private void RefreshInfoText()
    {
        if (_currentDifficulty == Difficulty.None)
        {
            _infoText.text = "";
            return;
        }

        string key = $"{_currentDifficulty}_INFO";

        _infoText.text = LocalizationManager.Instance.Get(key);
    }

    // 환경 설정 버튼
    public void OnClickSettings()
    {
        _settingsPopup.SetActive(true);
    }

    // 메인 버튼
    public void OnClickMain()
    {
        GameSceneManager.Instance.LoadTitle();
    }

    // 전투 버튼 - 클릭하면 맵 선택 스크롤뷰 나옴
    public void OnClickCombat()
    {
        _howToPlayPanel.SetActive(false);
        _encyclopdiaPanel.SetActive(false);
        
        // 난이도 선택이 열려있을땐 다 닫음
        if (_difficultyPanel.activeSelf)
        {
            _difficultyPanel.SetActive(false);
            _mapScrollView.SetActive(false);
            // _combatOutline.SetActive(false);

            _currentMapIndex = -1;
            ResetDifficulty();
            return;
        }

        bool isActive = _mapScrollView.activeSelf;
        // _combatOutline.SetActive(!isActive);
        _mapScrollView.SetActive(!isActive);
        if (!isActive) SetDifficulty(Difficulty.None);
    }

    // 게임 방법 버튼
    public void OnClickHowToPlay()
    {
        _difficultyPanel.SetActive(false);
        _settingsPopup.SetActive(false);
        _mapScrollView.SetActive(false);
        _encyclopdiaPanel.SetActive(false);
        
        bool isActive = _howToPlayPanel.activeSelf;
        _howToPlayPanel.SetActive(!isActive);
    }

    // 맵 선택
    public void OnClickMap(int index)
    {
        _currentMapIndex = index;
        _mapScrollView.SetActive(false);
        _difficultyPanel.SetActive(true);
        _howToPlayPanel.SetActive(false);
        _encyclopdiaPanel.SetActive(false);
    }

    // 뒤로가기 - 난이도 선택에서 맵 선택으로
    public void OnClickBack()
    {
        _currentMapIndex = -1;
        ResetDifficulty();
        _difficultyPanel.SetActive(false);
        _mapScrollView.SetActive(true);
    }

    public void OnClickStart()
    {
        GameSceneManager.Instance.ChangeScene(GetSceneIndex());
    }

    public void OnClickEasy()
    {
        SetDifficulty(Difficulty.Easy);
    }

    public void OnClickNormal()
    {
        SetDifficulty(Difficulty.Normal);
    }

    public void OnClickHard()
    {
        SetDifficulty(Difficulty.Hard);
    }

    private void SetDifficulty(Difficulty difficulty)
    {
        if (_currentDifficulty == difficulty)
        {
            _currentDifficulty = Difficulty.None;
        }
        else
        {
            _currentDifficulty = difficulty;
        }
        UpdateDifficultyUI();
        RefreshInfoText();
    }

    private void ResetDifficulty()
    {
        _currentDifficulty = Difficulty.None;
        UpdateDifficultyUI();
    }

    private void UpdateDifficultyUI()
    {
        _easyOutline.SetActive(_currentDifficulty == Difficulty.Easy);
        _normalOutline.SetActive(_currentDifficulty == Difficulty.Normal);
        _hardOutline.SetActive(_currentDifficulty == Difficulty.Hard);

        _startButton.SetActive(_currentDifficulty != Difficulty.None);
    }

    private int GetSceneIndex()
        => (_currentMapIndex * 3) + (int)_currentDifficulty;
}

public enum Difficulty
{
    None,
    Easy = 2,
    Normal = 3,
    Hard = 4
}
