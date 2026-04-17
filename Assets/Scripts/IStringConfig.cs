using System.Collections.Generic;

/// <summary>
/// 运行时配置字符串驱动接口。
/// </summary>
public interface IStringConfig
{
    List<string> GetOptionList();
    void ApplyConfigByString(string selectedOption);
}