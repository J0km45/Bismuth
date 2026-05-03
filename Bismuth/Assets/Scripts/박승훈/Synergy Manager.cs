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
    
    public UnityEvent<UnitStat> OnUnitCreated;
    public UnityEvent<UnitStat> OnUnitRemoved;
    public event Action<Dictionary<int, List<int>>> OnSynergyChanged;

    // key = 시너지 ID, value = 해당 시너지를 보유한 유닛의 개수
    private Dictionary<int, List<int>> synergiesDict = new();

    [SerializeField] private int _unitCount = 0;

    [SerializeField] private bool _log = true;

    private void Awake()
    {
        _summonUnit = GetComponent<SummonUnit>();
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