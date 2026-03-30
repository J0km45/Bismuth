using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SynergyLevelData
{
    [Header("효과값 목록")]
    [SerializeField] private List<float> _effectValues = new();
    
    
    [Header("활성 유닛 수")]
    [SerializeField] private int _activeCount;


    public int ActiveCount => _activeCount;
    public List<float> EffectValues => _effectValues;

    public void SetData(int activeCount, List<float> effectValues)
    {
        _activeCount = activeCount;
        _effectValues = effectValues;
    }


}