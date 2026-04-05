using TMPro;
using UnityEngine;

public class DisplayPanel : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("전체화면 / 창모드")]
    [SerializeField] private TMP_Text _displayModeText;
    [Tooltip("수직동기화")]
    [SerializeField] private TMP_Text _vSyncText;
    [Tooltip("해상도")]
    [SerializeField] private TMP_Text _resolutionText;
    [Tooltip("주사율")]
    [SerializeField] private TMP_Text _refreshRateText;
    [Tooltip("폰트 크기")]
    [SerializeField] private TMP_Text _fontSizeText;

    private void OnEnable()
    {
        RefreshText();
    }

    private void RefreshText()
    {
        _displayModeText.text = LocalizationManager.Instance.Get("DISPLAYMODE");
        _vSyncText.text = LocalizationManager.Instance.Get("V_SYNC");
        _resolutionText.text = LocalizationManager.Instance.Get("RESOLUTION");
        _refreshRateText.text = LocalizationManager.Instance.Get("REFRESH_RATE");
        _fontSizeText.text = LocalizationManager.Instance.Get("FONT_SIZE");
    }
}
