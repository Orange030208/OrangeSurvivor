using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MobileJoystick : ViewPartBase
{
    [Header("元素")]
    [SerializeField] private GameObject inputRoot;
    [SerializeField] private RectTransform joystickOutline;
    [SerializeField] private RectTransform joystickKnob;

    [Header("设置")]
    [SerializeField] private float moveFactor;

    private Canvas parentCanvas;
    private RectTransform parentCanvasRectTransform;
    private Graphic[] inputRootGraphics;
    private EventTrigger[] inputRootEventTriggers;
    private Vector3 clickedPosition;
    private Vector3 move;
    private bool canControl;

    private bool inputEnabled = true;

    private void Awake()
    {
        ResolveReferences();
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
        if (inputEnabled && canControl)
        {
            ControlJoystick();
        }
    }

    public override UniTask ShowAsync(CancellationToken cancellationToken = default)
    {
        SetInputEnabled(true);
        return UniTask.CompletedTask;
    }

    public override UniTask HideAsync(CancellationToken cancellationToken = default)
    {
        SetInputEnabled(false);
        return UniTask.CompletedTask;
    }

    public void SetInputEnabled(bool enabled)
    {
        ResolveReferences();
        inputEnabled = enabled;

        if (!enabled || !canControl)
        {
            HideJoystick();
        }

        SetInputRootEnabled(enabled);
    }

    public void ClickedOnJoystickZoneCallback()
    {
        if (!inputEnabled)
        {
            return;
        }

        clickedPosition = ReadPointerPosition();
        joystickOutline.position = clickedPosition;
        ShowJoystick();
    }

    public Vector2 GetMoveDirection()
    {
        return inputEnabled ? move / GetCanvasScale() : Vector2.zero;
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

    private void ResolveReferences()
    {
        if (inputRoot == null)
        {
            inputRoot = transform.parent != null ? transform.parent.gameObject : gameObject;
        }

        if (joystickOutline == null)
        {
            throw new MissingReferenceException($"{nameof(MobileJoystick)} '{name}' is missing joystick outline.");
        }

        if (joystickKnob == null)
        {
            throw new MissingReferenceException($"{nameof(MobileJoystick)} '{name}' is missing joystick knob.");
        }

        parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas == null)
        {
            throw new MissingReferenceException($"{nameof(MobileJoystick)} '{name}' requires a parent Canvas.");
        }

        parentCanvasRectTransform = parentCanvas.GetComponent<RectTransform>();
        if (parentCanvasRectTransform == null)
        {
            throw new MissingComponentException($"{nameof(MobileJoystick)} parent canvas '{parentCanvas.name}' requires a RectTransform.");
        }
    }

    private void SetInputRootEnabled(bool enabled)
    {
        if (inputRoot.GetComponent<ViewBase>() == null)
        {
            inputRoot.SetActive(enabled);
            return;
        }

        inputRootGraphics ??= inputRoot.GetComponentsInChildren<Graphic>(true);
        inputRootEventTriggers ??= inputRoot.GetComponentsInChildren<EventTrigger>(true);

        for (int i = 0; i < inputRootGraphics.Length; i++)
        {
            if (inputRootGraphics[i] != null)
            {
                inputRootGraphics[i].raycastTarget = enabled;
            }
        }

        for (int i = 0; i < inputRootEventTriggers.Length; i++)
        {
            if (inputRootEventTriggers[i] != null)
            {
                inputRootEventTriggers[i].enabled = enabled;
            }
        }
    }
}
