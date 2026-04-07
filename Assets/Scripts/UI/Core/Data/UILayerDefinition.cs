using System;

[Serializable]
public sealed class UILayerDefinition
{
    public UILayerType layerType = UILayerType.Default;
    public int sortingOrder;
    public bool blocksRaycasts = true;
}
