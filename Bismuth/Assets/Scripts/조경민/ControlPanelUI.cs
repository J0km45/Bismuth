using TMPro;
using UnityEngine;

public class ControlPanelUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("레벨")]
    [SerializeField] private TMP_Text _levelText;
    [Tooltip("시간")]
    [SerializeField] private TMP_Text _timeText;
    [Tooltip("재화")]
    [SerializeField] private TMP_Text _goldText;
    [Tooltip("합성소")]
    [SerializeField] private TMP_Text _combinationText;
    [Tooltip("시너지")]
    [SerializeField] private TMP_Text _synergyText;
    [Tooltip("일반 뽑기")]
    [SerializeField] private TMP_Text _drawText;
    [Tooltip("확률 +")]
    [SerializeField] private TMP_Text _upgradeText;

    [Header("━━━━ 패널 ━━━━")]
    [Tooltip("합성소")]
    [SerializeField] private GameObject _combinationScrollView;

    private PlayerDataManager _playerData;
    private bool _isCombinationSVOpened => _combinationScrollView.activeSelf;
    private float _elapsedTime = 0f; // 누적 시간 저장용

    private void Awake()
    {
        if (_playerData == null)
        {
            _playerData = FindAnyObjectByType<PlayerDataManager>();
        }
    }

    private void Start()
    {
        // TODO : 수정해야됨(로컬라이징)
        _combinationText.text = "Combination";
        _drawText.text = "Draw";
        _upgradeText.text = "Upgrade";

        RefreshLevel();
        RefreshGold();
    }

    private void Update()
    {
        UpdateElapsedTime();
    }

    private void UpdateElapsedTime()
    {
        if (Time.timeScale > 0f)
        {
            _elapsedTime += Time.unscaledDeltaTime;
        }
        int minutes = (int)(_elapsedTime / 60);
        int seconds = (int)(_elapsedTime % 60);
        _timeText.text = $"{minutes:D2} : {seconds:D2}";
    }

    // PlayerDataManager에서 이걸 호출 하거나 PlayerDataManager에 이벤트를 만들어서 이걸 구독하는 방식으로 연결 필요
    public void RefreshLevel()
    {
        if (_playerData == null)
            return;

        _levelText.text = $"Lv {_playerData.Level}";
    }

    public void RefreshGold()
    {
        if (_playerData == null)
            return;

        _goldText.text = $"Gold : {_playerData.Gold}";
    }

    public void OnClickCombination()
    {
        if (!_isCombinationSVOpened) _combinationScrollView.transform.SetAsLastSibling();
        _combinationScrollView.SetActive(!_isCombinationSVOpened);
    }
}
