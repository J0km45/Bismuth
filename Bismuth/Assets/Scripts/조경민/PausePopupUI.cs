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

    private void Start()
    {
        // TODO : 수정해야됨(로컬라이징)
        _menuText.text = "MENU";
        _resumeText.text = "Resume";
        _settingsText.text = "Settings";
        _lobbyText.text = "Lobby";
    }

    // 게임 재개 버튼
    public void OnClickResume()
    {
        _settingsPopup.SetActive(false);
        gameObject.SetActive(false);
        TimeScaleController.Instance.SetPausePopup(false);
    }

    // 환경 설정 버튼
    public void OnClickSettings()
    {
        _settingsPopup.SetActive(true);
        TimeScaleController.Instance.SetSettingsPopup(true);
    }

    // 로비복귀 버튼
    public void OnClickLobby()
    {
        GameSceneManager.Instance.ChangeScene(1);
    }
}
