using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 유닛 한 기의 가변 스탯 관리 허브.
// - Base 값 저장 + StatModifier 리스트 보관
// - Get(StatType) 호출 시 실시간 계산 : (Base + ΣFlat) × (1 + ΣPercentAdd) × Π(1 + PercentMul)
// - Add / Remove / RemoveBySource / RemoveAll 로 언제든 모디파이어 중도 제거 가능
// - 변경 시 OnStatChanged(StatType) 이벤트 발행
//
// ※ 여기서 말하는 "허브"는 배치 보드(BoardSystem)와 무관. 한 유닛의 스탯 상태를 모아두는 컴포넌트일 뿐.
// 아직 어떤 소비자도 이 컴포넌트를 읽지 않는다. (1단계 : 뼈대만)
public class UnitStatHub : MonoBehaviour
{
    [SerializeField] private bool _log = true;

    // StatType → Base 값
    private readonly Dictionary<StatType, float> _base = new();

    // StatType → 해당 스탯에 걸린 모디파이어 리스트
    private readonly Dictionary<StatType, List<StatModifier>> _mods = new();

    public event Action<StatType> OnStatChanged;

    // ─────────────────────────────────────────────────────────
    // 인스펙터 디버그 노출 (Dictionary 는 직렬화 안 되므로 List 로 미러링)
    // _baseInspector  : 편집 가능. 플레이 중 OnValidate 가 _base 로 동기화 + NotifyChanged.
    // _modsInspector  : Read-only 용. 매 변경 시점에 _mods 로부터 재구성하여 사용자 편집은 덮어씀.
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct BaseEntry
    {
        public StatType Type;
        public float Value;
    }

    [Serializable]
    public struct ModifierEntry
    {
        public StatType Target;
        public StatOperation Op;
        public float Value;
        public ModifierSource Source;
        public string Key; // object Key 를 string 으로 가시화
    }

    [Header("==== 인스펙터 디버그 (편집 가능: Base) ====")]
    [Tooltip("Base 값. 플레이 중 인스펙터에서 변경하면 즉시 SetBase 와 동일하게 반영된다 (NotifyChanged 발행).")]
    [SerializeField] private List<BaseEntry> _baseInspector = new();

    [Header("==== 인스펙터 디버그 (Read-Only: 현재 모디파이어) ====")]
    [Tooltip("현재 적용된 모든 모디파이어 표시. 인스펙터에서 편집해도 다음 변경 시점에 덮어써짐.")]
    [SerializeField] private List<ModifierEntry> _modsInspector = new();

    // ───────── Base 값 ─────────

    public void SetBase(StatType type, float value)
    {
        _base[type] = value;
        SyncBaseInspector();
        NotifyChanged(type);

        if (_log)
            DebugTool.Log($"[StatHub] SetBase {type} = {value}", DebugType.Unit, this);
    }

    public float GetBase(StatType type)
    {
        return _base.TryGetValue(type, out float v) ? v : 0f;
    }

    // ───────── 모디파이어 추가 ─────────

    public void Add(StatModifier mod)
    {
        if (mod == null)
        {
            DebugTool.Warnning("[StatHub] null StatModifier 추가 시도", DebugType.Unit, this);
            return;
        }

        if (!_mods.TryGetValue(mod.Target, out List<StatModifier> list))
        {
            list = new List<StatModifier>();
            _mods[mod.Target] = list;
        }

        list.Add(mod);
        SyncModsInspector();
        NotifyChanged(mod.Target);

        if (_log)
            DebugTool.Log($"[StatHub] Add {mod}", DebugType.Unit, this);
    }

    // ───────── 모디파이어 제거 ─────────

    // Key 단위 제거. 같은 Key 가 여러 StatType 에 걸린 경우 모두 제거한다.
    public bool Remove(object key)
    {
        if (key == null) return false;

        bool removed = false;
        foreach (var kvp in _mods)
        {
            List<StatModifier> list = kvp.Value;
            bool changedHere = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (Equals(list[i].Key, key))
                {
                    if (_log)
                        DebugTool.Log($"[StatHub] Remove {list[i]}", DebugType.Unit, this);

                    list.RemoveAt(i);
                    changedHere = true;
                    removed = true;
                }
            }

            if (changedHere)
                NotifyChanged(kvp.Key);
        }

        if (removed)
            SyncModsInspector();

