using NUnit.Framework;
using UnityEditor;
using System.Reflection;

public sealed class FlyForestConfigurationTests
{
    private const string FlyForestEnemyPath = "Assets/GameContent/Enemies/Data/FlyForest/FlyForestEnemy.asset";

    [Test]
    public void FlyForestEnemyAssetOnlyKeepsNormalAttackAndMovementConfiguration()
    {
        FlyForestEnemySO enemy = AssetDatabase.LoadAssetAtPath<FlyForestEnemySO>(FlyForestEnemyPath);
        Assert.NotNull(enemy, $"Missing required test asset at {FlyForestEnemyPath}.");

        SerializedObject serializedObject = new(enemy);

        Assert.IsNull(serializedObject.FindProperty("lowHpPercent"));
        Assert.IsNull(serializedObject.FindProperty("fastBurstModifierData"));
        Assert.IsNull(serializedObject.FindProperty("retreatMovement"));

        Assert.NotNull(serializedObject.FindProperty("normalAttackAction"));
        Assert.NotNull(serializedObject.FindProperty("normalAttackSpeedBenefitRatio"));
        Assert.NotNull(serializedObject.FindProperty("normalMovement"));
    }

    [Test]
    public void FlyForestBrainDoesNotKeepStateMachineArchitecture()
    {
        Assert.IsNull(typeof(FlyForestBrain).GetField("stateMachine", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsNull(typeof(FlyForestBrain).GetNestedType("FlyForestAIState", BindingFlags.Public | BindingFlags.NonPublic));
    }
}
