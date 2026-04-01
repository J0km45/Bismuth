using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CatalogPersistenceManager : MonoBehaviour
{
    
    [SerializeField] private UnitCatalogSO _unitCatalogSO;

    private HashSet<int> _discoveredIds = new();
    private bool _initialized;
    private bool _dirty;

    private void Start()
    {
        Debug.Log($"[도감저장] Start - SO: {(_unitCatalogSO != null ? _unitCatalogSO.name : "NULL!")}");

        if (_unitCatalogSO == null)
        {
            Debug.LogError("[도감저장] UnitCatalogSO가 연결되지 않았습니다!");
            return;
        }

        StartCoroutine(InitAfterDelay());
    }

    private IEnumerator InitAfterDelay()
    {
        yield return null;
        yield return null;

        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
            pair.Summoned = false;

        CatalogSerializer.Load(_unitCatalogSO);

        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
        {
            if (pair.Summoned)
                _discoveredIds.Add(pair.UnitId);
        }

        _initialized = true;
        Debug.Log($"[도감저장] 초기화 완료 - 기존 개방: {_discoveredIds.Count} / {_unitCatalogSO.UnitCatalog.Count}");
        Debug.Log($"[도감저장] JSON 경로: {Path.Combine(Application.persistentDataPath, "catalog.save.json")}");
    }

    private void LateUpdate()
    {
        if (!_initialized) return;

        UnitStat[] allUnits = FindObjectsByType<UnitStat>(FindObjectsSortMode.None);

        foreach (UnitStat unit in allUnits)
        {
            if (unit.Id == 0) continue;

            if (_discoveredIds.Add(unit.Id))
            {
                Debug.Log($"[도감저장] 새 유닛 발견! ID:{unit.Id} Name:{unit.Name}");
                MarkSummonedInSO(unit.Id);
                _dirty = true;
            }
        }

        if (_dirty)
        {
            _dirty = false;
            CatalogSerializer.Save(_unitCatalogSO);
            Debug.Log($"[도감저장] 저장 완료 - 총 개방: {_discoveredIds.Count}");
        }
    }

    private void MarkSummonedInSO(int unitId)
    {
        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
        {
            if (pair.UnitId == unitId)
            {
                pair.Summoned = true;
                return;
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (_unitCatalogSO != null && _initialized)
        {
            Debug.Log("[도감저장] 앱 종료 - 최종 저장");
            CatalogSerializer.Save(_unitCatalogSO);
        }
    }
}
