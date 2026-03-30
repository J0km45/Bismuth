using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Bismuth/Enemy SO")]
public class EnemySO : ScriptableObject
{
    [SerializeField] private List<EnemyData> _rows = new();

    public IReadOnlyList<EnemyData> Rows => _rows;

    public void SetRows(List<EnemyData> rows)
    {
        _rows = rows != null ? new List<EnemyData>(rows) : new List<EnemyData>();
    }
}
