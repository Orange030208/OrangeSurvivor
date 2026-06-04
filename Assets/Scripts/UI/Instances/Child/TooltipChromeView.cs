using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class TooltipChromeView : MonoBehaviour, ITooltipChromeHandler
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button pinButton;
    [SerializeField] private Button closeButton;

    private TooltipSessionHandle sessionHandle;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void ApplyTooltipChrome(TooltipChromeContext context)
    {
        sessionHandle = context.SessionHandle;

        bool showPin = context.ChromeOptions.AllowUserPin && !context.IsPinned;
        bool showClose = context.IsPinned || context.ChromeOptions.ShowCloseButton;
        if (pinButton != null)
        {
            pinButton.gameObject.SetActive(showPin);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(showClose);
        }

        if (root != null)
        {
            root.SetActive(showPin || showClose);
        }
    }

    private void AddListeners()
    {
        if (pinButton != null)
        {
            pinButton.onClick.AddListener(OnPinClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void RemoveListeners()
    {
        if (pinButton != null)
        {
            pinButton.onClick.RemoveListener(OnPinClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }

    private void OnPinClicked()
    {
        if (sessionHandle.IsValid)
        {
            sessionHandle.PinAsync().Forget();
        }
    }

    private void OnCloseClicked()
    {
        if (sessionHandle.IsValid)
        {
            sessionHandle.CloseAsync().Forget();
        }
    }
}
