using TMPro;
using UnityEngine;

public class GameoverPopupUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("Game Over")]
    [SerializeField] private TMP_Text _gameoverText;
    [Tooltip("다시시작")]
    [SerializeField] private TMP_Text _retryText;
    [Tooltip("메인화면")]
    [SerializeField] private TMP_Text _mainText;

    private void OnEnable()
    {
        RefreshText();
    }

    private void RefreshText()
    {
        _gameoverText.text = LocalizationManager.Instance.Get("GAME_OVER");
        _retryText.text = LocalizationManager.Instance.Get("RESTART");
        _mainText.text = LocalizationManager.Instance.Get("MAIN");
    }

    public void OnClickRetry()
    {
        GameSceneManager.Instance.ReloadScene();
    }

    public void OnClickMain()
    {
        GameSceneManager.Instance.LoadTitle();
    }
}