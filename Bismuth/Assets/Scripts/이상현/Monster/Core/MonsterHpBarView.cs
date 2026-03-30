using UnityEngine;
using UnityEngine.UI;

public class MonsterHpBarView : MonoBehaviour
{
    [Header("====컴포넌트 참조====")]
    [Tooltip("몬스터 컨트롤러")]
    [SerializeField] private MonsterController _monsterController;
    
    [Tooltip("체력 비율 표시할 Fill 이미지")]
    [SerializeField] private Image _fillImage;

    private void Reset()
    {
        _monsterController = GetComponentInParent<MonsterController>();
    }

    private void Awake()
    {
        if (_monsterController == null)
        {
            _monsterController = GetComponentInParent<MonsterController>();
        }
    }

    private void OnEnable() => _monsterController.HealthChanged += HandleHealthChanged;
    private void OnDisable() => _monsterController.HealthChanged -= HandleHealthChanged;
    
    
    private void HandleHealthChanged(MonsterController monsterController)
    {
        RefreshHpBar();
    }

    private void RefreshHpBar()
    {
        if (_monsterController == null) return;
        if (_fillImage == null) return;

        if (_monsterController.MaxHp <= 0f)
        {
            _fillImage.fillAmount = 0f;
            return;
        }
        
        float hpRatio = Mathf.Clamp01
            (_monsterController.CurrentHp / _monsterController.MaxHp);
        _fillImage.fillAmount = hpRatio;
    }
}
