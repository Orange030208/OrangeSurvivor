using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Template Catalog", menuName = "SO/Enemies/Enemy Template Catalog", order = 1)]
public class EnemyTemplateCatalogSO : ScriptableObject
{
    [Header("Templates")]
    [SerializeField] private Enemy meleeTemplate;
    [SerializeField] private Enemy rangeTemplate;

    public Enemy GetTemplate(EnemyTemplateKind templateKind)
    {
        return templateKind switch
        {
            EnemyTemplateKind.Melee => meleeTemplate,
            EnemyTemplateKind.Range => rangeTemplate,
            _ => null
        };
    }
}
