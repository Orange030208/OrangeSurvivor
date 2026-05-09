using UnityEngine;

[CreateAssetMenu(fileName = "Fact Definition", menuName = ScriptableObjectMenuPaths.CONTENT_FACT_DEFINITION, order = 0)]
public class FactDefinitionSO : ScriptableObject
{
    private const string FACT_ID_PREFIX = "Fact_";

    [SerializeField] private string factId = FACT_ID_PREFIX;
    [SerializeField] private string displayName;
    [SerializeField] private FactValueType valueType = FactValueType.Float;
    [SerializeField] private FactDefinitionBuiltInKind builtInKind;

    [Header("Built-in Parameters")]
    [SerializeField] private PropType propType;
    [SerializeField] private UpgradeCardTag upgradeCardTag;
    [SerializeField] private WeaponTag weaponTag;
    [SerializeField] private WeaponDataSO weaponData;

    public string FactId => string.IsNullOrWhiteSpace(factId) ? name : factId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? FactId : displayName;
    public FactValueType ValueType => valueType;
    public FactDefinitionBuiltInKind BuiltInKind => builtInKind;
    public PropType PropType => propType;
    public UpgradeCardTag UpgradeCardTag => upgradeCardTag;
    public WeaponTag WeaponTag => weaponTag;
    public WeaponDataSO WeaponData => weaponData;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(factId))
        {
            factId = FACT_ID_PREFIX;
        }
    }

    public void InitializeRuntime(
        string runtimeFactId,
        FactValueType runtimeValueType,
        FactDefinitionBuiltInKind runtimeBuiltInKind = FactDefinitionBuiltInKind.None)
    {
        factId = string.IsNullOrWhiteSpace(runtimeFactId) ? FACT_ID_PREFIX : runtimeFactId;
        displayName = runtimeFactId;
        valueType = runtimeValueType;
        builtInKind = runtimeBuiltInKind;
    }
}
