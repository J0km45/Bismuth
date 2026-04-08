using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TitleSceneUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("게임 시작")]
    [SerializeField] private TMP_Text _startText;
    [Tooltip("환경설정")]
    [SerializeField] private TMP_Text _settingsText;
    [Tooltip("게임종료")]
    [SerializeField] private TMP_Text _quitText;

    [Header("━━━━ 패널 ━━━━")]
    [Tooltip("환경 설정 팝업")]
    [SerializeField] private GameObject _settingsPopup;
    
    [Header("━━━━ 이미지 ━━━━")]
    [Tooltip("캐릭터 이미지")]
    [SerializeField] private Image _characterImage;
    [SerializeField] private Sprite[] _characterSprites;

    private void Awake()
    {
        _characterImage.sprite = _characterSprites[Random.Range(0, _characterSprites.Length)];
    }

    private void Start()
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
        _startText.text = LocalizationManager.Instance.Get("START_GAME");
        _settingsText.text = LocalizationManager.Instance.Get("OPTIONS");
        _quitText.text = LocalizationManager.Instance.Get("QUIT");
    }

    // 게임 시작 버튼 (로비화면으로)
    public void OnClickStart()
    {
        SFXController.Instance.OnClickMenu();
        GameSceneManager.Instance.LoadNextStage();
    }

    // 환경 설정 버튼
    public void OnClickSettings()
    {
        SFXController.Instance.OnClickMenu();
        _settingsPopup.SetActive(true);
    }

    // 게임 종료 버튼
    public void OnClickQuit()
    {
        SFXController.Instance.OnClickMenu();
        GameSceneManager.Instance.GameQuit();
    }

    public void OnClickOpenURL(string url)
    {
        SFXController.Instance.OnClickMenu();
        Application.OpenURL(url);
    }
}
