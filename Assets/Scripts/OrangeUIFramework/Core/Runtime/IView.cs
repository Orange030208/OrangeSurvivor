namespace Orange.UIFramework
{
    public interface IView
    {
        string InstanceId { get; }
        bool IsOpen { get; }
        bool InputActive { get; }
        bool BlocksRaycasts { get; }
        bool RequiresTick { get; }
        ViewRuntimePhase Phase { get; }

        void Initialize(ViewHandle handle);
        void ApplyInputState(bool interactable, bool blocksRaycasts);
        void Tick(float deltaTime);
    }
}
