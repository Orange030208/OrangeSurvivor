using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景级运行时内容入口装配器。
/// 每个正式玩法入口场景应显式挂载一次，并在 Inspector 中绑定 Catalog。
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class GameContentBootstrap : MonoBehaviour
{
    [SerializeField] private GameContentCatalogSO catalog;

    private GameContentCatalogProvider provider;

    public GameContentCatalogSO Catalog => catalog;

    private void Awake()
    {
        // 需要早于各类玩法 Manager 读取默认 prefab、内容池和表现配置。
        TryInitializeRuntime();
    }

    private void OnDestroy()
    {
        // 只清理由当前 Bootstrap 创建的 Provider，避免影响后续加载场景自己的内容入口。
        GameContentRuntime.ClearProvider(provider);
    }

    /// <summary>
    /// 安装 Catalog Provider；只有场景缺少 Catalog 引用时返回 false。
    /// </summary>
    public bool TryInitializeRuntime()
    {
        if (provider != null)
        {
            GameContentRuntime.SetProvider(provider);
            return true;
        }

        if (catalog == null)
        {
            Debug.LogError($"{nameof(GameContentBootstrap)} '{name}' is missing {nameof(GameContentCatalogSO)}.", this);
            return false;
        }

        List<string> errors = new();
        if (!catalog.ValidateCatalog(errors))
        {
            // 先完整输出配置问题，再继续安装 Provider；这样错误会指向 Catalog，而不是后续某个无关系统。
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError(errors[i], catalog);
            }
        }

        provider = new GameContentCatalogProvider(catalog);
        GameContentRuntime.SetProvider(provider);
        return true;
    }
}
