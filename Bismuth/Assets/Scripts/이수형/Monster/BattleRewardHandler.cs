using UnityEngine;

/// <summary>
/// BattleWaveRunner의 보상 이벤트를 구독하여
/// 인간 시너지 보너스를 반영한 최종 보상을 PlayerDataManager에 지급한다.
/// 시너지 참조 패턴은 DamageCalculator와 동일하게 SynergySO 직접 참조 방식을 사용한다.
/// </summary>
public class BattleRewardHandler : MonoBehaviour
{
    private const int HumanSynergyId = (int)SynergyManager.SynergyType.Human;

    [Header("====참조====")]
    [Tooltip("보상 이벤트를 구독할 전투 진행자")]
    [SerializeField] private BattleWaveRunner _battleWaveRunner;

    [Tooltip("최종 보상을 지급할 플레이어 데이터")]
    [SerializeField] private PlayerDataManager _playerDataManager;

    [Header("====시너지====")]
    [Tooltip("시너지 효과값 조회용 SO")]
    [SerializeField] private SynergySO _synergySO;

    [Tooltip("시너지 활성 수 조회용 매니저")]
    [SerializeField] private SynergyManager _synergyManager;

    [Header("====디버그====")]
    [SerializeField] private bool _log = true;

    private bool _warnedMissingSynergySo;
    private bool _warnedMissingSynergyManager;

    private void Awake()
    {
        _playerDataManager = GetComponentInParent<PlayerDataManager>();
        _synergyManager = GetComponentInParent<SynergyManager>();
    }

    private void Start()
    {
        DebugTool.DebugSelect(DebugType.Synergy, _log);
    }

    private void OnEnable()
    {
        if (_battleWaveRunner == null)
        {
            DebugTool.Error("BattleWaveRunner 참조가 비어 있습니다.", DebugType.Synergy, this);
            return;
        }

        _battleWaveRunner.KillRewardReady += HandleKillRewardReady;
        _battleWaveRunner.WaveClearRewardReady += HandleWaveClearRewardReady;
    }

    private void OnDisable()
    {
        if (_battleWaveRunner == null)
            return;

        _battleWaveRunner.KillRewardReady -= HandleKillRewardReady;
        _battleWaveRunner.WaveClearRewardReady -= HandleWaveClearRewardReady;
    }

    // ─── 처치 보상 : 기본 보상 + 인간 시너지 보너스 ───
    private void HandleKillRewardReady(MonsterController monster, int baseReward)
    {
        if (_playerDataManager == null)
        {
            DebugTool.Error("PlayerDataManager 참조가 비어 있습니다.", DebugType.Synergy, this);
            return;
        }

        int humanBonus = GetHumanSynergyBonus();
        int finalReward = baseReward + humanBonus;

        _playerDataManager.Gold += finalReward;

        if (_log)
        {
            string monsterName = monster != null ? monster.name : "알 수 없음";
            DebugTool.Log(
                $"[처치 보상] 대상 : {monsterName} / 기본 : {baseReward} / 인간 시너지 : +{humanBonus} / 최종 : {finalReward} / 소지금 : {_playerDataManager.Gold}",
                DebugType.Synergy,
                this
            );
        }
    }

    // ─── 웨이브 클리어 보상 : 시너지 보너스 없이 기본 보상만 ───
    private void HandleWaveClearRewardReady(WaveDataSO waveData, int reward)
    {
        if (_playerDataManager == null)
        {
            DebugTool.Error("PlayerDataManager 참조가 비어 있습니다.", DebugType.Synergy, this);
            return;
        }

        _playerDataManager.Gold += reward;

        if (_log)
        {
            int waveNumber = waveData != null ? waveData.WaveNumber : -1;
            DebugTool.Log(
                $"[웨이브 클리어 보상] 웨이브 : {waveNumber} / 보상 : {reward} / 소지금 : {_playerDataManager.Gold}",
                DebugType.Synergy,
                this
            );
        }
    }

    // ─── 인간 시너지 보너스 조회 (DamageCalculator 패턴) ───
    private int GetHumanSynergyBonus()
    {
        if (_synergyManager == null)
        {
            WarnMissingSynergyManager();
            return 0;
        }

        if (_synergySO == null)
        {
            WarnMissingSynergySo();
            return 0;
        }

        SynergyData humanData = GetSynergyData(HumanSynergyId);
        if (humanData == null || humanData.Levels == null || humanData.Levels.Count == 0)
            return 0;

        int activeCount = _synergyManager.GetSynergyLevel(HumanSynergyId);
        float bonus = GetMatchedBonus(humanData, activeCount);

        return Mathf.RoundToInt(bonus);
    }

    private SynergyData GetSynergyData(int synergyId)
    {
        if (_synergySO == null || _synergySO.Rows == null)
            return null;

        for (int i = 0; i < _synergySO.Rows.Count; i++)
        {
            SynergyData data = _synergySO.Rows[i];
            if (data != null && data.ID == synergyId)
                return data;
        }

        return null;
    }

    private float GetMatchedBonus(SynergyData synergyData, int activeCount)
    {
        float bonus = 0f;

        for (int i = 0; i < synergyData.Levels.Count; i++)
        {
            SynergyLevelData level = synergyData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            if (level.EffectValues == null || level.EffectValues.Count == 0)
                continue;

            bonus = level.EffectValues[0];
        }

        return bonus;
    }

    private void WarnMissingSynergyManager()
    {
        if (_warnedMissingSynergyManager)
            return;

        _warnedMissingSynergyManager = true;
        DebugTool.Warnning("SynergyManager 참조가 없어 인간 시너지 보너스를 적용하지 않습니다.", DebugType.Synergy, this);
    }

    private void WarnMissingSynergySo()
    {
        if (_warnedMissingSynergySo)
            return;

        _warnedMissingSynergySo = true;
        DebugTool.Warnning("SynergySO 참조가 없어 인간 시너지 보너스를 적용하지 않습니다.", DebugType.Synergy, this);
    }
}