        return removed;
    }

    // Source 단위 일괄 제거. 예) UnitEnhance 전체 리셋, SynergyEnhance 재적용 전 싹 제거 등.
    public int RemoveBySource(ModifierSource source)
    {
        int total = 0;

        foreach (var kvp in _mods)
        {
            List<StatModifier> list = kvp.Value;
            int before = list.Count;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Source == source)
                    list.RemoveAt(i);
            }

            int removed = before - list.Count;
            if (removed > 0)
            {
                total += removed;
                NotifyChanged(kvp.Key);
            }
        }

        if (total > 0)
            SyncModsInspector();

        if (_log && total > 0)
            DebugTool.Log($"[StatHub] RemoveBySource {source} : {total} 개 제거", DebugType.Unit, this);

        return total;
    }

    // 특정 스탯에 걸린 모디파이어 전체 제거.
    public int RemoveAll(StatType type)
    {
        if (!_mods.TryGetValue(type, out List<StatModifier> list) || list.Count == 0)
            return 0;

        int count = list.Count;
        list.Clear();
        SyncModsInspector();
        NotifyChanged(type);

        if (_log)
            DebugTool.Log($"[StatHub] RemoveAll {type} : {count} 개 제거", DebugType.Unit, this);

        return count;
    }

    // ───────── 최종값 조회 ─────────

    // 최종값 = (Base + ΣFlat) × (1 + ΣPercentAdd) × Π(1 + PercentMul)
    public float Get(StatType type)
    {
        return CalculateExcluding(type, null);
    }

    // 내부 계산 : exclude(m) == true 인 모디파이어를 제외하고 최종값을 낸다.
    // exclude 가 null 이면 전부 포함 (= Get 과 동일).
    private float CalculateExcluding(StatType type, Func<StatModifier, bool> exclude)
    {
        float baseValue = GetBase(type);

        if (!_mods.TryGetValue(type, out List<StatModifier> list) || list.Count == 0)
            return baseValue;

        float flat = 0f;
        float percentAddSum = 0f;
        float percentMulProduct = 1f;

        for (int i = 0; i < list.Count; i++)
        {
            StatModifier m = list[i];
            if (exclude != null && exclude(m))
                continue;

            switch (m.Op)
            {
                case StatOperation.Flat:
                    flat += m.Value;
                    break;
                case StatOperation.PercentAdd:
                    percentAddSum += m.Value;
                    break;
                case StatOperation.PercentMul:
                    percentMulProduct *= (1f + m.Value);
                    break;
            }
        }

        return (baseValue + flat) * (1f + percentAddSum) * percentMulProduct;
    }

    // ───────── 기여도 조회 (UI 분해 표시용) ─────────
    //
    // 정의 : "이 소스(또는 키)가 없었다면 줄어들었을 양" = Get(type) - <제외 계산>
    // PercentAdd / PercentMul 이 섞여도 의미가 일관된 차분(counterfactual) 방식.
    // 예) Base=100, UnitEnhance +20%, SynergySkill +30% → 최종 150.
    //     GetContribution(AttackPower, UnitEnhance)  == 20
    //     GetContribution(AttackPower, SynergySkill) == 30

    /// <summary> 해당 Source 에 속한 모든 모디파이어를 제외한 가상 최종값. </summary>
    public float GetWithout(StatType type, ModifierSource source)
    {
        return CalculateExcluding(type, m => m.Source == source);
    }

    /// <summary> 해당 Source 가 더해준 순수 기여량 (= Get - GetWithout). </summary>
    public float GetContribution(StatType type, ModifierSource source)
    {
        return Get(type) - GetWithout(type, source);
    }

    /// <summary> 특정 Key 를 제외한 가상 최종값. </summary>
    public float GetWithoutKey(StatType type, object key)
    {
        if (key == null) return Get(type);
        return CalculateExcluding(type, m => Equals(m.Key, key));
    }

    /// <summary> 특정 Key 가 더해준 순수 기여량. (예: "SynergySkill_Gunner" 하나만) </summary>
    public float GetContributionByKey(StatType type, object key)
    {
        return Get(type) - GetWithoutKey(type, key);
    }

    /// <summary>
    /// 해당 스탯에 기여 중인 모든 Source → 기여량 맵을 한 번에 반환.
    /// UI 에서 "강화 +X / 시너지 +Y / 시너지강화 +Z" 같은 표시에 사용.
    /// (모디파이어가 하나도 없는 Source 는 맵에 포함되지 않는다.)
    /// </summary>
    public Dictionary<ModifierSource, float> GetContributionsBySource(StatType type)
    {
        Dictionary<ModifierSource, float> result = new();

        if (!_mods.TryGetValue(type, out List<StatModifier> list) || list.Count == 0)
            return result;

        // 이 스탯에 실제로 등장하는 Source 만 수집
        HashSet<ModifierSource> sources = new();
        for (int i = 0; i < list.Count; i++)
            sources.Add(list[i].Source);

        float total = Get(type);
        foreach (ModifierSource src in sources)
            result[src] = total - GetWithout(type, src);

        return result;
    }

    // ───────── 디버그 ─────────

    // 어떤 모디파이어가 어떻게 적용되는지 한눈에 보기 위한 문자열.
    public string Breakdown(StatType type)
    {
        float baseValue = GetBase(type);
        StringBuilder sb = new();
        sb.AppendLine($"── {type} Breakdown ──");
        sb.AppendLine($"Base : {baseValue}");

        if (!_mods.TryGetValue(type, out List<StatModifier> list) || list.Count == 0)
        {
            sb.AppendLine("(모디파이어 없음)");
            sb.AppendLine($"Final : {baseValue}");
            return sb.ToString();
        }

        float flat = 0f;
        float percentAddSum = 0f;
        float percentMulProduct = 1f;

        for (int i = 0; i < list.Count; i++)
        {
            StatModifier m = list[i];
            sb.AppendLine($"  · {m}");

            switch (m.Op)
            {
                case StatOperation.Flat: flat += m.Value; break;
                case StatOperation.PercentAdd: percentAddSum += m.Value; break;
                case StatOperation.PercentMul: percentMulProduct *= (1f + m.Value); break;
            }
        }

        float final = (baseValue + flat) * (1f + percentAddSum) * percentMulProduct;
        sb.AppendLine($"ΣFlat = {flat}, ΣPercentAdd = {percentAddSum * 100f:+0.##;-0.##;0}%, Π(1+PercentMul) = ×{percentMulProduct:F3}");
        sb.AppendLine($"Final : {final}");
        return sb.ToString();
    }

    private void NotifyChanged(StatType type)
    {
        OnStatChanged?.Invoke(type);
    }

    // ─────────────────────────────────────────────────────────
    // 인스펙터 동기화 헬퍼
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// _base Dictionary → _baseInspector List 동기화.
    /// 기존 항목은 값만 갱신하여 사용자 편집 중 항목 순서를 흔들지 않는다.
    /// </summary>
    private void SyncBaseInspector()
    {
        foreach (var kvp in _base)
        {
            bool found = false;
            for (int i = 0; i < _baseInspector.Count; i++)
            {
                if (_baseInspector[i].Type == kvp.Key)
                {
                    _baseInspector[i] = new BaseEntry { Type = kvp.Key, Value = kvp.Value };
                    found = true;
                    break;
                }
            }

            if (!found)
                _baseInspector.Add(new BaseEntry { Type = kvp.Key, Value = kvp.Value });
        }
    }

    /// <summary>
    /// _mods Dictionary → _modsInspector List 재구성. (Read-only 표시 전용)
    /// </summary>
    private void SyncModsInspector()
    {
        _modsInspector.Clear();

        foreach (var kvp in _mods)
        {
            List<StatModifier> list = kvp.Value;
            for (int i = 0; i < list.Count; i++)
            {
                StatModifier mod = list[i];
                _modsInspector.Add(new ModifierEntry
                {
                    Target = mod.Target,
                    Op = mod.Op,
                    Value = mod.Value,
                    Source = mod.Source,
                    Key = mod.Key?.ToString() ?? "(null)"
                });
            }
        }
    }

    /// <summary>
    /// 인스펙터에서 _baseInspector 값을 변경하면 호출됨.
    /// 플레이 중에만 동작하여 _base Dictionary 동기화 + NotifyChanged 발행.
    /// _modsInspector 는 사용자 편집을 항상 덮어씀 (read-only 의도).
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        for (int i = 0; i < _baseInspector.Count; i++)
        {
            BaseEntry entry = _baseInspector[i];

            if (!_base.TryGetValue(entry.Type, out float existing) || !Mathf.Approximately(existing, entry.Value))
            {
                _base[entry.Type] = entry.Value;
                NotifyChanged(entry.Type);

                if (_log)
                    DebugTool.Log($"[StatHub] (Inspector) SetBase {entry.Type} = {entry.Value}", DebugType.Unit, this);
            }
        }

        // 사용자가 ModifierEntry 를 만졌어도 다음 변경 시점에 어차피 덮어씀.
        // 일관성을 위해 즉시 재구성.
        SyncModsInspector();
    }
}
