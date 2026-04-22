using UnityEngine;

/// <summary>
/// 描述器
/// </summary>
public abstract class Describer:MonoBehaviour
{
    public abstract void Display(IDescribable describer);
}