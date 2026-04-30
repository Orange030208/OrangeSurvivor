using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradeCardRarityPresenter : MonoBehaviour
{
    [SerializeField] private UpgradeCardRarityShaderTarget[] shaderTargets = Array.Empty<UpgradeCardRarityShaderTarget>();
    [SerializeField] private bool includeSelfGraphicWhenTargetsEmpty = true;

    private readonly List<RuntimeMaterialBinding> runtimeBindings = new();

    public int ConfiguredTargetCount
    {
        get
        {
            int count = 0;
            UpgradeCardRarityShaderTarget[] targets = shaderTargets ?? Array.Empty<UpgradeCardRarityShaderTarget>();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i]?.TargetGraphic != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public IReadOnlyList<Material> RuntimeMaterials
    {
        get
        {
            List<Material> materials = new(runtimeBindings.Count);
            for (int i = 0; i < runtimeBindings.Count; i++)
            {
                Material material = runtimeBindings[i].AppliedMaterial;
                if (material != null)
                {
                    materials.Add(material);
                }
            }

            return materials;
        }
    }

    public void Apply(UpgradeCardRarityPresentationProfile profile)
    {
        ReleaseRuntimeMaterials();

        UpgradeCardRarityShaderTarget[] targets = ResolveShaderTargets();
        for (int i = 0; i < targets.Length; i++)
        {
            ApplyProfileToTarget(targets[i], profile);
        }
    }

    public void ConfigureTargets(IReadOnlyList<UpgradeCardRarityShaderTarget> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            shaderTargets = Array.Empty<UpgradeCardRarityShaderTarget>();
            return;
        }

        List<UpgradeCardRarityShaderTarget> configuredTargets = new(targets.Count);
        for (int i = 0; i < targets.Count; i++)
        {
            UpgradeCardRarityShaderTarget target = targets[i];
            if (target == null || target.TargetGraphic == null)
            {
                continue;
            }

            target.Validate();
            configuredTargets.Add(target);
        }

        shaderTargets = configuredTargets.ToArray();
    }

    public void ConfigureTargets(IReadOnlyList<Graphic> targetGraphics)
    {
        if (targetGraphics == null || targetGraphics.Count == 0)
        {
            shaderTargets = Array.Empty<UpgradeCardRarityShaderTarget>();
            return;
        }

        List<UpgradeCardRarityShaderTarget> configuredTargets = new(targetGraphics.Count);
        for (int i = 0; i < targetGraphics.Count; i++)
        {
            Graphic targetGraphic = targetGraphics[i];
            if (targetGraphic == null)
            {
                continue;
            }

            configuredTargets.Add(UpgradeCardRarityShaderTarget.Create(targetGraphic.name, targetGraphic));
        }

        shaderTargets = configuredTargets.ToArray();
    }

    private UpgradeCardRarityShaderTarget[] ResolveShaderTargets()
    {
        if (shaderTargets != null && shaderTargets.Length > 0)
        {
            return shaderTargets;
        }

        if (!includeSelfGraphicWhenTargetsEmpty)
        {
            return Array.Empty<UpgradeCardRarityShaderTarget>();
        }

        Graphic graphic = GetComponent<Graphic>();
        return graphic != null
            ? new[] { UpgradeCardRarityShaderTarget.Create(graphic.name, graphic) }
            : Array.Empty<UpgradeCardRarityShaderTarget>();
    }

    private void ApplyProfileToTarget(UpgradeCardRarityShaderTarget shaderTarget, UpgradeCardRarityPresentationProfile profile)
    {
        if (shaderTarget == null || shaderTarget.TargetGraphic == null)
        {
            return;
        }

        Graphic target = shaderTarget.TargetGraphic;
        Material originalMaterial = target.material;
        Material baseMaterial = shaderTarget.MaterialTemplate != null ? shaderTarget.MaterialTemplate : originalMaterial;
        if (baseMaterial == null)
        {
            return;
        }

        Material material = shaderTarget.InstantiateMaterial ? new Material(baseMaterial) : baseMaterial;
        if (shaderTarget.InstantiateMaterial)
        {
            material.name = $"{baseMaterial.name} ({profile.PresentationKey} {shaderTarget.DisplayName})";
        }

        ApplyShaderParameters(material, profile.ShaderParameters, shaderTarget.IntensityMultiplier);
        ApplyShaderParameters(material, shaderTarget.ParameterOverrides, shaderTarget.IntensityMultiplier);
        target.material = material;
        runtimeBindings.Add(new RuntimeMaterialBinding(
            target,
            originalMaterial,
            material,
            shaderTarget.InstantiateMaterial ? material : null));
    }

    private static void ApplyShaderParameters(
        Material material,
        IReadOnlyList<UpgradeCardShaderParameter> parameters,
        float targetIntensityMultiplier)
    {
        if (material == null || parameters == null)
        {
            return;
        }

        for (int i = 0; i < parameters.Count; i++)
        {
            parameters[i].ApplyTo(material, targetIntensityMultiplier);
        }
    }

    private void OnValidate()
    {
        shaderTargets ??= Array.Empty<UpgradeCardRarityShaderTarget>();
        for (int i = 0; i < shaderTargets.Length; i++)
        {
            shaderTargets[i]?.Validate();
        }
    }

    private void OnDisable()
    {
        ReleaseRuntimeMaterials();
    }

    private void OnDestroy()
    {
        ReleaseRuntimeMaterials();
    }

    private void ReleaseRuntimeMaterials()
    {
        for (int i = 0; i < runtimeBindings.Count; i++)
        {
            RuntimeMaterialBinding binding = runtimeBindings[i];
            if (binding.Target != null)
            {
                binding.Target.material = binding.OriginalMaterial;
            }

            if (binding.RuntimeMaterial != null)
            {
                DestroyMaterial(binding.RuntimeMaterial);
            }
        }

        runtimeBindings.Clear();
    }

    private static void DestroyMaterial(Material material)
    {
        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }

    private readonly struct RuntimeMaterialBinding
    {
        public RuntimeMaterialBinding(Graphic target, Material originalMaterial, Material appliedMaterial, Material runtimeMaterial)
        {
            Target = target;
            OriginalMaterial = originalMaterial;
            AppliedMaterial = appliedMaterial;
            RuntimeMaterial = runtimeMaterial;
        }

        public Graphic Target { get; }
        public Material OriginalMaterial { get; }
        public Material AppliedMaterial { get; }
        public Material RuntimeMaterial { get; }
    }
}
