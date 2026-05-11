using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class ShopInventoryPanel : ViewPartBase
{
    [SerializeField] private MonoBehaviour motionSource;
    [SerializeField] private Button toggleButton;
    [SerializeField] private InventoryUI inventoryUI;

    private IUIRuntimeMotion motion;
    private bool visible;
    private bool eventsBound;

    private void Awake()
    {
        ResolveInventoryUI();
        ValidateConfiguration();
        motion = ResolveRuntimeMotion(motionSource, "inventory sidebar");
        motion.RefreshDefaults();
    }

    private void OnDisable()
    {
        EndSession();
    }

    public void BeginSession(InventoryOperateManager inventoryOperateManager, UIManager uiManager)
    {
        BindEvents();
        ResolveInventoryUI();
        inventoryUI.ConfigureSession(inventoryOperateManager, uiManager);
        SetVisibleImmediate(false);
    }

    public void EndSession()
    {
        UnbindEvents();
        inventoryUI?.ReleaseSession();
        SetVisibleImmediate(false);
        motion?.Kill();
    }

    private void BindEvents()
    {
        if (eventsBound)
        {
            return;
        }

        toggleButton.onClick.AddListener(OnToggleRequested);
        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(OnToggleRequested);
        eventsBound = false;
    }

    private void OnToggleRequested()
    {
        AudioSfxBridge.RequestPlay(visible ? AudioSfxKey.UiCancel : AudioSfxKey.UiConfirm);
        SetVisible(!visible);
    }

    private void SetVisible(bool value)
    {
        visible = value;
        motion?.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void SetVisibleImmediate(bool value)
    {
        visible = value;
        motion?.SetImmediate(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void ResolveInventoryUI()
    {
        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInChildren<InventoryUI>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (motionSource == null)
        {
            throw new MissingReferenceException($"{nameof(ShopInventoryPanel)} '{name}' is missing motion source.");
        }

        if (toggleButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopInventoryPanel)} '{name}' is missing toggle button.");
        }

        if (inventoryUI == null)
        {
            throw new MissingReferenceException($"{nameof(ShopInventoryPanel)} '{name}' is missing inventory UI.");
        }
    }

    private IUIRuntimeMotion ResolveRuntimeMotion(MonoBehaviour source, string fieldName)
    {
        if (source is IUIRuntimeMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIRuntimeMotion resolvedMotion)
            {
                return resolvedMotion;
            }
        }

        throw new MissingComponentException($"{nameof(ShopInventoryPanel)} '{name}' expects {fieldName} to implement {nameof(IUIRuntimeMotion)}.");
    }
}
