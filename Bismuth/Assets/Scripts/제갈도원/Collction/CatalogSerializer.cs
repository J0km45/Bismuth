using System.IO;
using UnityEngine;

public class CatalogSerializer
{
    // 파일 이름 json으로 저장 할 거임
    private const string SaveFileName = "catalog.save.json";

    // 저장 경로 AppData -> LocalLoa -> Bismuth -> catalog.save.json
    // Path.Combine : 문자열 더하기
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);


    // UnitCatalogSO 데이터를 JSON 으로 저장
    // 현재 도감 상태를 JSON 파일로 저장
    public static void Save(UnitCatalogSO unitCatalogSO)
    {
        if (unitCatalogSO == null) return;

        try
        {
            // SO 수정 x
            // 직접 저장하지 않고 저장 전용 데이터 클래스로 변환
            CatalogSaveData saveData = CatalogSaveData.FromCatalogSO(unitCatalogSO);

            // Json 은 문자열임
            string json = JsonUtility.ToJson(saveData, true);

            string directoryPath = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            File.Move(tempPath, SavePath);
            Debug.Log($"도감 저장 완료\n경로 : {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"도감 저장 실패: {e.Message}\n경로 : {SavePath}");
        }
    }

    // 저장을 했으면 읽어야 함
    // JOSN 파일을 읽어서 도감 상태 복원
    public static void Load(UnitCatalogSO unitCatalogSO)
    {
        if (unitCatalogSO == null)
        {
            Debug.LogWarning("CatalogSerializer.Load : UnitCatalogSO가 null 입니다.");
            return;
        }

        // 저장 파일이 없으면 불러올 것이 없음
        if (!HasSaveFile())
        {
            Debug.Log("도감 저장 파일이 없습니다.");
            return;
        }


        try
        {
            // 문자열 가져오기
            string json = File.ReadAllText(SavePath);

            // Json 문자열 -> 저장용 데이터 클래스로 변환
            CatalogSaveData saveData = JsonUtility.FromJson<CatalogSaveData>(json);
            if (saveData == null)
            {
                Debug.LogWarning($"도감 로드 실패: JSON 파싱 결과가 null 입니다.\n경로 : {SavePath}");
                return;
            }

            if (saveData.entries == null)
                saveData.entries = new System.Collections.Generic.List<CatalogEntry>();

            ApplyCatalogSO(saveData, unitCatalogSO);
            Debug.Log("도감 로드 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"도감 로드 실패: {e.Message}\n경로 : {SavePath}");
        }
    }

    // 저장 파일 있는지 확인
    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }


    // 저장된 CatalogSvaeData의 값을 UnitCatalogSO에 반영
    // id 가 같은 항목을 찾아서 summoned 값만 덮어쓰기
    private static void ApplyCatalogSO(CatalogSaveData catalogSaveData, UnitCatalogSO unitCatalogSO)
    {
        if (unitCatalogSO.UnitCatalog == null || catalogSaveData.entries == null) return;

        // SO 안에 있는 리스트를 전부 순회
        // unitCatalogSO.UnitCatalog 에는 유닛 목록이 들어가 있고
        for (int i = 0; i < unitCatalogSO.UnitCatalog.Count; i++)
        {
            UnitIdSummonedPair pair = unitCatalogSO.UnitCatalog[i];

            // 데이터 있으면 건너뜀
            if (pair == null) continue;


            // catalogSaveData.entries 에는 저장된 상태가 들어가 있음
            for (int j = 0; j < catalogSaveData.entries.Count; j++)
            {
                CatalogEntry entry = catalogSaveData.entries[j];

                // id 같으면 같은 유닛
                // 저장 데이터에서 같은 ID 를 찾아 summonde 에 반영
                if (entry.unitID == pair.UnitId)
                {
                    pair.Summoned = entry.summoned;
                    break;
                }
            }
        }
    }
}