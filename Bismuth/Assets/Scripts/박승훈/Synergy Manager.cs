using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SynergyManager : MonoBehaviour
{
    [SerializeField] private SummonUnit _summonUnit;

    [Header("시너지 데이터 SO (ID → SynergyData 조회용)")]
    [SerializeField] private SynergySO _synergySO;

    public UnityEvent<UnitStat> OnUnitCreated;
    public UnityEvent<UnitStat> OnUnitRemoved;
    public event Action<Dictionary<int, List<int>>> OnSynergyChanged;

    // key = 시너지 ID, value = 해당 시너지를 보유한 유닛의 개수
    private Dictionary<int, List<int>> synergiesDict = new();

    // key = 시너지 ID, value = SynergySO 의 SynergyData 참조 (Awake 에서 빌드)
    private Dictionary<int, SynergyData> _dataById = new();

    // 누락된 ID 경고가 스팸되지 않도록 한 번만 찍게 하는 집합
    private HashSet<int> _warnedMissingIds = new();

    [SerializeField] private int _unitCount = 0;

    [SerializeField] private bool _log = true;

    private void Awake()
    {
        _summonUnit = GetComponent<SummonUnit>();
        BuildDataCache();
    }

    private void OnEnable()
    {
        OnUnitCreated.AddListener(ChangedSynergy);
        OnUnitRemoved.AddListener(RemoveSynergy);
    }

    private void OnDisable()
    {
        OnUnitCreated.RemoveListener(ChangedSynergy);
        OnUnitRemoved.RemoveListener(RemoveSynergy);
    }

    private void Start()
    {
        DebugTool.DebugSelect(DebugType.Synergy, _log);
    }

    public void ChangedSynergy(UnitStat stat)
    {
        _unitCount++;
        for (int i = 0; i < stat.SynergIDs.Length; i++)
        {
            if (stat.SynergIDs[i] == 0)
                break;
            
            // 딕셔너리에 같은 시너지 ID가 없을 경우
            if (!synergiesDict.ContainsKey(stat.SynergIDs[i]))
            {
                List<int> list = new();
                list.Add(stat.Id);
                // 해당 시너지를 보유한 유닛 추가
                synergiesDict.Add(stat.SynergIDs[i], list);
            }

            // 딕셔너리에 같은 시너지 ID가 있을 경우 해당 유닛이 이미 추가되었는 지 중복 판단
            else
            {
                // 한 유닛에 같은 시너지가 있을 경우
                if (i > 1 && stat.SynergIDs[i] == stat.SynergIDs[i - 1])
                {
                    synergiesDict[stat.SynergIDs[i]].Add(stat.Id);
                }
                // 이미 있을 경우 넘기기
                if (synergiesDict[stat.SynergIDs[i]].Contains(stat.Id))
                    break;
                // 없을 경우 추가               
                synergiesDict[stat.SynergIDs[i]].Add(stat.Id);
            }
        }

        PrintSynergy();
        OnSynergyChanged?.Invoke(synergiesDict);
    }

    public void RemoveSynergy(UnitStat stat)
    {
        StringBuilder log = new();
        log.AppendLine("[시너지 제거 시작]");
        int idDuplication = 0;
        int id = stat.Id;
        _unitCount--;

        log.AppendLine($"제거 대상 ID : {stat.Id}, 소유 타워 개수 : {_summonUnit.OwnedTowers.Count}");
        for (int i = 0; i < _summonUnit.OwnedTowers.Count; i++)
        {
            log.Append($"탐색 ID : {_summonUnit.OwnedTowers[i].Id}");
            if (_summonUnit.OwnedTowers[i].Id == id)
            {
                idDuplication++;
            }
        }

        log.AppendLine($"중첩 개수 : {idDuplication}");

        if (idDuplication >= 2)
        {
            log.AppendLine($"중복 개수 : {idDuplication} 시너지 제거 불필요");
            DebugTool.Log(log.ToString(), DebugType.Synergy, this);
            return;
        }
        
        log.AppendLine($"중복 개수 : {idDuplication} 시너지 제거 필요");
        DebugTool.Log(log.ToString(), DebugType.Synergy, this);
        
        for (int i = 0; i < stat.SynergIDs.Length; i++)
        {
            if (stat.SynergIDs[i] == 0)
            {
                DebugTool.Log("존재하지 않는 시너지 입니다.", DebugType.Synergy, this);
                break;
            }

            if (synergiesDict.ContainsKey(stat.SynergIDs[i]))
            {
                DebugTool.Log($"시너지 ID : {synergiesDict[stat.SynergIDs[i]].Count} 개 ", DebugType.Synergy, this);
                synergiesDict[stat.SynergIDs[i]].Remove(stat.Id);
            }

            if (synergiesDict[stat.SynergIDs[i]].Count == 0)
            {
                DebugTool.Log($"시너지 ID : {stat.SynergIDs[i]} 삭제됨", DebugType.Synergy, this);
                synergiesDict.Remove(stat.SynergIDs[i]);
            }
        }
        
        PrintSynergy();
        OnSynergyChanged?.Invoke(synergiesDict);
    }

    private void PrintSynergy()
    {
        string log = $"[보유 유닛 수] : {_unitCount}\n[보유 시너지]\n";

        foreach (var value in synergiesDict)
        {
            switch (value.Key)
            {
                case (int)SynergyType.Warrior :
                    log += $"시너지 : 전사 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Magician :
                    log += $"시너지 : 마법사 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Archer :
                    log += $"시너지 : 궁수 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Gunner :
                    log += $"시너지 : 거너 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Fighter :
                    log += $"시너지 : 격투가 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Human :
                    log += $"시너지 : 인간 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Elf :
                    log += $"시너지 : 엘프 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Orc :
                    log += $"시너지 : 오크 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Furry :
                    log += $"시너지 : 수인 | 활성 개수 : {value.Value.Count}\n";
                    break;
                case (int)SynergyType.Spirit :
                    log += $"시너지 : 정령 | 활성 개수 : {value.Value.Count}\n";
                    break;
            }
        }
        
        DebugTool.Log(log, DebugType.Synergy, this);
    }

    public int GetSynergyLevel(int synergyType)
    {
        return synergiesDict.ContainsKey(synergyType) ? synergiesDict[synergyType].Count : 0;
    }

    // ───────── 시너지 데이터 조회 API ─────────
    // synergySO(에셋) + synergiesDict(현재 활성 수) 를 조합해,
    // 호출자는 시너지 ID 하나만 알면 현재 효과값까지 곧바로 얻을 수 있다.
    // 내부 위임 : SynergyManager → SynergyData(Layer1) 의 매칭 로직.

    /// <summary> ID 에 해당하는 SynergyData 를 반환. 없으면 null (경고 1회 출력). </summary>
    public SynergyData GetSynergyData(int id)
    {
        EnsureDataCache();

        if (_dataById.TryGetValue(id, out SynergyData data))
            return data;

        if (_warnedMissingIds.Add(id))
            DebugTool.Warnning($"[시너지] ID {id} 의 데이터를 찾을 수 없습니다.", DebugType.Data, this);

        return null;
    }

    /// <summary> 현재 활성 개수에 매칭되는 효과값을 반환. 없으면 0. </summary>
    public float GetEffectValue(int id, int effectIndex = 0)
    {
        TryGetEffectValue(id, effectIndex, out float value);
        return value;
    }

    /// <summary> 효과값 조회 + 성공 여부 반환. 0 과 "효과 없음" 을 구분해야 할 때 사용. </summary>
    public bool TryGetEffectValue(int id, int effectIndex, out float value)
    {
        value = 0f;

        SynergyData data = GetSynergyData(id);
        if (data == null)
            return false;

        int activeCount = GetSynergyLevel(id);
        return data.TryGetEffectValue(activeCount, effectIndex, out value);
    }

    /// <summary> 해당 시너지가 현재 1단계라도 활성 상태인지 여부. </summary>
    public bool IsSynergyActive(int id)
    {
        SynergyData data = GetSynergyData(id);
        if (data == null)
            return false;

        return data.IsActive(GetSynergyLevel(id));
    }

    /// <summary> 해당 시너지의 최소 활성 단계에 필요한 유닛 수. 데이터 없으면 int.MaxValue. </summary>
    public int GetMinActiveCount(int id)
    {
        SynergyData data = GetSynergyData(id);
        return data != null ? data.MinActiveCount : int.MaxValue;
    }

    /// <summary> 해당 시너지의 최대 단계에 필요한 유닛 수. 데이터 없으면 0. </summary>
    public int GetMaxActiveCount(int id)
    {
        SynergyData data = GetSynergyData(id);
        return data != null ? data.MaxActiveCount : 0;
    }

    // ───────── 캐시 관리 ─────────

    // Rows 수가 바뀐 경우(시트 런타임 로드 등) 캐시를 다시 빌드한다.
    private void EnsureDataCache()
    {
        if (_synergySO == null)
            return;

        IReadOnlyList<SynergyData> rows = _synergySO.Rows;
        if (rows == null)
            return;

        if (_dataById.Count == rows.Count)
            return;

        BuildDataCache();
    }

    private void BuildDataCache()
    {
        _dataById.Clear();
        _warnedMissingIds.Clear();

        if (_synergySO == null)
        {
            DebugTool.Warnning("[시너지] SynergySO 가 연결되지 않았습니다.", DebugType.Data, this);
            return;
        }

        IReadOnlyList<SynergyData> rows = _synergySO.Rows;
        if (rows == null)
            return;

        for (int i = 0; i < rows.Count; i++)
        {
            SynergyData data = rows[i];
            if (data == null)
                continue;

            if (_dataById.ContainsKey(data.ID))
            {
                DebugTool.Warnning($"[시너지] 중복 ID 데이터가 있습니다 : {data.ID}", DebugType.Data, this);
                continue;
            }

            _dataById.Add(data.ID, data);
        }
    }
    // ─────────────────────────────

    public enum SynergyType
    {
        None = 0,
        Warrior = 50001,
        Magician = 50002,
        Archer = 50003,
        Gunner = 50004,
        Fighter = 50005,
        Human = 50006,
        Elf = 50007,
        Orc = 50008,
        Furry = 50009,
        Spirit = 50010
    }
}