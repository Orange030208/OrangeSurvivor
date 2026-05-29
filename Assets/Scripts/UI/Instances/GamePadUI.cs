using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class GamePadUI : PopupBase
{
    [SerializeField] private MobileJoystick moveJoystick;
    [SerializeField] private PlatformSettingsProfileSO[] platformProfiles = Array.Empty<PlatformSettingsProfileSO>();

    private IPlayerMoveInputReceiver moveInputReceiver;
    private bool touchControlsEnabled;

    public override bool RequiresTick => true;

    public bool AllowsTouchControlsOnPlatform(RuntimePlatform platform)
    {
        return TouchControlsPlatformPolicy.IsTouchControlsEnabled(platformProfiles, platform);
    }

    public static bool IsRegisteredTouchControlsEnabled(ViewCatalog catalog, RuntimePlatform platform)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (!catalog.TryFindByType<GamePadUI>(out ViewDefinition definition))
        {
            throw new InvalidOperationException($"{nameof(GamePadUI)} must be registered in {nameof(ViewCatalog)} before it can be opened.");
        }

        GamePadUI gamePadPrefab = definition.Prefab != null
            ? definition.Prefab.GetComponent<GamePadUI>()
            : null;
        if (gamePadPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(GamePadUI)} registration must reference a prefab with {nameof(GamePadUI)} on the root.");
        }

        return gamePadPrefab.AllowsTouchControlsOnPlatform(platform);
    }

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        GamePadUIContext gamePadContext = context.GetPayload<GamePadUIContext>()
            ?? throw new InvalidOperationException($"{nameof(GamePadUI)} requires {nameof(GamePadUIContext)} payload.");
        BindInput(gamePadContext.Player);
        ApplyTouchControlsState();
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        SetTouchControlsEnabled(false);
        UnbindInput();
    }

    protected override void OnInputChanged(bool interactable, bool blocksRaycasts)
    {
        if (!interactable)
        {
            moveInputReceiver?.SetMoveInput(Vector2.zero);
        }
    }

    protected override void OnTick(float deltaTime)
    {
        if (!InputActive)
        {
            moveInputReceiver?.SetMoveInput(Vector2.zero);
            return;
        }

        moveInputReceiver?.SetMoveInput(ReadMoveDirection());
    }

    private void BindInput(Player player)
    {
        moveInputReceiver = player != null ? player.GetComponent<IPlayerMoveInputReceiver>() : null;
        moveInputReceiver?.SetMoveInput(Vector2.zero);
    }

    private void UnbindInput()
    {
        moveInputReceiver?.SetMoveInput(Vector2.zero);
        moveInputReceiver = null;
    }

    private Vector2 ReadMoveDirection()
    {
        GameInput input = GameInput.Instance;
        Vector2 inputMove = input != null ? input.Move : Vector2.zero;
        if (inputMove.sqrMagnitude > 0.0001f)
        {
            return Vector2.ClampMagnitude(inputMove, 1f);
        }

        return touchControlsEnabled ? moveJoystick.GetMoveDirection() : Vector2.zero;
    }

    private void ApplyTouchControlsState()
    {
        SetTouchControlsEnabled(AllowsTouchControlsOnPlatform(Application.platform));
    }

    private void SetTouchControlsEnabled(bool enabled)
    {
        touchControlsEnabled = enabled;
        moveJoystick.SetInputEnabled(enabled);
    }

    private void ValidateConfiguration()
    {
        if (moveJoystick == null)
        {
            throw new MissingReferenceException($"{nameof(GamePadUI)} '{name}' is missing mobile joystick.");
        }
    }
}
