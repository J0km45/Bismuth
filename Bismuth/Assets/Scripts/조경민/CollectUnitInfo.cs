using UnityEngine;
using UnityEngine.UI;

public class CollectUnitInfo : MonoBehaviour
{
    public GameObject selectedUnit;
    [SerializeField] private PlayerUIController playerUIController;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button sellButton;
    public void CollectInfo(GameObject unit)
    {
        selectedUnit = unit;
        UnitStat unitStat = unit.GetComponent<UnitStat>();
        DebugTool.Log($"Unit Id: {unitStat.Id}, Unit AttackPower: {unitStat.CurrentAttackPower}, Unit AttackSpeed: {unitStat.AttackSpeed} ",DebugType.UI,this);
    }

    private void Awake()
    {
        upgradeButton.onClick.AddListener(OnUpgrade);
        sellButton.onClick.AddListener(OnSell);
    }

    private void OnUpgrade()
        => playerUIController.OnUnitUpgrade(selectedUnit);

    private void OnSell()
        => playerUIController.OnUnitSell(selectedUnit);
}
