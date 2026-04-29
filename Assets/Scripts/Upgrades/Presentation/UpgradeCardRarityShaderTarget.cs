using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class UpgradeCardRarityShaderTarget
{
    [SerializeField] private string displayName;
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Material materialTemplate;
    [SerializeField] private bool instantiateMaterial = true;
    [Min(0f)]
    [SerializeField] private float intensityMultiplier = 1f;
    [SerializeField] private UpgradeCardShaderParameter[] parameterOverrides = Array.Empty<UpgradeCardShaderParameter>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Target" : displayName;
    public Graphic TargetGraphic => targetGraphic;
    public Material MaterialTemplate => materialTemplate;
    public bool InstantiateMaterial => instantiateMaterial;
    public float IntensityMultiplier => Mathf.Max(0f, intensityMultiplier);
    public UpgradeCardShaderParameter[] ParameterOverrides => parameterOverrides ?? Array.Empty<UpgradeCardShaderParameter>();

    public static UpgradeCardRarityShaderTarget Create(
        string displayName,
        Graphic targetGraphic,
        Material materialTemplate,
        float intensityMultiplier,
        params UpgradeCardShaderParameter[] parameterOverrides)
    {
        return new UpgradeCardRarityShaderTarget
        {
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Target" : displayName,
            targetGraphic = targetGraphic,
            materialTemplate = materialTemplate,
            instantiateMaterial = true,
            intensityMultiplier = Mathf.Max(0f, intensityMultiplier),
            parameterOverrides = parameterOverrides ?? Array.Empty<UpgradeCardShaderParameter>()
        };
    }

    public static UpgradeCardRarityShaderTarget Create(string displayName, Graphic targetGraphic)
    {
        return Create(displayName, targetGraphic, null, 1f, Array.Empty<UpgradeCardShaderParameter>());
    }

    public void Validate()
    {
        intensityMultiplier = Mathf.Max(0f, intensityMultiplier);
        parameterOverrides ??= Array.Empty<UpgradeCardShaderParameter>();
        for (int i = 0; i < parameterOverrides.Length; i++)
        {
            parameterOverrides[i].Validate();
        }
    }
}
