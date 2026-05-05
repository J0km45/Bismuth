using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlPanelUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("레벨")]
    [SerializeField] private TMP_Text _levelText;
    [Tooltip("배치한 타일 수 / 현재 최대 배치 가능 수")]
    [SerializeField] private TMP_Text _timeText;
    [Tooltip("재화")]
    [SerializeField] private TMP_Text _goldText;
    [Tooltip("합성소")]
    [SerializeField] private TMP_Text _combinationText;
    [Tooltip("시너지")]
    [SerializeField] private TMP_Text _synergyText;
    [Tooltip("일반 뽑기")]
    [SerializeField] private TMP_Text _drawText;
    [Tooltip("업그레이드")]
    [SerializeField] private TMP_Text _upgradeText;
    [Tooltip("업그레이드 소모 비용")]
    [SerializeField] private TMP_Text _upgradeGoldText;
    [Header("━━━━ 경고 문구 ━━━━")]
    [SerializeField] private TMP_Text _warningText;
    [SerializeField] private float _duration;

    [Header("━━━━ 패널 ━━━━")]
    [Tooltip("합성소")]
    [SerializeField] private GameObject _combinationScrollView;

    private PlayerDataManager _playerData;
    private PlayerUIController _playerUIController;
    private Coroutine _coroutine;

    private bool _isCombinationSVOpened => _combinationScrollView.activeSelf;

    private BoardSystem _boardSystem;
    private PlayerAction _playerAction;

    private void Awake()
    {
        if (_playerData == null)
        {
            _playerData = FindAnyObjectByType<PlayerDataManager>();
        }
        if (_playerUIController == null)
        {
            _playerUIController = FindAnyObjectByType<PlayerUIController>();
        }

        if (_boardSystem == null)
        {
            _boardSystem = FindAnyObjectByType<BoardSystem>();
        }

        if (_playerAction == null)
        {
            _playerAction = new PlayerAction();
        }
    }

    private void OnEnable()
    {
        _playerAction.Enable();

        _playerAction.UI.Combination.performed += OnClickCombination;

        if (_playerData != null)
        {
            _playerData.OnPlacementUpgradeLevelChanged += RefreshLevel;
            _playerData.OnPlacementUpgradeLevelChanged += RefreshUpgradeGold;
            _playerData.OnPlaceableTileCountChanged += RefreshPlacementTileCount;
        }

        if (_boardSystem != null)
            _boardSystem.OnPlacedTileCountChanged += RefreshPlacementTileCount;
    }

    private void Start()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        RefreshText();
        RefreshLevel();
        RefreshUpgradeGold();
        RefreshPlacementTileCount();
    }

    private void OnDisable()
    {
        _playerAction.UI.Combination.performed -= OnClickCombination;
        
        _playerAction.Disable();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;

        if (_playerData != null)
        {
            _playerData.OnPlacementUpgradeLevelChanged -= RefreshLevel;
            _playerData.OnPlacementUpgradeLevelChanged -= RefreshUpgradeGold;
            _playerData.OnPlaceableTileCountChanged -= RefreshPlacementTileCount;
        }

        if (_boardSystem != null)
            _boardSystem.OnPlacedTileCountChanged -= RefreshPlacementTileCount;
    }

    private void RefreshText()
    {
        _combinationText.text = LocalizationManager.Instance.Get("MERGE");
        _synergyText.text = LocalizationManager.Instance.Get("SYNERGY");
        _drawText.text = LocalizationManager.Instance.Get("SUMMON");
        _upgradeText.text = LocalizationManager.Instance.Get("LEVEL_UP");

        RefreshGold();
    }

    private void Update()
    {
        RefreshPlacementTileCount();
    }

    private void RefreshPlacementTileCount(int _ = 0)
    {
        RefreshPlacementTileCount();
    }

    private void RefreshPlacementTileCount()
    {
        if (_timeText == null || _playerData == null)
            return;

        if (_boardSystem == null)
            _boardSystem = BoardSystem.Instance != null ? BoardSystem.Instance : FindAnyObjectByType<BoardSystem>();

        int placedTileCount = 0;

        if (_boardSystem != null)
            placedTileCount = _boardSystem.CurrentPlacedTileCount;

        _timeText.text = $"{placedTileCount} / {_playerData.PlaceableTileCount}";
    }

    public void RefreshLevel()
    {
        if (_playerData == null)
            return;

        _levelText.text = $"Lv {_playerData.PlacementUpgradeLevel}";
    }

    public void RefreshGold()
    {
        if (_playerData == null)
            return;

        _goldText.text = $": {_playerData.Gold}";
    }

    public void RefreshUpgradeGold()
    {
        if (_playerData == null || _playerUIController == null)
            return;

        int level = _playerData.PlacementUpgradeLevel;
        int gold = _playerUIController.GetUpgradeGold(level);

        if(gold < 0)
        {
            _upgradeGoldText.text = "MAX";
        }
        else
        {
            _upgradeGoldText.text = $"{gold}";
        }
    }

    public void ShowWarningText(string text)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(ShowWarningCoroutine(text));
    }

    private IEnumerator ShowWarningCoroutine(string message)
    {
        _warningText.gameObject.SetActive(true);
        _warningText.text = message;
        Color color = _warningText.color;
        float time = 0f;

        while(time < _duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / _duration;
            color.a = Mathf.Lerp(1f, 0f, t);
            _warningText.color = color;

            yield return null;
        }
        _warningText.gameObject.SetActive(false);
    }

    public void OnClickCombination(InputAction.CallbackContext ctx)
    {
        if(ctx.performed)
            OnClickCombination();
    }

    public void OnClickCombination()
    {
        if (!_isCombinationSVOpened) _combinationScrollView.transform.SetAsLastSibling();
        _combinationScrollView.SetActive(!_isCombinationSVOpened);
    }
}
