using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class PlayerLevelConfigTests
{
    private PlayerLevelConfigSO createdConfig;

    [TearDown]
    public void TearDown()
    {
        if (createdConfig != null)
        {
            Object.DestroyImmediate(createdConfig);
        }
    }

    [Test]
    public void DefaultExperienceRequirementCurveUsesNonLinearMilestones()
    {
        createdConfig = ScriptableObject.CreateInstance<PlayerLevelConfigSO>();

        Assert.AreEqual(8, createdConfig.GetRequiredExperienceForLevel(1));
        Assert.AreEqual(13, createdConfig.GetRequiredExperienceForLevel(2));
        Assert.AreEqual(19, createdConfig.GetRequiredExperienceForLevel(3));
        Assert.AreEqual(26, createdConfig.GetRequiredExperienceForLevel(4));
        Assert.AreEqual(34, createdConfig.GetRequiredExperienceForLevel(5));
        Assert.AreEqual(56, createdConfig.GetRequiredExperienceForLevel(7));
        Assert.AreEqual(89, createdConfig.GetRequiredExperienceForLevel(10));
        Assert.AreEqual(169, createdConfig.GetRequiredExperienceForLevel(15));
        Assert.AreEqual(274, createdConfig.GetRequiredExperienceForLevel(20));
        Assert.AreEqual(295, createdConfig.GetRequiredExperienceForLevel(21));
    }
}
