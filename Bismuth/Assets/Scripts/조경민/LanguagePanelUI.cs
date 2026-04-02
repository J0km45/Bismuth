using TMPro;
using UnityEngine;

public class LanguagePanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _languageText;

    [SerializeField] private TMP_Dropdown _languageDropdown;

    private void Start()
    {
        Language currentLanguage = LocalizationManager.Instance.CurrentLanguage;
        _languageDropdown.value = (int)currentLanguage;
        _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        RefreshText();
    }

    private void OnDestroy()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    private void RefreshText()
    {
        _languageText.text = LocalizationManager.Instance.Get("LANGUAGE");
    }

    private void OnLanguageChanged(int index)
    {
        LocalizationManager.Instance.ChangeLanguage((Language)index);
    }
}