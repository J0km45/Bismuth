using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 시너지 강화 디버그 UI (임시).
// - 드롭다운으로 시너지 선택
// - 선택된 시너지의 현재 레벨 / 다음 강화 비용 표시
// - 강화 버튼 클릭 시 SynergyEnhanceLevelManager.TryEnhance 호출
//
// 실제 출시 UI 는 별도 작업 (조경민). 본 패널은 검증/조정 용도.
//
// 인스펙터 연결 :
//   - Dropdown    : TMP_Dropdown (시너지 선택)
//   - LevelText   : TMP_Text     (현재 Lv. X / Y)
//   - CostText    : TMP_Text     (강화 (-NG) 또는 MAX)
//   - EnhanceButton : Button     (클릭 시 강화 시도)
public class SynergyEnhanceDebugPanel : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SynergyEnhanceLevelManager _levelManager;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown _dropdown;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private Button _enhanceButton;

    // 시트의 name 컬럼 (한글) 기준. 드롭다운 표시 라벨.
    private static readonly (int synergyId, string label)[] Entries = new[]
    {
        ((int)SynergyManager.SynergyType.Warrior,  "전사"),
        ((int)SynergyManager.SynergyType.Magician, "마법사"),
        ((int)SynergyManager.SynergyType.Archer,   "궁수"),
        ((int)SynergyManager.SynergyType.Gunner,   "거너"),
        ((int)SynergyManager.SynergyType.Fighter,  "격투가"),
        ((int)SynergyManager.SynergyType.Human,    "인간"),
        ((int)SynergyManager.SynergyType.Elf,      "엘프"),
        ((int)SynergyManager.SynergyType.Orc,      "오크"),
        ((int)SynergyManager.SynergyType.Furry,    "수인"),
        ((int)SynergyManager.SynergyType.Spirit,   "정령"),
    };

    private void Awake()
    {
        if (_levelManager == null)
            _levelManager = FindAnyObjectByType<SynergyEnhanceLevelManager>();
    }

    private void Start()
    {
        // 드롭다운 옵션 세팅
        if (_dropdown != null)
        {
            _dropdown.ClearOptions();

            List<string> options = new List<string>(Entries.Length);
            for (int i = 0; i < Entries.Length; i++)
                options.Add(Entries[i].label);

            _dropdown.AddOptions(options);
            _dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        if (_enhanceButton != null)
            _enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);

        if (_levelManager != null)
            _levelManager.OnEnhanceLevelChanged += HandleLevelChanged;

        Refresh();
    }

    private void OnDestroy()
    {
        if (_dropdown != null)
            _dropdown.onValueChanged.RemoveListener(OnDropdownChanged);

        if (_enhanceButton != null)
            _enhanceButton.onClick.RemoveListener(OnEnhanceButtonClicked);

        if (_levelManager != null)
            _levelManager.OnEnhanceLevelChanged -= HandleLevelChanged;
    }

    // ───────── 핸들러 ─────────

    private void OnDropdownChanged(int _)
    {
        Refresh();
    }

    private void OnEnhanceButtonClicked()
    {
        int synergyId = GetSelectedSynergyId();
        if (synergyId <= 0) return;

        if (_levelManager == null)
        {
            DebugTool.Warnning("[SynergyEnhanceDebugPanel] LevelManager 가 없어 강화를 시도할 수 없습니다.", DebugType.Synergy, this);
            return;
        }

        bool ok = _levelManager.TryEnhance(synergyId);

        // 실패해도 LevelManager 가 사유를 로그로 남김. 여기선 추가 처리 X.
        // 성공 시 OnEnhanceLevelChanged → HandleLevelChanged 경로로 자동 Refresh.
        if (!ok)
        {
            DebugTool.Log(
                $"[SynergyEnhanceDebugPanel] 강화 실패 | synergyId={synergyId}",
                DebugType.Synergy, this);
        }
    }

    private void HandleLevelChanged(Dictionary<int, int> _)
    {
        Refresh();
    }

    // ───────── 표시 갱신 ─────────

    private int GetSelectedSynergyId()
    {
        if (_dropdown == null) return 0;
        int idx = _dropdown.value;
        if (idx < 0 || idx >= Entries.Length) return 0;
        return Entries[idx].synergyId;
    }

    private void Refresh()
    {
        int synergyId = GetSelectedSynergyId();
        if (synergyId <= 0) return;

        int currentLevel = _levelManager != null ? _levelManager.GetLevel(synergyId) : 0;
        int maxLevel = _levelManager != null ? _levelManager.MaxLevel : 5;

        if (_levelText != null)
            _levelText.text = $"현재 Lv. {currentLevel} / {maxLevel}";

        bool atMax = currentLevel >= maxLevel;

        if (_costText != null)
        {
            if (atMax)
            {
                _costText.text = "MAX";
            }
            else
            {
                int cost = _levelManager != null
                    ? _levelManager.CalculateEnhanceCost(synergyId, currentLevel)
                    : 0;
                _costText.text = $"강화 (-{cost}G)";
            }
        }

        if (_enhanceButton != null)
            _enhanceButton.interactable = !atMax;
    }
}
