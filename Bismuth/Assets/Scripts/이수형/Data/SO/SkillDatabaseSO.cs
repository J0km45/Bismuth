using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체에서 사용하는 SkillDataSO들을 한 곳에 모은 마스터 데이터베이스.
/// 빌드에 직접 참조 형태로 포함되며, 런타임에는 ID로 SO를 조회하는 용도로 사용.
///
/// 리스트 갱신은 SkillDataSOGenerator(에디터 도구)가 자동으로 수행.
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Data/Skill/Skill Database")]
public class SkillDatabaseSO : ScriptableObject
{
    [Header("====전체 스킬 SO====")]
    [Tooltip("SkillDataSOGenerator로 자동 등록되는 스킬 SO 목록")]
    [SerializeField] private List<SkillDataSO> _allSkills = new();

    private Dictionary<int, SkillDataSO> _lookupCache;

    public IReadOnlyList<SkillDataSO> AllSkills => _allSkills;
    public int Count => _allSkills.Count;

    /// <summary>
    /// 스킬 ID로 SkillDataSO를 조회. 첫 호출 시 ID→SO 사전을 한 번 빌드.
    /// </summary>
    public bool TryGetById(int skillId, out SkillDataSO skillData)
    {
        EnsureLookup();
        return _lookupCache.TryGetValue(skillId, out skillData);
    }

    private void EnsureLookup()
    {
        if (_lookupCache != null)
            return;

        _lookupCache = new Dictionary<int, SkillDataSO>();

        foreach (SkillDataSO so in _allSkills)
        {
            if (so == null)
                continue;

            if (_lookupCache.ContainsKey(so.Id))
            {
                DebugTool.Warnning(
                    $"[SkillDatabaseSO] 중복 스킬 ID 발견. ID: {so.Id}",
                    DebugType.Data, this);
                continue;
            }

            _lookupCache[so.Id] = so;
        }
    }

    private void OnDisable()
    {
        // 도메인 리로드 등으로 SO가 재초기화될 때 캐시 무효화
        _lookupCache = null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 도구 전용 - 전체 스킬 SO 리스트를 갱신한다.
    /// </summary>
    public void EditorSetAllSkills(List<SkillDataSO> skills)
    {
        _allSkills = skills ?? new List<SkillDataSO>();
        _lookupCache = null;
    }
#endif
}
