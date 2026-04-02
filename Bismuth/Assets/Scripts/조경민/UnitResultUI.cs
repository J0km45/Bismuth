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

    public void SetData(SummonUnit.SummonedTowerRecord record)
    {
        _nameText.text = record.unitData.UnitName;
        _iconImage.sprite = record.unitData.Icon;
        _totalDamageText.text = FormatDamage(record.unitStat.DealtDamage);
        _killCountText.text = record.unitStat.KillCount.ToString();
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
