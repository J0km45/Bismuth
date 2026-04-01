using System;
using System.Collections.Generic;

[Serializable]

// 저장할 데이터
public class CatalogEntry
{
    public int unitID;
    public string unitName;
    public bool summoned; // 소환
}


[Serializable]
public class CatalogSaveData
{
    // 일단 리스트로 해
    public List<CatalogEntry> entries = new();

    public static CatalogSaveData FromCatalogSO(UnitCatalogSO unitCatalogSO)
    {
        CatalogSaveData saveData = new();

        foreach (UnitIdSummonedPair  pair in unitCatalogSO.UnitCatalog)
        {
            saveData.entries.Add(new CatalogEntry
            {
                unitID = pair.UnitId, unitName = pair.Name, summoned = pair.Summoned
            });
        }

        return saveData;
    }


}