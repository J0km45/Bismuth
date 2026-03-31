using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class UnitAttackSensor : MonoBehaviour
{
    [Header("Sensor")]
    [SerializeField] private CircleCollider2D sensorCollider;
    [SerializeField] private LayerMask monsterLayerMask;
    [SerializeField] private UnitStat unitStat;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private AttackRangeVisualizer rangeVisualizer;
    [Header("Debug")]
    [SerializeField] private bool sensorLog = true;

    private readonly List<MonsterController> monstersInRange = new();

    public IReadOnlyList<MonsterController> MonstersInRange => monstersInRange;
    public CircleCollider2D SensorCollider => sensorCollider;

    private void Reset()
    {
        sensorCollider = GetComponent<CircleCollider2D>();
        ConfigureCollider();
        TrySetDefaultMonsterLayer();
    }

    private void Awake()
    {
        CacheReferences();
        EnsureRuntimeComponents();

        ConfigureCollider();
        TrySetDefaultMonsterLayer();
        SyncRadiusFromUnitStat();

        if (rangeVisualizer != null)
            rangeVisualizer.Show();
    }

    private void EnsureRuntimeComponents()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.sortingOrder = 10;
            }
        }

        if (rangeVisualizer == null)
        {
            rangeVisualizer = GetComponent<AttackRangeVisualizer>();

            if (rangeVisualizer == null)
                rangeVisualizer = gameObject.AddComponent<AttackRangeVisualizer>();
        }
    }
    private void CacheReferences()
    {
        this.gameObject.layer = LayerMask.NameToLayer("Default");

        if (sensorCollider == null)
            sensorCollider = GetComponent<CircleCollider2D>();

        if (unitStat == null)
            unitStat = GetComponentInParent<UnitStat>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (rangeVisualizer == null)
            rangeVisualizer = GetComponent<AttackRangeVisualizer>();





    }

    public void SyncRadiusFromUnitStat()
    {
        CacheReferences();

        if (sensorCollider == null || unitStat == null)
            return;

        float radius = Mathf.Max(0.01f, unitStat.Range);
        sensorCollider.radius = radius;

        rangeVisualizer.Init(sensorCollider, lineRenderer);
    }

    private void ConfigureCollider()
    {
        if (sensorCollider == null)
            return;

        sensorCollider.isTrigger = true;
    }

    private void TrySetDefaultMonsterLayer()
    {
        if (monsterLayerMask.value != 0)
            return;

        int monsterLayer = LayerMask.NameToLayer("Monster");
        if (monsterLayer >= 0)
            monsterLayerMask = 1 << monsterLayer;
    }

    public void SetRadius(float radius)
    {
        if (sensorCollider == null)
            sensorCollider = GetComponent<CircleCollider2D>();

        if (sensorCollider == null)
            return;

        sensorCollider.radius = Mathf.Max(0.01f, radius);
    }

    public MonsterController GetFirstTarget()
    {
        PruneInvalidTargets();

        if (monstersInRange.Count == 0)
            return null;

        return monstersInRange[0];
    }

    public bool Contains(MonsterController monster)
    {
        if (!IsTargetUsable(monster))
            return false;

        return monstersInRange.Contains(monster) && IsActuallyInRange(monster);
    }

    public void PruneInvalidTargets()
    {
        for (int i = monstersInRange.Count - 1; i >= 0; i--)
        {
            MonsterController monster = monstersInRange[i];

            bool removable =
                !IsTargetUsable(monster) ||
                !IsActuallyInRange(monster);

            if (!removable)
                continue;

            if (sensorLog && monster != null)
            {
                DebugTool.Log(
                    $"[UnitAttackSensor] 범위 목록에서 제거: {monster.name}",
                    DebugType.Unit,
                    this
                );
            }

            monstersInRange.RemoveAt(i);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegister(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 유닛이 범위 안에 소환된 경우도 보정
        TryRegister(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        MonsterController monster = other.GetComponentInParent<MonsterController>();
        if (monster == null)
            return;

        if (monstersInRange.Remove(monster) && sensorLog)
        {
            DebugTool.Log(
                $"[UnitAttackSensor] 범위 이탈 감지: {monster.name}",
                DebugType.Unit,
                this
            );
        }
    }

    private void OnDisable()
    {
        monstersInRange.Clear();
    }

    private void TryRegister(Collider2D other)
    {
        if (!IsMonsterLayer(other.gameObject.layer))
            return;

        MonsterController monster = other.GetComponentInParent<MonsterController>();
        if (!IsTargetUsable(monster))
            return;

        if (!IsActuallyInRange(monster))
            return;

        if (monstersInRange.Contains(monster))
            return;

        monstersInRange.Add(monster);

        if (sensorLog)
        {
            DebugTool.Log(
                $"[UnitAttackSensor] 타겟 등록: {monster.name}",
                DebugType.Unit,
                this
            );
        }
    }

    private bool IsMonsterLayer(int layer)
    {
        return (monsterLayerMask.value & (1 << layer)) != 0;
    }

    private bool IsTargetUsable(MonsterController monster)
    {
        return monster != null && monster.gameObject.activeInHierarchy;
    }

    private bool IsActuallyInRange(MonsterController monster)
    {
        if (monster == null || sensorCollider == null)
            return false;

        Vector3 sensorCenter = GetWorldCenter();
        Vector3 targetPosition = monster.transform.position;

        float radius = GetWorldRadius();
        float sqrDistance = (targetPosition - sensorCenter).sqrMagnitude;

        return sqrDistance <= radius * radius;
    }

    private Vector3 GetWorldCenter()
    {
        if (sensorCollider == null)
            return transform.position;

        return sensorCollider.transform.TransformPoint(sensorCollider.offset);
    }

    private float GetWorldRadius()
    {
        if (sensorCollider == null)
            return 0f;

        Vector3 lossyScale = sensorCollider.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));

        return sensorCollider.radius * maxScale;
    }

    private void OnDrawGizmosSelected()
    {
        CircleCollider2D circle = sensorCollider != null ? sensorCollider : GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        Vector3 center = circle.transform.TransformPoint(circle.offset);
        Vector3 lossyScale = circle.transform.lossyScale;
        float radius = circle.radius * Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, radius);
    }

    private void OnValidate()
    {
        CacheReferences();
        ConfigureCollider();
        SyncRadiusFromUnitStat();
    }
}