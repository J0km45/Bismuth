using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputHandler : MonoBehaviour
{
    public PlayerAction Input { get; private set; }

    [Tooltip("일시정지 팝업")]
    [SerializeField] private GameObject _pausePopup;
    [SerializeField] private GameObject _settingsPopup;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        if (Input == null)
        {
            Input = new PlayerAction();
        }
    }

    private void OnEnable()
    {
        Input.Enable();

        Input.UI.Settings.performed += OnEsc;
    }

    private void OnDisable()
    {
        Input.UI.Settings.performed -= OnEsc;
        Input.Disable();
    }

    public void OnEsc(InputAction.CallbackContext context)
    {
        if (_settingsPopup.activeSelf)
        {
            SFXController.Instance.OnClickMenu();
            _settingsPopup.SetActive(false);
            TimeScaleController.Instance.SetSettingsPopup(false);
            return;
        }
        TogglePausePopup();
    }

    public void TogglePausePopup(InputAction.CallbackContext context)
    {
        TogglePausePopup();
    }

    public void TogglePausePopup()
    {
        SFXController.Instance.OnClickMenu();
        _pausePopup.SetActive(!_pausePopup.activeSelf);
        TimeScaleController.Instance.SetPausePopup(_pausePopup.activeSelf);
    }
}
