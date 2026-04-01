using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputHandler : MonoBehaviour
{
    public TestAction Input { get; private set; }

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
            Input = new TestAction();
        }
    }

    private void OnEnable()
    {
        Input.Enable();

        Input.UI.Esc.performed += OnEsc;
    }

    private void OnDisable()
    {
        Input.UI.Esc.performed -= OnEsc;

        Input.Disable();
    }

    public void OnEsc(InputAction.CallbackContext context)
    {
        if (_settingsPopup.activeSelf)
        {
            _settingsPopup.SetActive(false);
            TimeScaleController.Instance.SetSettingsPopup(false);
            return;
        }
        TogglePausePopup();
    }

    public void TogglePausePopup()
    {
        _pausePopup.SetActive(!_pausePopup.activeSelf);
        TimeScaleController.Instance.SetPausePopup(_pausePopup.activeSelf);
    }
}
