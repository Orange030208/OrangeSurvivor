using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ScreenShakeTests
{
    [SetUp]
    public void SetUp()
    {
        GameEventBus.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        GameEventBus.Clear();
    }

    [Test]
    public void BossMeleeDefaultSettingsArePlayable()
    {
        ScreenShakeSettings settings = ScreenShakeSettings.CreateBossMeleeDefault();

        Assert.IsTrue(settings.CanPlay);
        Assert.Greater(settings.Duration, 0f);
        Assert.Greater(settings.PositionStrength, 0f);
        Assert.Greater(settings.Frequency, 0f);
        Assert.IsNotNull(settings.FadeCurve);
        Assert.Greater(settings.FadeCurve.length, 0);
    }

    [Test]
    public void SettingsValidationCorrectsInvalidValues()
    {
        ScreenShakeSettings settings = new(
            true,
            -1f,
            -2f,
            -3f,
            -4f,
            -5f,
            true,
            null);

        settings.OnValidate();

        Assert.Greater(settings.Duration, 0f);
        Assert.AreEqual(0f, settings.PositionStrength);
        Assert.AreEqual(0f, settings.RotationStrength);
        Assert.AreEqual(0f, settings.ZoomStrength);
        Assert.Greater(settings.Frequency, 0f);
        Assert.IsNotNull(settings.FadeCurve);
        Assert.Greater(settings.FadeCurve.length, 0);
    }

    [Test]
    public void BridgeSkipsInvalidRequests()
    {
        int eventCount = 0;
        GameEventBus.Subscribe<ScreenShakeRequestedEvent>(_ => eventCount++);

        ScreenShakeBridge.Request(new ScreenShakeSettings(false, 0.1f, 1f, 0f, 10f));
        ScreenShakeBridge.Request(new ScreenShakeSettings(true, 0f, 1f, 0f, 10f));
        ScreenShakeBridge.Request(ScreenShakeSettings.CreateBossMeleeDefault(), 0f);

        Assert.AreEqual(0, eventCount);
    }

    [Test]
    public void BridgePublishesValidRequest()
    {
        int eventCount = 0;
        ScreenShakeRequest receivedRequest = default;
        ScreenShakeSettings settings = ScreenShakeSettings.CreateBossMeleeDefault();
        GameEventBus.Subscribe<ScreenShakeRequestedEvent>(eventData =>
        {
            eventCount++;
            receivedRequest = eventData.Request;
        });

        ScreenShakeBridge.Request(settings, 0.5f, new Vector2(2f, 3f));

        Assert.AreEqual(1, eventCount);
        Assert.AreSame(settings, receivedRequest.Settings);
        Assert.AreEqual(0.5f, receivedRequest.StrengthScale);
        Assert.IsTrue(receivedRequest.HasSourcePosition);
        Assert.AreEqual(new Vector2(2f, 3f), receivedRequest.SourcePosition);
    }

    [Test]
    public void PlayerAppliesAndRestoresShakeOffset()
    {
        GameObject cameraObject = new("ScreenShakeTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            ScreenShakePlayer player = cameraObject.AddComponent<ScreenShakePlayer>();
            ScreenShakeSettings settings = ScreenShakeSettings.CreateBossMeleeDefault();

            player.Play(new ScreenShakeRequest(settings, 1f, new Vector2(-4f, 0f)));
            InvokeTick(player, 0.02f, 0.02f);

            Assert.AreNotEqual(Vector3.zero, cameraObject.transform.position);

            player.StopAll();

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(Quaternion.Angle(Quaternion.identity, cameraObject.transform.rotation), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(camera.orthographicSize, Is.EqualTo(8f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static void InvokeTick(ScreenShakePlayer player, float scaledDeltaTime, float unscaledDeltaTime)
    {
        MethodInfo tickMethod = typeof(ScreenShakePlayer).GetMethod(
            "Tick",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(tickMethod);
        tickMethod.Invoke(player, new object[] { scaledDeltaTime, unscaledDeltaTime });
    }
}
