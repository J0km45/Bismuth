using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SynergyEnhanceData
{
    [Header("기본")]
    [FormerlySerializedAs("_target")]
    [SerializeField] private int _synergyEnhanceId;
    [FormerlySerializedAs("_name")]
    [SerializeField] private int _target;
    // 시트에서 target 과 level1 사이 표시용 이름(예: 전사, 마법사).
    [SerializeField] private string _label;

    [Header("단계 수치")]
    [SerializeField] private int _level1;
    [SerializeField] private int _level2;
    [SerializeField] private int _level3;
    [SerializeField] private int _level4;
    [SerializeField] private int _level5;

    [Header("보너스 / 설명")]
    [SerializeField] private int _bonusValue;
    // [SerializeField] private string _bonusExplain;
    // [SerializeField] private string _explain;
    [SerializeField] private string _type;

    public int SynergyEnhanceID => _synergyEnhanceId;
    public int Target => _target;
    public string Label => _label;
    public int Level1 => _level1;
    public int Level2 => _level2;
    public int Level3 => _level3;
    public int Level4 => _level4;
    public int Level5 => _level5;
    public int BonusValue => _bonusValue;
    // public string BonusExplain => _bonusExplain;
    // public string Explain => _explain;
    public string Type => _type;

    // 강화 단계 1~5 에 해당하는 수치. 범위 밖이면 0.
    public int GetLevelValue(int stage)
    {
        return stage switch
        {
            1 => _level1,
            2 => _level2,
            3 => _level3,
            4 => _level4,
            5 => _level5,
            _ => 0
        };
    }

    // 시트 한 행 파싱.
    // 12열: SynergyEnhanceID, target, label, level1~level5, bonus_value, bonus_explain, explain, type
    // 11열: SynergyEnhanceID, target, level1~level5, bonus_value, bonus_explain, explain, type (label 없음)
    public static SynergyEnhanceData Create(string[] line)
    {
        if (line == null || line.Length < 11)
            return null;

        if (!int.TryParse(SafeGet(line, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out int synergyEnhanceId))
            return null;

        if (!int.TryParse(SafeGet(line, 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int target))
            return null;

        SynergyEnhanceData data = new SynergyEnhanceData();
        data._synergyEnhanceId = synergyEnhanceId;
        data._target = target;

        int levelStart;
        int bonusIndex;
        int typeIndex;

        if (line.Length >= 12)
        {
            data._label = SafeGet(line, 2);
            levelStart = 3;
            bonusIndex = 8;
            typeIndex = 11;
        }
        else
        {
            data._label = string.Empty;
            levelStart = 2;
            bonusIndex = 7;
            typeIndex = 10;
        }

        if (!int.TryParse(SafeGet(line, levelStart), NumberStyles.Integer, CultureInfo.InvariantCulture, out data._level1))
            return null;
        if (!int.TryParse(SafeGet(line, levelStart + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out data._level2))
            return null;
        if (!int.TryParse(SafeGet(line, levelStart + 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out data._level3))
            return null;
        if (!int.TryParse(SafeGet(line, levelStart + 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out data._level4))
            return null;
        if (!int.TryParse(SafeGet(line, levelStart + 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out data._level5))
            return null;

        int.TryParse(SafeGet(line, bonusIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out data._bonusValue);
        data._type = SafeGet(line, typeIndex);

        return data;
    }

    private static string SafeGet(string[] arr, int index)
    {
        if (arr == null || index < 0 || index >= arr.Length)
            return string.Empty;

        return arr[index]?.Trim() ?? string.Empty;
    }
}
