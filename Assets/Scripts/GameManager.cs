using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager: MonoBehaviour
{
    [SerializeField]
    CanvasGroup uiCanvas;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            uiCanvas.alpha = uiCanvas.alpha == 1 ? 0 : 1;
        }
    }
}
