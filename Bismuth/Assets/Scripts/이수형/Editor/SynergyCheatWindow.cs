#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 시너지 테스트용 치트 에디터 창.
/// - 메뉴 : Tools / Synergy Cheat
/// - Play 모드에서 시너지 + 레벨 선택 후 Spawn 버튼을 누르면
///   SynergyCheatExecutor.ExecuteSynergyCheat() 이 호출되어
///   현재 보드를 비우고 해당 시너지/레벨에 맞는 서로 다른 유닛들을 한 번에 소환한다.
/// </summary>
public class SynergyCheatWindow : EditorWindow
{
    // ━━━━━━━━ 시너지 라벨/ID 테이블 ━━━━━━━━
    // SynergyManager.SynergyType enum 의 None 을 제외한 10개를 노출.
    // 한글 라벨은 SynergyManager.PrintSynergy() 의 switch 문과 동일하게 유지.
    private static readonly (string label, int id)[] SynergyEntries = new (string, int)[]
    {
        ("전사 (Warrior)",     (int)SynergyManager.SynergyType.Warrior),
        ("마법사 (Magician)",  (int)SynergyManager.SynergyType.Magician),
        ("궁수 (Archer)",      (int)SynergyManager.SynergyType.Archer),
        ("거너 (Gunner)",      (int)SynergyManager.SynergyType.Gunner),
        ("격투가 (Fighter)",   (int)SynergyManager.SynergyType.Fighter),
        ("인간 (Human)",       (int)SynergyManager.SynergyType.Human),
        ("엘프 (Elf)",         (int)SynergyManager.SynergyType.Elf),
        ("오크 (Orc)",         (int)SynergyManager.SynergyType.Orc),
        ("수인 (Furry)",       (int)SynergyManager.SynergyType.Furry),
        ("정령 (Spirit)",      (int)SynergyManager.SynergyType.Spirit),
    };

    // ━━━━━━━━ 상태 ━━━━━━━━
    private int _selectedSynergyIndex = 0;
    private int _selectedLevelIndex = 0;

    private string[] _synergyLabels;
    private string[] _levelLabels = new string[0];

    private int _cachedLevelsForSynergyId = -1; // 레벨 라벨 캐시 무효화용

    private bool _hasLastResult = false;
    private SynergyCheatExecutor.ExecuteResult _lastResult;
    private string _lastSynergyLabel = "";
    private int _lastLevelActiveCount = 0;

    [MenuItem("Tools/Synergy Cheat")]
    public static void Open()
    {
        SynergyCheatWindow window = GetWindow<SynergyCheatWindow>("Synergy Cheat");
        window.minSize = new Vector2(340f, 240f);
    }

