using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public class EnemyData
{
    [Header("기본 정보")]
    [SerializeField] private int _id;
    [SerializeField] private string _category;
    [SerializeField] private string _name;

    [Header("기본 스탯")]
    [SerializeField] private int _baseHp;
    [SerializeField] private float _hpGrowth;
    [SerializeField] private int _baseDefense;
    [SerializeField] private float _defenseGrowth;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private MoveType _moveType;

    [Header("전투 정보")]
    [SerializeField] private int _baseDamageToBase;
    [SerializeField] private int _killReward;

    public int Id => _id;
    public string Category => _category;
    public string Name => _name;
    public int BaseHp => _baseHp;
    public float HpGrowth => _hpGrowth;
    public int BaseDefense => _baseDefense;
    public float DefenseGrowth => _defenseGrowth;
    public float MoveSpeed => _moveSpeed;
    public MoveType EnemyMoveType => _moveType;
    public int BaseDamageToBase => _baseDamageToBase;
    public int KillReward => _killReward;

    public enum MoveType
    {
        Normal,
        TeleportAfterWait
    }

    public static EnemyData Create(string[] line)
    {
        if (line == null || line.Length < 11)
            return null;

        if (!int.TryParse(SafeGet(line, 0), out int id))
            return null;

        EnemyData data = new EnemyData();
        data._id = id;
        data._category = SafeGet(line, 1);
        data._name = SafeGet(line, 2);
        data._baseHp = ParseInt(SafeGet(line, 3));
        data._hpGrowth = ParseFloat(SafeGet(line, 4));
        data._baseDefense = ParseInt(SafeGet(line, 5));
        data._defenseGrowth = ParseFloat(SafeGet(line, 6));
        data._moveSpeed = ParseFloat(SafeGet(line, 7));
        data._moveType = ParseMoveType(SafeGet(line, 8));
        data._baseDamageToBase = ParseInt(SafeGet(line, 9));
        data._killReward = ParseInt(SafeGet(line, 10));

        return data;
    }

    private static MoveType ParseMoveType(string value)
    {
        return value.Trim() switch
        {
            "일반" => MoveType.Normal,
            "대기 후 순간이동" => MoveType.TeleportAfterWait,
            _ => MoveType.Normal
        };
    }

    private static int ParseInt(string value)
    {
        if (int.TryParse(value, out int parsed))
            return parsed;
        return 0;
    }

    private static float ParseFloat(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0f;

        string normalized = value.Replace(",", ".").Trim();
        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            return parsed;

        return 0f;
    }

    private static string SafeGet(string[] line, int index)
    {
        return index >= 0 && index < line.Length ? line[index]?.Trim() ?? string.Empty : string.Empty;
    }
}
