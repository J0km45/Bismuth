using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleDifficulty
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public class DifficultyWaveSet
{
    [Header("====난이도 정보====")]
    [Tooltip("해당 웨이브 세트의 난이도 설정\n예)Easy, Normal, Hard")]
    [SerializeField] private BattleDifficulty _difficulty = BattleDifficulty.Normal;
    
    [Header("====웨이브 목록====")]
    [Tooltip("해당 난이도에서 사용할 웨이브 목록\n순서대로 넣어야 합니다.")]
    [SerializeField] private List<WaveDataSO> _waves = new();
    
    public BattleDifficulty Difficulty => _difficulty;
    public IReadOnlyList<WaveDataSO> Waves => _waves;
}

[CreateAssetMenu(fileName = "MapBattleConfig", menuName = "Data/Battle/Map Battle Config")]
public class MapBattleConfigSO : ScriptableObject
{
    [Header("====기본 정보====")]
    [Tooltip("이 설정이 어떤 맵용인지 구분하기 위한 이름")]
    [SerializeField] private string _mapName;

    [Tooltip("이 설정이 어떤 난이도용인지 구분하기 위한 이름")]
    [SerializeField] private string _difficultyName;

    [Header("====웨이브 묶음====")]
    [Tooltip("이 씬이 순서대로 실행할 웨이브 목록")]
    [SerializeField] private List<WaveDataSO> _waves = new();
        
    public string MapName => _mapName;
    public string DifficultyName => _difficultyName;
    public int WaveCount => _waves.Count;
    public IReadOnlyList<WaveDataSO> Waves => _waves;

    /// <summary>
    /// 스포너는 웨이브 1개만 실행하고, 이 SO는 그 웨이브 목록만 보관한다.
    /// </summary>
    public bool TryGetWaveAt(int index, out WaveDataSO waveData)
    {
        waveData = null;

        if (_waves == null) return false;
        if (index < 0 || index >= _waves.Count) return false;
        
        waveData = _waves[index];
        return waveData != null;
    }
    
}
