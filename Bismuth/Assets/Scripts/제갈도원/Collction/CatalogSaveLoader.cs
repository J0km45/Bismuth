using System.Collections;
using UnityEngine;


// 씬의 UnitCatalog 목록이 준비된 뒤 도감 JSON을 로드합니다.
// 로비, 맵등 활성 오브젝트에 붙이고 UnitCatalogSO를 할당

public class CatalogSaveLoader : MonoBehaviour
{
    [SerializeField] private UnitCatalogSO _unitCatalogSO;

    private void Start()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {
        yield return null;

        if (_unitCatalogSO == null)
        {
            Debug.LogError("[CatalogSaveLoader] UnitCatalogSO가 할당되지 않았습니다.", this);
            yield break;
        }

        CatalogPersistenceManager.EnsureInitialized(_unitCatalogSO);
        SyncAllUnit();
        RefreshAllCatalogUIs();
    }

    private void SyncAllUnit()
    {
        foreach (UnitCatalogManager manager in FindObjectsByType<UnitCatalogManager>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (manager == null) continue;

            foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
            {
                if (pair == null) continue;
                if (manager.UnitCatalog.ContainsKey(pair.UnitId))
                    manager.UnitCatalog[pair.UnitId] = pair.Summoned;
            }
        }
    }

    private static void RefreshAllCatalogUIs()
    {
        foreach (CatalogUIController ui in FindObjectsByType<CatalogUIController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (ui != null)
                ui.RefreshSummonedState();
        }
    }
}
