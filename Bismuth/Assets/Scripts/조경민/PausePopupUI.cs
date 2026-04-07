using TMPro;
using UnityEngine;

public class PausePopupUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("Menu")]
    [SerializeField] private TMP_Text _menuText;
    [Tooltip("다시하기")]
    [SerializeField] private TMP_Text _resumeText;
    [Tooltip("환경설정")]
    [SerializeField] private TMP_Text _settingsText;
    [Tooltip("로비")]
    [SerializeField] private TMP_Text _lobbyText;

    [Header("━━━━ 패널 ━━━━")]
    [SerializeField] private GameObject _settingsPopup;

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    private void RefreshText()
    {
        _menuText.text = LocalizationManager.Instance.Get("MENU");
        _resumeText.text = LocalizationManager.Instance.Get("RESUME");
        _settingsText.text = LocalizationManager.Instance.Get("SETTINGS");
        _lobbyText.text = LocalizationManager.Instance.Get("LOBBY");
    }

    // 게임 재개 버튼
    public void OnClickResume()
    {
        SFXController.Instance.OnClickMenu();
        _settingsPopup.SetActive(false);
        gameObject.SetActive(false);
        TimeScaleController.Instance.SetPausePopup(false);
    }

    // 환경 설정 버튼
    public void OnClickSettings()
    {
        SFXController.Instance.OnClickMenu();
        _settingsPopup.SetActive(true);
        TimeScaleController.Instance.SetSettingsPopup(true);
    }

    // 로비복귀 버튼
    public void OnClickLobby()
    {
        SFXController.Instance.OnClickMenu();
        GameSceneManager.Instance.ChangeScene(1);
    }
}
