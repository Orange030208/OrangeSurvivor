using UnityEngine;

namespace Orange.Input
{
    public interface IInputBindingOverrideStore
    {
        string LoadBindingOverrides();

        void SaveBindingOverrides(string overridesJson);

        void ClearBindingOverrides();
    }

    public abstract class InputBindingOverrideStore : ScriptableObject, IInputBindingOverrideStore
    {
        public abstract string LoadBindingOverrides();

        public abstract void SaveBindingOverrides(string overridesJson);

        public abstract void ClearBindingOverrides();
    }
}
