using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HitEffectPool : MonoBehaviour
{
    private static HitEffectPool _instance;
    public static HitEffectPool Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<HitEffectPool>();

            if (_instance == null)
            {
                GameObject poolRoot = new GameObject("HitEffectPool");
                _instance = poolRoot.AddComponent<HitEffectPool>();
            }

            return _instance;
        }
    }

    [SerializeField, Min(0)] private int defaultPrewarmCount = 6;
    [SerializeField] private bool poolLog = false;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private readonly Dictionary<GameObject, Transform> poolRoots = new();

    public static GameObject SpawnPooled(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return SpawnPooled(prefab, position, rotation, 1f);
    }

    public static GameObject SpawnPooled(GameObject prefab, Vector3 position, Quaternion rotation, float uniformScaleMultiplier)
    {
        if (prefab == null)
            return null;

        GameObject effect = Instance.Get(prefab, position, rotation);
        if (effect == null)
            return null;

        HitEffectSpawner spawner = effect.GetComponent<HitEffectSpawner>();
        if (spawner != null)
        {
            spawner.ConfigureSpawn(uniformScaleMultiplier);
        }
        else
        {
            float clampedScale = Mathf.Max(0.01f, uniformScaleMultiplier);
            effect.transform.localScale = Vector3.one * clampedScale;
        }

        return effect;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        Queue<GameObject> queue = GetOrCreatePool(prefab);

        GameObject instance = null;
        while (queue.Count > 0 && instance == null)
            instance = queue.Dequeue();

        if (instance == null)
            instance = CreateInstance(prefab);

        HitEffectPoolMember member = instance.GetComponent<HitEffectPoolMember>();
        if (member != null)
            member.NotifySpawned();

        Transform tr = instance.transform;
        tr.SetParent(null, false);
        tr.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        if (poolLog)
        {
            DebugTool.Log(
                $"히트 이펙트 Get | prefab={prefab.name}, remaining={queue.Count}",
                DebugType.Unit,
                this
            );
        }

        return instance;
    }

    public void Release(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
        {
            if (instance != null)
                Destroy(instance);
            return;
        }

        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            Destroy(instance);
            return;
        }

        HitEffectPoolMember member = instance.GetComponent<HitEffectPoolMember>();
        if (member != null && member.IsInPool)
            return;

        if (!poolRoots.TryGetValue(prefab, out Transform root) || root == null)
            root = CreatePoolRoot(prefab);

        if (member != null)
            member.NotifyReleased();

        instance.SetActive(false);
        instance.transform.SetParent(root, false);
        queue.Enqueue(instance);

        if (poolLog)
        {
            DebugTool.Log(
                $"히트 이펙트 Release | prefab={prefab.name}, cached={queue.Count}",
                DebugType.Unit,
                this
            );
        }
    }

    private Queue<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out Queue<GameObject> existing))
            return existing;

        Queue<GameObject> queue = new Queue<GameObject>();
        pools[prefab] = queue;

        Transform root = CreatePoolRoot(prefab);
        poolRoots[prefab] = root;

        for (int i = 0; i < defaultPrewarmCount; i++)
        {
            GameObject instance = CreateInstance(prefab);
            instance.SetActive(false);
            instance.transform.SetParent(root, false);

            HitEffectPoolMember member = instance.GetComponent<HitEffectPoolMember>();
            if (member != null)
                member.NotifyReleased();

            queue.Enqueue(instance);
        }

        return queue;
    }

    private Transform CreatePoolRoot(GameObject prefab)
    {
        GameObject rootObject = new GameObject($"{prefab.name}_HitPool");
        rootObject.transform.SetParent(transform, false);
        return rootObject.transform;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab);

        HitEffectPoolMember member = instance.GetComponent<HitEffectPoolMember>();
        if (member == null)
            member = instance.AddComponent<HitEffectPoolMember>();

        member.Bind(this, prefab);

        if (instance.GetComponent<HitEffectSpawner>() == null)
            instance.AddComponent<HitEffectSpawner>();

        return instance;
    }
}

public class HitEffectPoolMember : MonoBehaviour
{
    private HitEffectPool ownerPool;
    private GameObject sourcePrefab;

    public bool IsInPool { get; private set; }

    public void Bind(HitEffectPool ownerPool, GameObject sourcePrefab)
    {
        this.ownerPool = ownerPool;
        this.sourcePrefab = sourcePrefab;
    }

    public void NotifySpawned()
    {
        IsInPool = false;
    }

    public void NotifyReleased()
    {
        IsInPool = true;
    }

    public void ReleaseToPool()
    {
        if (ownerPool != null && sourcePrefab != null)
        {
            ownerPool.Release(sourcePrefab, gameObject);
            return;
        }

        Destroy(gameObject);
    }
}