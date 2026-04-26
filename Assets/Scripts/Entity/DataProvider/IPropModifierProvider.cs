using System.Collections.Generic;

public interface IPropModifierProvider
{
    IReadOnlyList<PropModifierData> PropModifierDataList { get; }
}