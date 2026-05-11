using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class ScreenShakePlayer : MonoBehaviour
{
    private const float POSITION_EPSILON_SQR = 0.0000001f;
    private const float ROTATION_EPSILON = 0.001f;
    private const float ZOOM_EPSILON = 0.0001f;
    private const float MIN_CAMERA_SIZE = 0.01f;

    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0f)] private float globalStrengthScale = 1f;
    [SerializeField, Min(0f)] private float maxPositionOffset = 2.5f;
    [SerializeField, Min(0f)] private float maxRotationOffset = 8f;
    [SerializeField, Min(0f)] private float maxZoomOffset = 0.45f;

    private readonly List<ActiveShake> activeShakes = new();
    private Vector3 appliedPositionOffset;
    private Quaternion appliedRotationOffset = Quaternion.identity;
    private float appliedZoomOffset;
    private Vector3 lastAppliedWorldPosition;
    private Quaternion lastAppliedWorldRotation = Quaternion.identity;
    private float lastAppliedZoomValue;
    private bool hasAppliedState;
    private int nextSeed = 1;

    public bool IsShaking => activeShakes.Count > 0;
    public int ActiveShakeCount => activeShakes.Count;

    private void OnEnable()
    {
        ResolveCamera();
        GameEventBus.Subscribe<ScreenShakeRequestedEvent>(OnScreenShakeRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<ScreenShakeRequestedEvent>(OnScreenShakeRequested);
        StopAll();
    }

    private void LateUpdate()
    {
        Tick(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void OnValidate()
    {
        globalStrengthScale = Mathf.Max(0f, globalStrengthScale);
        maxPositionOffset = Mathf.Max(0f, maxPositionOffset);
        maxRotationOffset = Mathf.Max(0f, maxRotationOffset);
        maxZoomOffset = Mathf.Max(0f, maxZoomOffset);
    }

    public void Play(ScreenShakeRequest request)
    {
        if (globalStrengthScale <= 0f || !ScreenShakeBridge.CanRequest(request))
        {
            return;
        }

        Transform shakeTransform = ResolveShakeTransform();
        if (shakeTransform == null)
        {
            return;
        }

        activeShakes.Add(ActiveShake.Create(request, shakeTransform, nextSeed++));
    }

    public void StopAll()
    {
        activeShakes.Clear();
        RestoreAppliedOffsets();
        ClearAppliedState();
    }

    private void OnScreenShakeRequested(ScreenShakeRequestedEvent eventData)
    {
        Play(eventData.Request);
    }

    private void Tick(float scaledDeltaTime, float unscaledDeltaTime)
    {
        Transform shakeTransform = ResolveShakeTransform();
        if (shakeTransform == null)
        {
            activeShakes.Clear();
            ClearAppliedState();
            return;
        }

        if (activeShakes.Count == 0)
        {
            if (hasAppliedState)
            {
                ApplyOffsets(shakeTransform, Vector3.zero, 0f, 0f);
                ClearAppliedState();
            }

            return;
        }

        Vector3 positionOffset = Vector3.zero;
        float rotationOffset = 0f;
        float zoomOffset = 0f;

        for (int i = activeShakes.Count - 1; i >= 0; i--)
        {
            ActiveShake shake = activeShakes[i];
            ScreenShakeSettings settings = shake.Settings;
            if (settings == null || !settings.CanPlay)
            {
                activeShakes.RemoveAt(i);
                continue;
            }

            float deltaTime = settings.UseUnscaledTime ? unscaledDeltaTime : scaledDeltaTime;
            shake.Elapsed += Mathf.Max(0f, deltaTime);
            if (shake.Elapsed >= settings.Duration)
            {
                activeShakes.RemoveAt(i);
                continue;
            }

            float normalizedTime = Mathf.Clamp01(shake.Elapsed / settings.Duration);
            float intensity = Mathf.Max(0f, EvaluateFade(settings, normalizedTime)) *
                              Mathf.Max(0f, settings.StrengthScale) *
                              Mathf.Max(0f, shake.StrengthScale) *
                              globalStrengthScale;
            float sampleTime = shake.Elapsed * Mathf.Max(0.01f, settings.Frequency);

            if (settings.PositionStrength > 0f)
            {
                float radialNoise = EvaluateNoise(shake.Seed, sampleTime, 0.13f);
                float lateralNoise = EvaluateNoise(shake.Seed, sampleTime, 11.37f);
                Vector2 planarOffset =
                    (shake.PrimaryDirection * radialNoise + shake.LateralDirection * lateralNoise) *
                    settings.PositionStrength *
                    intensity;
                positionOffset += new Vector3(planarOffset.x, planarOffset.y, 0f);
            }

            if (settings.RotationStrength > 0f)
            {
                rotationOffset += EvaluateNoise(shake.Seed, sampleTime, 29.71f) *
                                  settings.RotationStrength *
                                  intensity;
            }

            if (settings.ZoomStrength > 0f)
            {
                zoomOffset += EvaluateNoise(shake.Seed, sampleTime, 47.19f) *
                              settings.ZoomStrength *
                              intensity;
            }

            activeShakes[i] = shake;
        }

        if (maxPositionOffset > 0f)
        {
            positionOffset = Vector3.ClampMagnitude(positionOffset, maxPositionOffset);
        }
        else
        {
            positionOffset = Vector3.zero;
        }

        rotationOffset = maxRotationOffset > 0f
            ? Mathf.Clamp(rotationOffset, -maxRotationOffset, maxRotationOffset)
            : 0f;
        zoomOffset = maxZoomOffset > 0f
            ? Mathf.Clamp(zoomOffset, -maxZoomOffset, maxZoomOffset)
            : 0f;

        ApplyOffsets(shakeTransform, positionOffset, rotationOffset, zoomOffset);

        if (activeShakes.Count == 0)
        {
            ClearAppliedState();
        }
    }

    private void ApplyOffsets(Transform shakeTransform, Vector3 positionOffset, float rotationOffset, float zoomOffset)
    {
        Vector3 basePosition = ResolveBasePosition(shakeTransform);
        Quaternion baseRotation = ResolveBaseRotation(shakeTransform);
        float baseZoomValue = ResolveBaseZoomValue();

        shakeTransform.position = basePosition + positionOffset;
        Quaternion rotationDelta = Mathf.Abs(rotationOffset) > ROTATION_EPSILON
            ? Quaternion.Euler(0f, 0f, rotationOffset)
            : Quaternion.identity;
        shakeTransform.rotation = baseRotation * rotationDelta;

        Camera camera = ResolveCamera();
        float appliedZoom = 0f;
        if (camera != null && Mathf.Abs(zoomOffset) > ZOOM_EPSILON)
        {
            float finalZoom = Mathf.Max(MIN_CAMERA_SIZE, baseZoomValue + zoomOffset);
            SetCameraZoomValue(camera, finalZoom);
            appliedZoom = finalZoom - baseZoomValue;
        }
        else if (camera != null && Mathf.Abs(appliedZoomOffset) > ZOOM_EPSILON)
        {
            SetCameraZoomValue(camera, Mathf.Max(MIN_CAMERA_SIZE, baseZoomValue));
        }

        appliedPositionOffset = positionOffset;
        appliedRotationOffset = rotationDelta;
        appliedZoomOffset = appliedZoom;
        lastAppliedWorldPosition = shakeTransform.position;
        lastAppliedWorldRotation = shakeTransform.rotation;
        lastAppliedZoomValue = camera != null ? GetCameraZoomValue(camera) : 0f;
        hasAppliedState = true;
    }

    private Vector3 ResolveBasePosition(Transform shakeTransform)
    {
        Vector3 currentPosition = shakeTransform.position;
        if (!hasAppliedState)
        {
            return currentPosition;
        }

        bool externallyMoved = (currentPosition - lastAppliedWorldPosition).sqrMagnitude > POSITION_EPSILON_SQR;
        return externallyMoved ? currentPosition : currentPosition - appliedPositionOffset;
    }

    private Quaternion ResolveBaseRotation(Transform shakeTransform)
    {
        Quaternion currentRotation = shakeTransform.rotation;
        if (!hasAppliedState)
        {
            return currentRotation;
        }

        bool externallyRotated = Quaternion.Angle(currentRotation, lastAppliedWorldRotation) > ROTATION_EPSILON;
        return externallyRotated ? currentRotation : currentRotation * Quaternion.Inverse(appliedRotationOffset);
    }

    private float ResolveBaseZoomValue()
    {
        Camera camera = ResolveCamera();
        if (camera == null)
        {
            return 0f;
        }

        float currentZoomValue = GetCameraZoomValue(camera);
        if (!hasAppliedState)
        {
            return currentZoomValue;
        }

        bool externallyZoomed = Mathf.Abs(currentZoomValue - lastAppliedZoomValue) > ZOOM_EPSILON;
        return externallyZoomed ? currentZoomValue : currentZoomValue - appliedZoomOffset;
    }

    private void RestoreAppliedOffsets()
    {
        if (!hasAppliedState)
        {
            return;
        }

        Transform shakeTransform = ResolveShakeTransform();
        if (shakeTransform != null)
        {
            shakeTransform.position = ResolveBasePosition(shakeTransform);
            shakeTransform.rotation = ResolveBaseRotation(shakeTransform);
        }

        Camera camera = ResolveCamera();
        if (camera != null && Mathf.Abs(appliedZoomOffset) > ZOOM_EPSILON)
        {
            SetCameraZoomValue(camera, Mathf.Max(MIN_CAMERA_SIZE, ResolveBaseZoomValue()));
        }
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = GetComponentInChildren<Camera>();
        }

        return targetCamera;
    }

    private Transform ResolveShakeTransform()
    {
        Camera camera = ResolveCamera();
        return camera != null ? camera.transform : transform;
    }

    private static float GetCameraZoomValue(Camera camera)
    {
        return camera.orthographic ? camera.orthographicSize : camera.fieldOfView;
    }

    private static void SetCameraZoomValue(Camera camera, float value)
    {
        if (camera.orthographic)
        {
            camera.orthographicSize = value;
            return;
        }

        camera.fieldOfView = value;
    }

    private static float EvaluateFade(ScreenShakeSettings settings, float normalizedTime)
    {
        AnimationCurve curve = settings.FadeCurve;
        return curve != null && curve.length > 0
            ? curve.Evaluate(normalizedTime)
            : 1f - normalizedTime;
    }

    private static float EvaluateNoise(int seed, float sampleTime, float offset)
    {
        float seedOffset = seed * 0.173f + offset;
        return Mathf.PerlinNoise(seedOffset, sampleTime) * 2f - 1f;
    }

    private static Vector2 ResolvePrimaryDirection(int seed)
    {
        float angle = Mathf.Repeat(seed * 57.29578f * 0.618034f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private void ClearAppliedState()
    {
        appliedPositionOffset = Vector3.zero;
        appliedRotationOffset = Quaternion.identity;
        appliedZoomOffset = 0f;
        lastAppliedWorldPosition = Vector3.zero;
        lastAppliedWorldRotation = Quaternion.identity;
        lastAppliedZoomValue = 0f;
        hasAppliedState = false;
    }

    private struct ActiveShake
    {
        public ScreenShakeSettings Settings;
        public float StrengthScale;
        public float Elapsed;
        public int Seed;
        public Vector2 PrimaryDirection;
        public Vector2 LateralDirection;

        public static ActiveShake Create(ScreenShakeRequest request, Transform shakeTransform, int seed)
        {
            Vector2 primaryDirection = ResolvePrimaryDirection(seed);
            if (request.HasSourcePosition)
            {
                Vector2 sourceDirection = (Vector2)shakeTransform.position - request.SourcePosition;
                if (sourceDirection.sqrMagnitude > 0.0001f)
                {
                    primaryDirection = sourceDirection.normalized;
                }
            }

            return new ActiveShake
            {
                Settings = request.Settings,
                StrengthScale = request.StrengthScale,
                Elapsed = 0f,
                Seed = seed,
                PrimaryDirection = primaryDirection,
                LateralDirection = new Vector2(-primaryDirection.y, primaryDirection.x)
            };
        }
    }
}
