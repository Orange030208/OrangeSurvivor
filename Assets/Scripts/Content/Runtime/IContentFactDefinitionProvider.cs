using System.Collections.Generic;

public interface IContentFactDefinitionProvider
{
    void CollectFactDefinitions(List<FactDefinitionSO> results);
}
