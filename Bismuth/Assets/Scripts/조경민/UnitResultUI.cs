using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitResultUI : MonoBehaviour
{
    [Header("━━━━ 이미지 ━━━━")]
    [SerializeField] private Image _iconImage;

    [Header("━━━━ 텍스트 ━━━━")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _totalDamageText;
    [SerializeField] private TMP_Text _killCountText;

    SummonUnit.SummonedTowerRecord _record;

    public void SetData(SummonUnit.SummonedTowerRecord record)
    {
        _record = record;
        _iconImage.sprite = record.unitData.Icon;

        RefreshText();
    }

    private void RefreshText()
    {
        _nameText.text = LocalizationManager.Instance.Get(_record.unitData.UnitName);
        _totalDamageText.text = $"{LocalizationManager.Instance.Get("TOTAL_DMG")} : {FormatDamage(_record.unitStat.DealtDamage)}";
        _killCountText.text = $"{LocalizationManager.Instance.Get("KILL_COUNT")} : {_record.unitStat.KillCount}";
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
            return damage.ToString();
        }
    }
}
