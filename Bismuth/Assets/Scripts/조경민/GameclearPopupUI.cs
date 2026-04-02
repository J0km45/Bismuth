using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameclearPopupUI : MonoBehaviour
{
    [SerializeField] private SummonUnit _summonUnit;

    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("총 데미지")]
    [SerializeField] private TMP_Text _totalDamageText;
    [Tooltip("mvp의 데미지( ex)192M )")]
    [SerializeField] private TMP_Text _damageText;
    [Tooltip("클리어")]
    [SerializeField] private TMP_Text _clearText;
    [Tooltip("스테이지 이름")]
    [SerializeField] private TMP_Text _stageText;
    //[Tooltip("다시시작")]
    //[SerializeField] private TMP_Text _retryText;
    [Tooltip("로비화면")]
    [SerializeField] private TMP_Text _mainText;

    [Header("━━━━ 이미지 ━━━━")]
    [Tooltip("MVP 이미지")]
    [SerializeField] private Image _mvpImage;

    [Header("━━━━ 유닛결과 ━━━━")]
    [Tooltip("프리팹 생성위치")]
    [SerializeField] private Transform _unitResultContent;
    [Tooltip("유닛결과(프리팹)")]
    [SerializeField] private UnitResultUI _unitResultPrefab;

    private void OnEnable()
    {
        RefreshText();
    }

    private void RefreshText()
    {
        _totalDamageText.text = LocalizationManager.Instance.Get("TOTAL_DMG");
        _clearText.text = LocalizationManager.Instance.Get("CLEAR");
        _mainText.text = LocalizationManager.Instance.Get("LOBBY");
    }

    public void ShowResult(string mapName, string difficulty)
    {
        List<SummonUnit.SummonedTowerRecord> sortedList = _summonUnit.OwnedTowers
            .OrderByDescending(unit => unit.unitStat.DealtDamage)  // 딜량 높은 순
            .ThenByDescending(unit => unit.unitStat.KillCount)     // 딜량이 같다면 킬수 높은 순
            .ToList();

        SummonUnit.SummonedTowerRecord mvp = sortedList[0];
        _mvpImage.sprite = mvp.unitData.Illustration;
        _damageText.text = FormatDamage(mvp.unitStat.DealtDamage);

        _stageText.text = $"{LocalizationManager.Instance.Get(mapName)} - {LocalizationManager.Instance.Get(difficulty)}";

        foreach (SummonUnit.SummonedTowerRecord record in sortedList)
        {
            UnitResultUI item = Instantiate(_unitResultPrefab, _unitResultContent);
            item.SetData(record);
        }
    }

    private string FormatDamage(int damage)
    {
        if (damage >= 1000000f)
        {
            return $"{damage / 1000000f:0.#}M";
        }
        else if (damage >= 1000f)
        {
            return $"{damage / 1000f:0.#}K";
        }
        else
        {
            return damage.ToString("0");
        }
    }

    //public void OnClickRetry()
    //{
    //    GameSceneManager.Instance.ReloadScene();
    //}

    public void OnClickMain()
    {
        GameSceneManager.Instance.ChangeScene(1);
    }
}
