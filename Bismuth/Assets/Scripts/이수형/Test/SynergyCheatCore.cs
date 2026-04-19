using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시너지 치트 UI의 순수 로직 계층.
/// - 시너지 ID + 레벨 인덱스를 받아, 그 레벨을 달성하는 데 필요한 유닛 데이터 목록을 반환한다.
/// - UI(EditorWindow, Canvas 등) 구현과 독립적으로 런타임/에디터 양쪽에서 호출 가능.
/// - 실제 "보드 비우기 + 소환" 은 Step 2 에서 이 결과물을 소비하는 형태로 붙일 예정.
/// </summary>
public static class SynergyCheatCore
{
    // ━━━━━━━━ 결과 구조체 ━━━━━━━━

    /// <summary>
    /// GetUnitsForSynergyLevel 의 결과.
    /// units       : 실제로 소환할 UnitData 목록 (UnitData.Id 오름차순, 중복 없음)
    /// requiredCount : 해당 레벨을 달성하는 데 필요한 유닛 수 (SynergyLevelData.ActiveCount)
    /// availableCount : 실제로 소환 가능한 유닛 수 ( = units.Count )
    /// isShort     : 시너지 보유 유닛 수가 requiredCount 보다 적어 부족한 경우 true
    /// </summary>
    public struct UnitSelection
    {
        public List<UnitData> units;
        public int requiredCount;
        public int availableCount;
        public bool isShort;
    }

    // ━━━━━━━━ Public API ━━━━━━━━

    /// <summary>
    /// 해당 시너지의 모든 레벨 ActiveCount 배열. (드롭다운 라벨 생성용)
    /// 데이터 없으면 빈 배열 반환.
    /// </summary>
    public static int[] GetLevelActiveCounts(int synergyId)
    {
        SynergyData data = FindSynergyData(synergyId);
        if (data == null || data.Levels == null || data.Levels.Count == 0)
            return new int[0];

        int[] result = new int[data.Levels.Count];
        for (int i = 0; i < data.Levels.Count; i++)
            result[i] = data.Levels[i] != null ? data.Levels[i].ActiveCount : 0;

        return result;
    }

    /// <summary>
    /// 해당 시너지의 levelIndex 번째 레벨을 달성하기 위한 유닛 목록을 결정한다.
    /// - 해당 시너지를 보유한 모든 티어의 유닛 중, UnitData.Id 오름차순으로 필요 개수만큼 선택
    /// - 유닛이 부족하면 있는 만큼만 반환하고 isShort = true (경고 로그 출력)
    /// </summary>
    public static UnitSelection GetUnitsForSynergyLevel(int synergyId, int levelIndex)
    {
        UnitSelection selection = new UnitSelection
        {
            units = new List<UnitData>(),
            requiredCount = 0,
            availableCount = 0,
            isShort = false
        };

        // 1) 시너지 데이터 확보
        SynergyData synData = FindSynergyData(synergyId);
        if (synData == null)
        {
            DebugTool.Warning(
                $"[치트] 시너지 ID {synergyId} 데이터를 찾을 수 없습니다.",
                DebugType.Synergy
            );
            return selection;
        }

        if (synData.Levels == null || synData.Levels.Count == 0)
        {
            DebugTool.Warning(
                $"[치트] 시너지 {synData.SynergyName} 에 레벨 데이터가 없습니다.",
                DebugType.Synergy
            );
            return selection;
        }

        if (levelIndex < 0 || levelIndex >= synData.Levels.Count)
        {
            DebugTool.Warning(
                $"[치트] 시너지 {synData.SynergyName} 레벨 인덱스 {levelIndex} 는 범위를 벗어납니다. (총 {synData.Levels.Count} 단계)",
                DebugType.Synergy
            );
            return selection;
        }

        SynergyLevelData level = synData.Levels[levelIndex];
        if (level == null)
        {
            DebugTool.Warning(
                $"[치트] 시너지 {synData.SynergyName} 레벨 {levelIndex} 가 null 입니다.",
                DebugType.Synergy
            );
            return selection;
        }

        selection.requiredCount = level.ActiveCount;

        // 2) 해당 시너지 보유 유닛을 모든 티어 UnitSO 에서 수집
        List<UnitData> candidates = CollectUnitsBySynergy(synergyId);

        // 3) UnitData.Id 오름차순 정렬 (중복 ID 방지용 HashSet 병행)
        HashSet<int> addedIds = new HashSet<int>();
        candidates.Sort((a, b) => a.Id.CompareTo(b.Id));

        // 4) 필요 개수만큼 선택 (부족하면 있는 만큼)
        int take = Mathf.Min(selection.requiredCount, candidates.Count);
        for (int i = 0; i < candidates.Count && selection.units.Count < take; i++)
        {
            UnitData candidate = candidates[i];
            if (candidate == null) continue;
            if (!addedIds.Add(candidate.Id)) continue; // 서로 다른 시너지SO 에 동일 유닛이 중복 등록된 경우 방어

            selection.units.Add(candidate);
        }

        selection.availableCount = selection.units.Count;
        selection.isShort = selection.availableCount < selection.requiredCount;

        if (selection.isShort)
        {
            DebugTool.Warning(
                $"[치트] 시너지 '{synData.SynergyName}' 레벨 {levelIndex + 1} 은 {selection.requiredCount} 명이 필요하지만, " +
                $"해당 시너지를 가진 서로 다른 유닛은 {selection.availableCount} 명뿐입니다. (가능한 만큼만 소환됩니다)",
                DebugType.Synergy
            );
        }

        return selection;
    }

    // ━━━━━━━━ Internal ━━━━━━━━

    /// <summary> 씬에서 SynergyManager 를 찾아 SynergyData 조회. Play 모드가 아니거나 씬에 없으면 null. </summary>
    private static SynergyData FindSynergyData(int synergyId)
    {
        SynergyManager manager = Object.FindAnyObjectByType<SynergyManager>();
        if (manager == null)
        {
            DebugTool.Warning(
                "[치트] SynergyManager 를 씬에서 찾을 수 없습니다. Play 모드인지 / 현재 씬에 매니저가 있는지 확인하세요.",
                DebugType.Synergy
            );
            return null;
        }

        return manager.GetSynergyData(synergyId);
    }

    /// <summary> 씬의 SummonUnit.Units(티어별 UnitSO 4개) 를 순회하며 해당 시너지 보유 유닛을 전부 수집. </summary>
    private static List<UnitData> CollectUnitsBySynergy(int synergyId)
    {
        List<UnitData> result = new List<UnitData>();

        SummonUnit summon = Object.FindAnyObjectByType<SummonUnit>();
        if (summon == null)
        {
            DebugTool.Warning(
                "[치트] SummonUnit 을 씬에서 찾을 수 없습니다. Play 모드인지 / 현재 씬에 매니저가 있는지 확인하세요.",
                DebugType.Synergy
            );
            return result;
        }

        if (summon.Units == null)
            return result;

        foreach (UnitSO so in summon.Units)
        {
            if (so == null) continue;
            List<UnitData> found = so.GetUnitsBySynergy(synergyId);
            if (found != null) result.AddRange(found);
        }

        return result;
    }
}
