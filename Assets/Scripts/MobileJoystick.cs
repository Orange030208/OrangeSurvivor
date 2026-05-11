using UnityEngine;
using UnityEngine.InputSystem;

public class MobileJoystick : MonoBehaviour
{
    [Header("元素")]
    [SerializeField] private RectTransform joystickOutline;
    [SerializeField] private RectTransform joystickKnob;

    [Header("设置")]
    [SerializeField] private float moveFactor;

    private Canvas parentCanvas;
    private RectTransform parentCanvasRectTransform;
    private Vector3 clickedPosition;
    private Vector3 move;
    private bool canControl;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        parentCanvasRectTransform = parentCanvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        HideJoystick();
    }

    private void OnDisable()
    {
        HideJoystick();
    }

    private void Update()
    {
        if (canControl)
        {
            ControlJoystick();
        }
    }

    public void ClickedOnJoystickZoneCallback()
    {
        clickedPosition = ReadPointerPosition();
        joystickOutline.position = clickedPosition;
        ShowJoystick();
    }

    public Vector2 GetMoveDirection()
    {
        return move / GetCanvasScale();
    }

    private void ShowJoystick()
    {
        joystickOutline.gameObject.SetActive(true);
        canControl = true;
    }

    private void HideJoystick()
    {
        joystickOutline.gameObject.SetActive(false);
        canControl = false;
        move = Vector3.zero;
    }

    private void ControlJoystick()
    {
        Vector3 currentPosition = ReadPointerPosition();
        Vector3 direction = currentPosition - clickedPosition;

        float canvasScale = GetCanvasScale();
        float moveMagnitude = direction.magnitude * moveFactor * canvasScale;

        float absoluteWidth = joystickOutline.rect.width / 2;
        float realWidth = absoluteWidth * canvasScale;

        moveMagnitude = Mathf.Min(moveMagnitude, realWidth);
        move = direction.normalized * moveMagnitude;

        Vector3 targetPosition = clickedPosition + move;
        joystickKnob.position = targetPosition;

        if (IsPointerReleased())
        {
            HideJoystick();
        }
    }

    private static Vector3 ReadPointerPosition()
    {
        if (Pointer.current != null)
        {
            Vector2 pointerPosition = Pointer.current.position.ReadValue();
            return new Vector3(pointerPosition.x, pointerPosition.y, 0f);
        }

        return Vector3.zero;
    }

    private static bool IsPointerReleased()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current == null || Touchscreen.current.primaryTouch.press.isPressed)
        {
            return false;
        }

        return Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
    }

    private float GetCanvasScale()
    {
        return parentCanvasRectTransform.localScale.x;
    }
}
