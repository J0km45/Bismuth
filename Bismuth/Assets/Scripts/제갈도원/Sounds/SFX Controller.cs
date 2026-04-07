using UnityEngine;

public class SFXController : MonoBehaviour
{
    public static SFXController Instance { get; private set; }
    
    [SerializeField] private SFXSO _sfxso;
    [SerializeField] private AudioSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _source = GetComponent<AudioSource>();
    }
    // 보스 시작
    public void OnStartBoss()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.StartBoss]);
    }
    // 유닛 소환
    public void OnDrawSuccess()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Spawn]);
    }
    // 레벨 선택
    public void OnSelectLevel()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.SelectLevel]);
    }
    // 합성
    public void OnMerge()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Merge]);
    }
    // 유닛 판매
    public void OnUnitSell()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Sell]); 
    }
    // 게임 오버
    public void OnGameOver()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.GameOver]); 
    }
    // 게임 클리어
    public void OnGameVictory()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Final_Victory]); 
    }
    // 게임 클리어
    public void OnEnforce()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Enforce]); 
    }
    // 게임 클리어
    public void OnEnforceGatcha()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Enforce_Gatcha]); 
    }
    // 재화 부족으로 인한 실패
    public void OnUIFailure()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Controll_Fail]);
    }
    // 메뉴 클릭
    public void OnClickMenu()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.Click_Menu]); 
    }
    // 유닛 배치 실패
    public void OnBatchFail()
    {
        AudioManager.Instance.PlayUI(_source, _sfxso.SFXList[(int)SFXType.BatchFail]); 
    }
}
