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

[CreateAssetMenu(fileName = "MapBattleConfigSO", menuName = "Data/Battle/Map Battle Config")]
public class MapBattleConfigSO : ScriptableObject
{
    [Header("====맵 기본 정보====")]
    [Tooltip("맵의 아이디")]
    [SerializeField, Min(0)] private int _mapId;
    
    [Tooltip("맵의 이름")]
    [SerializeField] private string _mapName;
    
    [Header("====난이도별 웨이브 시트====")]
    [Tooltip("맵에서 선택 가능한 난이도별 웨이브 세트 목록")]
    [SerializeField] private List<DifficultyWaveSet> _difficultyWaveSets = new();
        
    public int MapId => _mapId;
    public string MapName => _mapName;
    public IReadOnlyList<DifficultyWaveSet> DifficultyWaveSets => _difficultyWaveSets;

    public bool TryGetWaveSet(BattleDifficulty difficulty, out DifficultyWaveSet result)
    {
        foreach (DifficultyWaveSet waveSet in _difficultyWaveSets)
        {
            if (waveSet.Difficulty == difficulty)
            {
                result = waveSet;
                return true;
            }
        }
        
        result = null;
        return false;
    }
    
}
