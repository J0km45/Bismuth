using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시너지 치트 UI의 실행 계층.
/// SynergyCheatCore 가 결정한 유닛 목록을 받아, 보드를 비우고 순차적으로 소환한다.
/// UI(Step 3) 는 ExecuteSynergyCheat() 한 번만 호출하면 된다.
/// </summary>
public static class SynergyCheatExecutor
{
    // ━━━━━━━━ 결과 구조체 ━━━━━━━━

    /// <summary>
    /// ExecuteSynergyCheat 의 결과 요약.
    /// despawnedCount : 보드에서 디스폰된 유닛 수
    /// requestedCount : Core 가 결정한 소환 요청 수 (= UnitSelection.units.Count)
    /// spawnedCount   : 실제로 TrySummonAndPlace 에 성공한 수
    /// failedCount    : requestedCount - spawnedCount
    /// requiredCount  : 시너지 레벨이 요구하는 유닛 수 (UI 표시용)
    /// isShort        : 시너지 보유 유닛 부족 여부 (Core 결과 그대로)
    /// aborted        : 잘못된 입력 등으로 보드 비우기/소환을 진행하지 않은 경우 true
    /// </summary>
    public struct ExecuteResult
    {
        public int despawnedCount;
        public int requestedCount;
        public int spawnedCount;
        public int failedCount;
        public int requiredCount;
        public bool isShort;
        public bool aborted;
    }

    // ━━━━━━━━ Public API ━━━━━━━━

    /// <summary>
    /// 시너지 ID + 레벨 인덱스를 받아:
    /// 1) Core 로 소환할 유닛 목록 결정
    /// 2) 결정된 목록이 비어있지 않으면 현재 보드의 모든 유닛 디스폰
    /// 3) 결정된 유닛들을 순차적으로 TrySummonAndPlace
    /// 결과 요약은 DebugTool 로그 + ExecuteResult 반환값으로 제공.
    /// </summary>
    public static ExecuteResult ExecuteSynergyCheat(int synergyId, int levelIndex)
    {
        ExecuteResult result = default;

        // 1) Core 로 소환 대상 결정
        SynergyCheatCore.UnitSelection selection =
            SynergyCheatCore.GetUnitsForSynergyLevel(synergyId, levelIndex);

        result.requiredCount = selection.requiredCount;
        result.requestedCount = selection.availableCount;
        result.isShort = selection.isShort;

        // 2) 안전 검사 : 빈 결과면 보드를 건드리지 않고 종료
        if (selection.units == null || selection.units.Count == 0)
        {
            DebugTool.Warning(
                $"[치트] 소환할 유닛이 없어 보드를 비우지 않고 종료합니다. " +
                $"(시너지ID={synergyId}, 레벨인덱스={levelIndex})",
                DebugType.Synergy
            );
            result.aborted = true;
            return result;
        }

        // 3) SummonUnit 확보
        SummonUnit summon = Object.FindAnyObjectByType<SummonUnit>();
        if (summon == null)
        {
            DebugTool.Error(
                "[치트] SummonUnit 을 씬에서 찾을 수 없습니다. Play 모드인지 확인하세요.",
                DebugType.Synergy
            );
            result.aborted = true;
            return result;
        }

        // 4) 보드 비우기
        result.despawnedCount = ClearBoard(summon);

        // 5) 순차 소환
        result.spawnedCount = SpawnSequentially(summon, selection.units);
        result.failedCount = result.requestedCount - result.spawnedCount;

        // 6) 요약 로그
        LogSummary(synergyId, levelIndex, result);

        return result;
    }

    // ━━━━━━━━ Internal ━━━━━━━━

    /// <summary>
    /// 현재 OwnedTowers 의 모든 유닛을 디스폰한다.
    /// TryDespawnTower 가 OwnedTowers 를 수정하므로 반드시 snapshot 을 떠서 순회.
    /// </summary>
    private static int ClearBoard(SummonUnit summon)
    {
        if (summon.OwnedTowers == null || summon.OwnedTowers.Count == 0)
            return 0;

        // snapshot : 순회 중 원본 수정으로 인한 InvalidOperationException 방지
        List<TowerUnit> snapshot = new List<TowerUnit>(summon.OwnedTowers.Count);
        foreach (SummonUnit.SummonedTowerRecord record in summon.OwnedTowers)
        {
            if (record == null || record.towerUnit == null) continue;
            snapshot.Add(record.towerUnit);
        }

        int despawned = 0;
        foreach (TowerUnit tower in snapshot)
        {
            if (tower == null) continue;
            if (summon.TryDespawnTower(tower))
                despawned++;
        }

        DebugTool.Log(
            $"[치트] 보드 비우기 완료 : {despawned} / {snapshot.Count}",
            DebugType.Synergy
        );

        return despawned;
    }

    /// <summary>
    /// 결정된 UnitData 리스트를 순차적으로 TrySummonAndPlace 한다.
    /// </summary>
    private static int SpawnSequentially(SummonUnit summon, List<UnitData> targets)
    {
        int spawned = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            UnitData data = targets[i];
            if (data == null) continue;

            bool ok = summon.TrySummonAndPlace(data);
            if (ok)
            {
                spawned++;
            }
            else
            {
                DebugTool.Warning(
                    $"[치트] 유닛 소환 실패 - ID={data.Id}, 이름={data.UnitName} ({i + 1}/{targets.Count})",
                    DebugType.Synergy
                );
            }
        }

        return spawned;
    }

    private static void LogSummary(int synergyId, int levelIndex, ExecuteResult r)
    {
        string shortMark = r.isShort ? " ※부족" : "";
        DebugTool.Log(
            $"[치트] 시너지 치트 실행 결과 - 시너지ID:{synergyId} / 레벨인덱스:{levelIndex}\n" +
            $"  요구개수 : {r.requiredCount}{shortMark}\n" +
            $"  보드 비우기 : {r.despawnedCount} 디스폰\n" +
            $"  소환 시도 : {r.requestedCount} | 성공 : {r.spawnedCount} | 실패 : {r.failedCount}",
            DebugType.Synergy
        );
    }
}