    private void OnEnable()
    {
        // 시너지 라벨 배열 사전 빌드
        _synergyLabels = new string[SynergyEntries.Length];
        for (int i = 0; i < SynergyEntries.Length; i++)
            _synergyLabels[i] = SynergyEntries[i].label;

        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    /// <summary> Play 모드 진입/이탈 시 레벨 캐시 무효화 (SynergyManager 가 새로 뜨므로). </summary>
    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        _cachedLevelsForSynergyId = -1;
        _hasLastResult = false;
        Repaint();
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(4);

        bool isPlaying = EditorApplication.isPlaying;
        if (!isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Play 모드에서만 동작합니다. 씬을 실행한 뒤 Spawn 버튼을 누르세요.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(4);

        DrawSynergyDropdown();
        DrawLevelDropdown(isPlaying);

        EditorGUILayout.Space(8);

        DrawSpawnButton(isPlaying);

        EditorGUILayout.Space(8);

        DrawLastResult();
    }

    // ━━━━━━━━ UI 파트 ━━━━━━━━

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Synergy Cheat", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "시너지와 레벨을 선택하면 보드를 비우고\n해당 시너지를 가진 서로 다른 유닛을 필요한 수만큼 소환합니다.",
            EditorStyles.wordWrappedMiniLabel
        );
    }

    private void DrawSynergyDropdown()
    {
        int newIndex = EditorGUILayout.Popup("시너지", _selectedSynergyIndex, _synergyLabels);
        if (newIndex != _selectedSynergyIndex)
        {
            _selectedSynergyIndex = newIndex;
            _cachedLevelsForSynergyId = -1; // 시너지 바뀌면 레벨 라벨 재구성
            _selectedLevelIndex = 0;
        }
    }

    private void DrawLevelDropdown(bool isPlaying)
    {
        int currentSynergyId = SynergyEntries[_selectedSynergyIndex].id;

        // 레벨 라벨 캐시 : Play 중이고 시너지가 바뀌었을 때만 재조회
        if (isPlaying && _cachedLevelsForSynergyId != currentSynergyId)
        {
            int[] counts = SynergyCheatCore.GetLevelActiveCounts(currentSynergyId);
            if (counts == null || counts.Length == 0)
            {
                _levelLabels = new string[] { "레벨 데이터 없음" };
            }
            else
            {
                _levelLabels = new string[counts.Length];
                for (int i = 0; i < counts.Length; i++)
                    _levelLabels[i] = $"Lv.{i + 1}  ({counts[i]}마리)";
            }
            _cachedLevelsForSynergyId = currentSynergyId;
            if (_selectedLevelIndex >= _levelLabels.Length)
                _selectedLevelIndex = 0;
        }
        else if (!isPlaying)
        {
            _levelLabels = new string[] { "Play 모드에서 로드됩니다" };
        }

        using (new EditorGUI.DisabledScope(!isPlaying || _levelLabels.Length == 0))
        {
            _selectedLevelIndex = EditorGUILayout.Popup("레벨", _selectedLevelIndex, _levelLabels);
        }
    }

    private void DrawSpawnButton(bool isPlaying)
    {
        using (new EditorGUI.DisabledScope(!isPlaying))
        {
            if (GUILayout.Button("Spawn (보드 초기화 + 소환)", GUILayout.Height(32f)))
            {
                ExecuteCheat();
            }
        }
    }

    private void DrawLastResult()
    {
        if (!_hasLastResult)
            return;

        EditorGUILayout.LabelField("최근 실행 결과", EditorStyles.boldLabel);

        SynergyCheatExecutor.ExecuteResult r = _lastResult;

        if (r.aborted)
        {
            EditorGUILayout.HelpBox(
                $"{_lastSynergyLabel} - 소환할 유닛이 없어 보드를 건드리지 않고 종료했습니다.",
                MessageType.Warning
            );
            return;
        }

        string shortMark = r.isShort ? " (※ 유닛 부족)" : "";
        string body =
            $"{_lastSynergyLabel}  /  필요 {r.requiredCount}마리{shortMark}\n" +
            $"보드 비우기 : {r.despawnedCount} 디스폰\n" +
            $"소환 : 성공 {r.spawnedCount} / 시도 {r.requestedCount}" +
            (r.failedCount > 0 ? $"   (실패 {r.failedCount})" : "");

        MessageType mt =
            r.failedCount > 0 ? MessageType.Warning :
            r.isShort         ? MessageType.Warning :
                                MessageType.Info;

        EditorGUILayout.HelpBox(body, mt);
    }

    // ━━━━━━━━ 실행 ━━━━━━━━

    private void ExecuteCheat()
    {
        int synergyId = SynergyEntries[_selectedSynergyIndex].id;
        int levelIndex = _selectedLevelIndex;

        _lastResult = SynergyCheatExecutor.ExecuteSynergyCheat(synergyId, levelIndex);
        _hasLastResult = true;
        _lastSynergyLabel = SynergyEntries[_selectedSynergyIndex].label;
        _lastLevelActiveCount = _lastResult.requiredCount;

        Repaint();
    }
}
#endif
