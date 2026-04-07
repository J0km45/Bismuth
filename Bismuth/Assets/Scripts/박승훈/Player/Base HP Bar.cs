using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseHPBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private TextMeshProUGUI _hpText;
    
    private int _currentHp;
    private int _maxHp;

    public void Initialize(int maxHp, int currentHp)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = Mathf.Clamp(currentHp, 0, _maxHp);

        Refresh();
    }

    public void SetMaxHp(int maxHp)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;

        Refresh();
    }

    public void SetHp(int currentHp)
    {
        _currentHp = Mathf.Clamp(currentHp, 0, _maxHp);

        Refresh();
    }

    public void Refresh()
    {
        float ratio = (float)_currentHp / _maxHp;

        if (_fillImage != null)
            _fillImage.fillAmount = ratio;

        if (_hpText != null)
            _hpText.text = $"{_currentHp}/{_maxHp}";
    }
}