//任何想显示在描述UI的东西都必须实现这个

using System.Collections.Generic;
using UnityEngine;

public interface IDescribable
{
    string Title { get; }
    Sprite Icon { get; }
    string Description { get; }
    IEnumerable<DescriptorInfo> GetExtraInfos();

    public static readonly IDescribable Default = new DefaultDescribe();
}

internal class DefaultDescribe:IDescribable
{
    public string Title { get; set; }
    public Sprite Icon { get;set; }
    public string Description { get; set; }
    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return new List<DescriptorInfo>();
    }
}

[System.Serializable]
public struct DescriptorInfo
{
    public string label;
    public string value;
    
    public DescriptorInfo(string label, string value)
    {
        this.label = label;
        this.value = value;
    }
}