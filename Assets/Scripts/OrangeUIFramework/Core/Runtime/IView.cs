using Cysharp.Threading.Tasks;

namespace Orange.UIFramework
{
    public interface IView
    {
        string InstanceId { get; }
        bool IsOpen { get; }

        void Initialize(ViewHandle handle);
        void ApplyInputState(bool interactable, bool blocksRaycasts);
        void Tick(float deltaTime);
    }
}
