using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CombineData
{
    [Header("━━━━ 조합 정보 ━━━━")]
    [Tooltip("조합 대상")] [SerializeField] private int resultUnit;
    [Tooltip("유닛 티어")] [SerializeField] private int tier;
    [Tooltip("재료 유닛 1")] [SerializeField] private int[] sourceUnit = new int[3];
    
    public int ResultUnit => resultUnit;
    public int Tier => tier;
    public int[] SourceUnit => sourceUnit;
    
    public static CombineData CreateFromSheetRow(string[] line)
    {
        if (line == null || line.Length < 5) return null;
        if (string.IsNullOrWhiteSpace(line[0]) || !int.TryParse(line[0].Trim(), out _)) return null;

        var data = new CombineData();
        data.InitFromSheetRow(line);
        
        return data;
    }
    
    private void InitFromSheetRow(string[] line)
    {
        int.TryParse(SafeGet(line, 0), out tier);
        int.TryParse(SafeGet(line, 1), out resultUnit);
        int.TryParse(SafeGet(line, 2), out sourceUnit[0]);
        int.TryParse(SafeGet(line, 3), out sourceUnit[1]);
        int.TryParse(SafeGet(line, 4), out sourceUnit[2]);
    }

    private static string SafeGet(string[] arr, int index)
        => (arr != null && index < arr.Length) ? arr[index]?.Trim() ?? "" : "";
}
