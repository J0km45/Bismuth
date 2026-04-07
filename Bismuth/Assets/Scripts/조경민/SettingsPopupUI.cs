using TMPro;
using UnityEngine;

public class SettingsPopupUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("Screen")]
    [SerializeField] private TMP_Text _displayText;
    [Tooltip("Control")]
    [SerializeField] private TMP_Text _controlText;
    [Tooltip("Volume")]
    [SerializeField] private TMP_Text _volumeText;
    [Tooltip("Language")]
    [SerializeField] private TMP_Text _languageText;

    [Header("━━━━ 패널 ━━━━")] 
    [SerializeField] private GameObject _displayPanel;
    [SerializeField] private GameObject _controlPanel;
    [SerializeField] private GameObject _volumePanel;
    [SerializeField] private GameObject _languagePanel;

    private void Start()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        RefreshText();
    }
    

    private void OnDestroy()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    private void RefreshText()
    {
        _displayText.text = LocalizationManager.Instance.Get("DISPLAY");
        _controlText.text = LocalizationManager.Instance.Get("CONTROL");
        _volumeText.text = LocalizationManager.Instance.Get("SOUND");
        _languageText.text = LocalizationManager.Instance.Get("LANGUAGE");
    }

    public void OnClickShowDisplay()
    {
        SFXController.Instance.OnClickMenu();
        ShowPanel(_displayPanel);
    }

    public void OnClickShowControl()
    {
        SFXController.Instance.OnClickMenu();
        ShowPanel(_controlPanel);
    }

    public void OnClickShowVolume()
    {
        SFXController.Instance.OnClickMenu();
        ShowPanel(_volumePanel);
    }

    public void OnClickShowLanguage()
    {
        SFXController.Instance.OnClickMenu();
        ShowPanel(_languagePanel);
    }

    public void OnClickClosePopup()
    {
        SFXController.Instance.OnClickMenu();
        gameObject.SetActive(false);
        TimeScaleController.Instance.SetSettingsPopup(false);
    }

    private void ShowPanel(GameObject targetPanel)
    {
        _displayPanel.SetActive(false);
        _controlPanel.SetActive(false);
        _volumePanel.SetActive(false);
        _languagePanel.SetActive(false);

        targetPanel.SetActive(true);
    }
}
