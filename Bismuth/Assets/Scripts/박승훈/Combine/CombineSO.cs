using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CombineDatabase", menuName = "Bismuth/Combine Database", order = 1)]
public class CombineSO : ScriptableObject
{
    [Header("━━━━ 합성 시트 데이터 ━━━━")] [Tooltip("구글시트 합성 시트 전체 데이터")] 
    [SerializeField] private List<CombineData> combineDatas = new List<CombineData>();
    public List<CombineData> CombineDatas => combineDatas;
    
    
    public void ClearUnits()
    {
        combineDatas.Clear();
    }

    /// <summary>
    /// 파싱된 유닛 데이터 추가
    /// </summary>
    public void AddUnit(CombineData data)
    {
        if (data != null)
            combineDatas.Add(data);
    }
}
