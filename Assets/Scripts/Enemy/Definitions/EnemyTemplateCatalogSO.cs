using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Template Catalog", menuName = "SO/Enemies/Enemy Template Catalog", order = 1)]
public class EnemyTemplateCatalogSO : ScriptableObject
{
    [Header("Templates")]
    [SerializeField] private Enemy meleeTemplate;
    [SerializeField] private Enemy rangeTemplate;
    [SerializeField] private Enemy orbitMageTemplate;
    [SerializeField] private Enemy novaMageTemplate;

    public Enemy GetTemplate(EnemyTemplateKind templateKind)
    {
        return templateKind switch
        {
            EnemyTemplateKind.Melee => meleeTemplate,
            EnemyTemplateKind.Range => rangeTemplate,
            EnemyTemplateKind.OrbitMage => orbitMageTemplate != null ? orbitMageTemplate : rangeTemplate,
            EnemyTemplateKind.NovaMage => novaMageTemplate != null ? novaMageTemplate : rangeTemplate,
            _ => null
        };
    }
}
