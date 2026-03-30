using UnityEngine;

public class SummonManager : MonoBehaviour
{
    [SerializeField] private SummonUnit summonUnit;
    [SerializeField] private bool BoardLog = true;

    private void Awake()
    {
        if (summonUnit == null)
            summonUnit = GetComponent<SummonUnit>();
    }

    private void Start()
    {
        DebugTool.DebugSelect(DebugType.Board, BoardLog);
    }

    public void SummonUnit()
    {
        if (summonUnit == null)
        {
            DebugTool.Error("SummonUnit 참조가 없습니다.", DebugType.Summon, this);
            return;
        }

        bool success = summonUnit.TrySummonAndPlace();

        if (!success)
        {
            DebugTool.Warnning("소환에 실패했습니다.", DebugType.Summon, this);
        }
    }

    public void DespawnUnit(TowerUnit tower)
    {
        if (summonUnit == null)
        {
            DebugTool.Error("SummonUnit 참조가 없습니다.", DebugType.Summon, this);
            //return false;
        }

        bool success = summonUnit.TryDespawnTower(tower);

        if (!success)
        {
            DebugTool.Warnning("디스폰에 실패했습니다.", DebugType.Summon, this);
        }

        //return success;
    }
}