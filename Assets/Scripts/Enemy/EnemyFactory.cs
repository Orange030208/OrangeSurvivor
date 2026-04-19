using UnityEngine;

public sealed class EnemyFactory
{
    private readonly EnemyTemplateCatalogSO templateCatalog;

    public EnemyFactory(EnemyTemplateCatalogSO templateCatalog)
    {
        this.templateCatalog = templateCatalog;
    }

    public Enemy Spawn(EnemyRuntimeSetup setup, Vector3 spawnPosition, Transform parent)
    {
        if (setup.Definition == null)
        {
            throw new MissingReferenceException($"{nameof(EnemyFactory)} requires a non-null {nameof(EnemyDefinitionSO)}.");
        }

        if (templateCatalog == null)
        {
            throw new MissingReferenceException($"{nameof(EnemyFactory)} requires a non-null {nameof(EnemyTemplateCatalogSO)}.");
        }

        Enemy template = templateCatalog.GetTemplate(setup.Definition.TemplateKind);
        if (template == null)
        {
            throw new MissingReferenceException($"{nameof(EnemyFactory)} cannot resolve template for {setup.Definition.TemplateKind}.");
        }

        Enemy enemy = Object.Instantiate(template, spawnPosition, Quaternion.identity, parent);
        enemy.Configure(setup);
        return enemy;
    }
}
