using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class SynergyData
{
    private const int MaxColumnIndex = 16; // A~Q

    [Header("기본 정보")]
    [SerializeField] private string _synergyName;
    [SerializeField] private int _id;

    [Header("단계별 효과")]
    [SerializeField] private List<SynergyLevelData> _levels = new();

    public int ID => _id;
    public string SynergyName => _synergyName;
    public List<SynergyLevelData> Levels => _levels;

    // ───────── 조회 헬퍼 ─────────
    // activeCount 만 주면 현재 단계/효과값을 계산해 반환한다.
    // 매칭 규칙 : Levels 중 activeCount >= level.ActiveCount 인 가장 높은 단계를 선택.
    // 순수 데이터 해석 계층 (로그는 상위 계층(SynergyManager)에서 처리).

    /// <summary> 활성 개수에 매칭되는 가장 높은 단계의 LevelData. 없으면 null. </summary>
    public SynergyLevelData GetMatchedLevel(int activeCount)
    {
        if (_levels == null || _levels.Count == 0)
            return null;

        SynergyLevelData matched = null;

        for (int i = 0; i < _levels.Count; i++)
        {
            SynergyLevelData level = _levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            matched = level;
        }

        return matched;
    }

    /// <summary> 매칭된 단계 인덱스. 도달하지 못하면 -1. </summary>
    public int GetLevelIndex(int activeCount)
    {
        if (_levels == null || _levels.Count == 0)
            return -1;

        int matchedIndex = -1;

        for (int i = 0; i < _levels.Count; i++)
        {
            SynergyLevelData level = _levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            matchedIndex = i;
        }

        return matchedIndex;
    }

    /// <summary> 현재 단계의 effectIndex 번째 효과값. 매칭 실패/범위 초과 시 0. </summary>
    public float GetEffectValue(int activeCount, int effectIndex = 0)
    {
        TryGetEffectValue(activeCount, effectIndex, out float value);
        return value;
    }

    /// <summary> 현재 단계의 효과값을 꺼내고 성공 여부를 반환한다. </summary>
    public bool TryGetEffectValue(int activeCount, int effectIndex, out float value)
    {
        value = 0f;

        SynergyLevelData matched = GetMatchedLevel(activeCount);
        if (matched == null)
            return false;

        List<float> effects = matched.EffectValues;
        if (effects == null || effectIndex < 0 || effectIndex >= effects.Count)
            return false;

        value = effects[effectIndex];
        return true;
    }

    /// <summary> 최소 활성 단계에 필요한 유닛 수. 단계 없으면 int.MaxValue. </summary>
    public int MinActiveCount
    {
        get
        {
            if (_levels == null || _levels.Count == 0)
                return int.MaxValue;

            for (int i = 0; i < _levels.Count; i++)
            {
                if (_levels[i] != null)
                    return _levels[i].ActiveCount;
            }

            return int.MaxValue;
        }
    }

    /// <summary> 최대 단계에 필요한 유닛 수. 단계 없으면 0. </summary>
    public int MaxActiveCount
    {
        get
        {
            if (_levels == null || _levels.Count == 0)
                return 0;

            for (int i = _levels.Count - 1; i >= 0; i--)
            {
                if (_levels[i] != null)
                    return _levels[i].ActiveCount;
            }

            return 0;
        }
    }

    /// <summary> 첫 단계에 도달했는지 여부. </summary>
    public bool IsActive(int activeCount)
    {
        if (_levels == null || _levels.Count == 0)
            return false;

        return activeCount >= MinActiveCount;
    }

    // ─────────────────────────────


    public SynergyData(int id, string synergyName, List<SynergyLevelData> levels)
    {
        _id = id;
        _synergyName = synergyName;
        _levels = levels;
    }

    // 시너지 데이터 반환
    // line 한 줄 받아서, 시너지 데이터 객체로 반환
    // | A |   B   | C |    D     |  E   |    F        | G |     H    |
    // | 1 | 전사  | 2 | 공격력+10 |  4   |  공격력+20  | 6 | 공격력+30 |
    // activeColumns : 얘가 활성 단계 숫자가 들어있는 열 인덱스 -> 활성 단계 시작 인덱스 
    public static SynergyData Create(string[] line, IReadOnlyList<int> activeColumns)
    {
        // if (line == null || line.Length < 3 || activeColumns == null || activeColumns.Count == 0)
        //     return null;

        // ID 파싱
        if (!int.TryParse(GetCell(line, 0), out int id))
            return null;

        // B 열 에 있는 애들 이름 읽기
        string synergyName = GetCell(line, 1);
    
        // 레벨 리스트 인데 여기서 단계별로 뭐가 활성화가 되는지
        List<SynergyLevelData> levels = new();

        // 순서대로 처리하고
        for (int i = 0; i < activeColumns.Count; i++)
        {
            int activeColumn = activeColumns[i]; // activeColumn : 시작 되는 열
                                                 // nextActiveColumn : 다음 엑티브가 시작되는 열
            int nextActiveColumn = i + 1 < activeColumns.Count ? activeColumns[i + 1] : MaxColumnIndex + 1;
            string activeCountText = GetCell(line, activeColumn);

            // 공백
            if (string.IsNullOrWhiteSpace(activeCountText))
                continue;

            // 정수 반환
            if (!int.TryParse(activeCountText, out int activeCount))
                continue;

            List<float> effectValues = new();

            // 효과값 읽기 Q 열 까지
            for (int col = activeColumn + 1; col < nextActiveColumn; col++)
            {
                if (col > MaxColumnIndex || col >= line.Length)
                    break;

                string cell = GetCell(line, col);

                if (string.IsNullOrWhiteSpace(cell))
                    continue;

                string normalizedCell = cell.Trim();

                if (!float.TryParse(
                        normalizedCell,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float effectValue))
                    continue;

                effectValues.Add(effectValue);
            }

            SynergyLevelData levelData = new SynergyLevelData();
            levelData.SetData(activeCount, effectValues);
            levels.Add(levelData);
        }

        return new SynergyData(id, synergyName, levels);
    }

    
    // 셀 값 불러오기 계속 사용 중 ㅇㅇ
    private static string GetCell(string[] line, int index)
    {
        if (line.Length <= index)
            return string.Empty;

        return line[index].Trim();
    }

}