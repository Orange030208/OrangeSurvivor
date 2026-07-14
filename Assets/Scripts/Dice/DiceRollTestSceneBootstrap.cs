using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

/// <summary>
/// 测试场景启动后只打开框架注册的测试页，不自行创建 Canvas 或绕过 UIManager。
/// </summary>
public sealed class DiceRollTestSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        OpenTestPageAsync().Forget();
    }

    private async UniTaskVoid OpenTestPageAsync()
    {
        await UniTask.Yield();
        await UIManager.Instance.OpenPageAsync<DiceRollTestPage>(
            cancellationToken: this.GetCancellationTokenOnDestroy());
    }
}
