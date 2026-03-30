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
        AddComponent();

        
        
        ConfigureCollider();
        TrySetDefaultMonsterLayer();
        SyncRadiusFromUnitStat();

        if (rangeVisualizer != null)
            rangeVisualizer.Show();
    }

    private void AddComponent()
    {
        lineRenderer = this.gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = 10;

        this.gameObject.AddComponent<AttackRangeVisualizer>();

        
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
        if (monster == null)
            return false;

        return monstersInRange.Contains(monster);
    }

    public void PruneInvalidTargets()
    {
        for (int i = monstersInRange.Count - 1; i >= 0; i--)
        {
            if (!IsTargetUsable(monstersInRange[i]))
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

        monstersInRange.Remove(monster);
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

        if (monstersInRange.Contains(monster))
            return;

        monstersInRange.Add(monster);

        DebugTool.Log($"[UnitAttackSensor] Registered monster: {monster.name}",DebugType.Unit,this);
    }

    private bool IsMonsterLayer(int layer)
    {
        return (monsterLayerMask.value & (1 << layer)) != 0;
    }

    private bool IsTargetUsable(MonsterController monster)
    {
        return monster != null && monster.gameObject.activeInHierarchy;
    }

    private void OnDrawGizmosSelected()
    {
        CircleCollider2D circle = sensorCollider != null ? sensorCollider : GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, circle.radius);
    }

    private void OnValidate()
    {
        CacheReferences();
        ConfigureCollider();
        SyncRadiusFromUnitStat();
    }
}