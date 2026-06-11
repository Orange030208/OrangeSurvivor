using UnityEngine;

namespace Orange.Input
{
    [CreateAssetMenu(menuName = "Orange/Input/PlayerPrefs Binding Override Store", fileName = "PlayerPrefs Binding Override Store")]
    public sealed class PlayerPrefsInputBindingOverrideStore : InputBindingOverrideStore
    {
        [SerializeField] private string playerPrefsKey = "Orange.Input.BindingOverrides";

        public override string LoadBindingOverrides()
        {
            return string.IsNullOrWhiteSpace(playerPrefsKey)
                ? string.Empty
                : PlayerPrefs.GetString(playerPrefsKey, string.Empty);
        }

        public override void SaveBindingOverrides(string overridesJson)
        {
            if (string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                return;
            }

            PlayerPrefs.SetString(playerPrefsKey, overridesJson ?? string.Empty);
            PlayerPrefs.Save();
        }

        public override void ClearBindingOverrides()
        {
            if (string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
