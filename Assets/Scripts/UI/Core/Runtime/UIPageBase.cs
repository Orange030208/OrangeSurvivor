using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPageBase : MonoBehaviour, IUIPage
{
    private CanvasGroup canvasGroup;

    private string instanceId = string.Empty;
    private bool isVisible;

    public System.Type PageType => GetType();
    public string InstanceId => instanceId;
    public bool IsVisible => isVisible;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetupInstance(string newInstanceId)
    {
        if (string.IsNullOrWhiteSpace(newInstanceId))
        {
            throw new System.ArgumentException("SetupInstance failed: newInstanceId is null or empty.", nameof(newInstanceId));
        }

        instanceId = newInstanceId;
    }

    public void HandleOpen(UIPageOpenContext context)
    {
        ValidateCanvasGroup();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        isVisible = true;
        OnPageOpened(context);
    }

    public void HandleClose()
    {
        ValidateCanvasGroup();
        OnPageClosed();
        isVisible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HandleFocusChanged(bool hasFocus)
    {
        ValidateCanvasGroup();
        canvasGroup.interactable = hasFocus;
        canvasGroup.blocksRaycasts = hasFocus;
        OnFocusChanged(hasFocus);
    }

    public void HandleTick(float deltaTime)
    {
        if (!isVisible)
        {
            return;
        }

        OnPageTick(deltaTime);
    }

    protected virtual void OnPageOpened(UIPageOpenContext context)
    {
    }

    protected virtual void OnPageClosed()
    {
    }

    protected virtual void OnFocusChanged(bool hasFocus)
    {
    }

    protected virtual void OnPageTick(float deltaTime)
    {
    }

    private void ValidateCanvasGroup()
    {
        if (canvasGroup == null)
        {
            throw new MissingReferenceException($"UIPage '{name}' is missing CanvasGroup reference.");
        }
    }
}
