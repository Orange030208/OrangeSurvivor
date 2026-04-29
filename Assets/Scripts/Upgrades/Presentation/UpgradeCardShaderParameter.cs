using UnityEngine;

[System.Serializable]
public struct UpgradeCardShaderParameter
{
    public enum ParameterType
    {
        Float,
        Color,
        Vector,
        Texture
    }

    [SerializeField] private string propertyName;
    [SerializeField] private ParameterType parameterType;
    [SerializeField] private bool scaleWithTargetIntensity;
    [SerializeField] private float floatValue;
    [ColorUsage(false, true)]
    [SerializeField] private Color colorValue;
    [SerializeField] private Vector4 vectorValue;
    [SerializeField] private Texture textureValue;

    private UpgradeCardShaderParameter(
        string propertyName,
        ParameterType parameterType,
        bool scaleWithTargetIntensity,
        float floatValue,
        Color colorValue,
        Vector4 vectorValue,
        Texture textureValue)
    {
        this.propertyName = propertyName;
        this.parameterType = parameterType;
        this.scaleWithTargetIntensity = scaleWithTargetIntensity;
        this.floatValue = floatValue;
        this.colorValue = colorValue;
        this.vectorValue = vectorValue;
        this.textureValue = textureValue;
    }

    public string PropertyName => propertyName;
    public ParameterType Type => parameterType;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(propertyName);

    public static UpgradeCardShaderParameter Float(
        string propertyName,
        float value,
        bool scaleWithTargetIntensity = false)
    {
        return new UpgradeCardShaderParameter(
            propertyName,
            ParameterType.Float,
            scaleWithTargetIntensity,
            value,
            UnityEngine.Color.white,
            Vector4.zero,
            null);
    }

    public static UpgradeCardShaderParameter Color(
        string propertyName,
        Color value,
        bool scaleWithTargetIntensity = false)
    {
        return new UpgradeCardShaderParameter(
            propertyName,
            ParameterType.Color,
            scaleWithTargetIntensity,
            0f,
            value,
            Vector4.zero,
            null);
    }

    public static UpgradeCardShaderParameter Vector(
        string propertyName,
        Vector4 value,
        bool scaleWithTargetIntensity = false)
    {
        return new UpgradeCardShaderParameter(
            propertyName,
            ParameterType.Vector,
            scaleWithTargetIntensity,
            0f,
            UnityEngine.Color.white,
            value,
            null);
    }

    public static UpgradeCardShaderParameter Texture(string propertyName, Texture value)
    {
        return new UpgradeCardShaderParameter(
            propertyName,
            ParameterType.Texture,
            false,
            0f,
            UnityEngine.Color.white,
            Vector4.zero,
            value);
    }

    public void ApplyTo(Material material, float targetIntensityMultiplier)
    {
        if (material == null || string.IsNullOrWhiteSpace(propertyName) || !material.HasProperty(propertyName))
        {
            return;
        }

        float multiplier = scaleWithTargetIntensity ? Mathf.Max(0f, targetIntensityMultiplier) : 1f;
        switch (parameterType)
        {
            case ParameterType.Float:
                material.SetFloat(propertyName, floatValue * multiplier);
                break;
            case ParameterType.Color:
                material.SetColor(propertyName, colorValue * multiplier);
                break;
            case ParameterType.Vector:
                material.SetVector(propertyName, vectorValue * multiplier);
                break;
            case ParameterType.Texture:
                if (textureValue != null)
                {
                    material.SetTexture(propertyName, textureValue);
                }
                break;
            default:
                return;
        }
    }

    public void Validate()
    {
        if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
        {
            floatValue = 0f;
        }
    }
}
