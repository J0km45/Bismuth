using UnityEngine;

[CreateAssetMenu(fileName = "SFX List", menuName = "Bismuth/SFX database", order = 0)]
public class SFXSO : ScriptableObject
{
    [Header("BGM List")]
    [SerializeField] private AudioClip[] _BGMList;
    [Header("UI SFX List")]
    [SerializeField] private AudioClip[] _SFXList;

    public AudioClip[] BGMList => _BGMList;
    public AudioClip[] SFXList => _SFXList;
    
    
}
public enum BGMType
{
    Main,
    Forest,
    FortTown,
    Ruins,
    Boss,
    None
}

public enum SFXType
{
    StartBoss,
    Spawn,
    Sell,
    SelectLevel,
    Merge,
    GameOver,
    Final_Victory,
    Enforce,
    Enforce_Gatcha,
    Controll_Fail,
    Click_Menu,
    BatchFail,
    None
}