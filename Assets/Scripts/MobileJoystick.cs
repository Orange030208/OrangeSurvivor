using UnityEngine;

public class MobileJoystick : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private RectTransform joystickOutline;
    [SerializeField] private RectTransform joystickKnob;

    [Header(" Settings ")]
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
        clickedPosition = Input.mousePosition;
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
        Vector3 currentPosition = Input.mousePosition;
        Vector3 direction = currentPosition - clickedPosition;

        float canvasScale = GetCanvasScale();
        float moveMagnitude = direction.magnitude * moveFactor * canvasScale;

        float absoluteWidth = joystickOutline.rect.width / 2;
        float realWidth = absoluteWidth * canvasScale;

        moveMagnitude = Mathf.Min(moveMagnitude, realWidth);
        move = direction.normalized * moveMagnitude;

        Vector3 targetPosition = clickedPosition + move;
        joystickKnob.position = targetPosition;

        if (Input.GetMouseButtonUp(0))
        {
            HideJoystick();
        }
    }

    private float GetCanvasScale()
    {
        return parentCanvasRectTransform.localScale.x;
    }
}