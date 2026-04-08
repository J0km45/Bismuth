using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CatalogPersistenceManager : MonoBehaviour
{
    private static CatalogPersistenceManager _instance;
    private static UnitCatalogSO _bootstrapCatalog;

    [SerializeField] private UnitCatalogSO _unitCatalogSO;

    private HashSet<int> _discoveredIds = new();
    private bool _initialized;
    private bool _dirty;

    public static void EnsureInitialized(UnitCatalogSO catalogSo)
    {
        if (_instance != null)
            return;

        if (catalogSo != null)
            _bootstrapCatalog = catalogSo;

        var go = new GameObject("[CatalogPersistence]");
        DontDestroyOnLoad(go);
        go.AddComponent<CatalogPersistenceManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Application.quitting += OnQuittingSave;

        if (_unitCatalogSO == null && _bootstrapCatalog != null)
            _unitCatalogSO = _bootstrapCatalog;
        _bootstrapCatalog = null;

        if (_unitCatalogSO == null)
        {
            Debug.LogError("[도감저장] UnitCatalogSO가 없습니다. EnsureInitialized(catalogSo)에 SO를 넘겨주세요.");
            return;
        }

        InitializeFromDiskImmediate();
    }

    private void OnDestroy()
    {
        if (_instance != this)
            return;

        Application.quitting -= OnQuittingSave;
        CancelInvoke(nameof(PeriodicSave));
        _instance = null;
    }

    private void InitializeFromDiskImmediate()
    {
        Debug.Log($"[도감저장] 초기화 - SO: {_unitCatalogSO.name}");

        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
        {
            if (pair != null)
                pair.Summoned = false;
        }

        CatalogSerializer.Load(_unitCatalogSO);
        RefreshDiscoveredFromSO();
        _initialized = true;

        InvokeRepeating(nameof(PeriodicSave), 20f, 25f);

        Debug.Log($"[도감저장] 초기화 완료 - 기존 개방: {_discoveredIds.Count} / {_unitCatalogSO.UnitCatalog.Count}");
        Debug.Log($"[도감저장] JSON 경로: {Path.Combine(Application.persistentDataPath, "catalog.save.json")}");
    }

    private void PeriodicSave()
    {
        if (_unitCatalogSO != null && _initialized)
            CatalogSerializer.Save(_unitCatalogSO);
    }

    private void RefreshDiscoveredFromSO()
    {
        _discoveredIds.Clear();
        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
        {
            if (pair != null && pair.Summoned)
                _discoveredIds.Add(pair.UnitId);
        }
    }

    private void OnQuittingSave()
    {
        if (_unitCatalogSO != null && _initialized)
            CatalogSerializer.Save(_unitCatalogSO);
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

    private void OnApplicationPause(bool pause)
    {
        if (pause && _unitCatalogSO != null && _initialized)
            CatalogSerializer.Save(_unitCatalogSO);
    }
}
