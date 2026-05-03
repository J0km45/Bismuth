using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CollectUnitInfo : MonoBehaviour
{
    public GameObject selectedUnit;
    
    [SerializeField] private PlayerUIController playerUIController;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button sellButton;
    
    private PlayerAction playerAction;

    public PlayerUIController PlayerUIController => playerUIController;
    
    private void OnEnable()
    {
        upgradeButton.onClick.AddListener(OnUpgrade);
        sellButton.onClick.AddListener(OnSell);

        if (playerAction == null)
        {
            playerAction = playerUIController.PlayerAction;
        }
        playerAction.UI.UnitUpgrade.performed += OnUpgrade;
        playerAction.UI.UnitSell.performed += OnSell;
    }

    private void OnDisable()
    {
        upgradeButton.onClick.RemoveListener(OnUpgrade);
        sellButton.onClick.RemoveListener(OnSell);
        
        playerAction.UI.UnitUpgrade.performed -= OnUpgrade;
        playerAction.UI.UnitSell.performed -= OnSell;
    }

    public void CollectInfo(GameObject unit)
    {
        selectedUnit = unit;
        UnitStat unitStat = unit.GetComponent<UnitStat>();
        UnitStatHub hub = unit.GetComponent<UnitStatHub>();
        float attackPower = hub != null ? hub.Get(StatType.AttackPower) : unitStat.BaseAttackPower;
        DebugTool.Log($"Unit Id: {unitStat.Id}, Unit AttackPower: {attackPower}, Unit AttackSpeed: {unitStat.AttackSpeed} ",DebugType.UI,this);
    }

    private void OnUpgrade(InputAction.CallbackContext ctx)
    {
        if(ctx.performed && selectedUnit != null)
            OnUpgrade();
    }

    private void OnSell(InputAction.CallbackContext ctx)
    {
        if(ctx.performed && selectedUnit != null)
            OnSell();
    }
    

    private void OnUpgrade()
        => playerUIController.OnUnitUpgrade(selectedUnit);

    private void OnSell()
        => playerUIController.OnUnitSell(selectedUnit);
}
