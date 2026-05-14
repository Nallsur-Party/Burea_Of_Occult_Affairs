using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class UIDraggableTogglePanel : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject panelRoot;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.H;
    [SerializeField] private bool startHidden = true;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (startHidden)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (WasTogglePressedThisFrame())
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        bool shouldShow = !panelRoot.activeSelf;
        panelRoot.SetActive(shouldShow);
    }

#if ENABLE_INPUT_SYSTEM
    private bool WasTogglePressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (TryGetKeyControl(keyboard, toggleKey, out KeyControl keyControl))
        {
            return keyControl.wasPressedThisFrame;
        }

        return false;
    }
#else
    private bool WasTogglePressedThisFrame()
    {
        return Input.GetKeyDown(toggleKey);
    }
#endif

#if ENABLE_INPUT_SYSTEM
    private bool TryGetKeyControl(Keyboard keyboard, KeyCode keyCode, out KeyControl keyControl)
    {
        keyControl = null;
        switch (keyCode)
        {
            case KeyCode.H:
                keyControl = keyboard.hKey;
                return true;
            case KeyCode.Escape:
                keyControl = keyboard.escapeKey;
                return true;
            case KeyCode.Tab:
                keyControl = keyboard.tabKey;
                return true;
            default:
                return false;
        }
    }
#endif
}
