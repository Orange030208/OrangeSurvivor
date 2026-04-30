using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradeCardHexTechParticleGraphic : MaskableGraphic
{
    public enum ParticleMode
    {
        Orbit,
        Burst
    }

    private const int MAX_PARTICLES = 96;
    private const float TWO_PI = Mathf.PI * 2f;

    [SerializeField] private Sprite sprite;
    [SerializeField] private ParticleMode mode;

    private readonly List<ParticleData> particles = new(MAX_PARTICLES);
    private Color primaryColor = Color.white;
    private Color accentColor = Color.white;
    private bool loopPlaying;
    private float emitAccumulator;

    public override Texture mainTexture => sprite != null && sprite.texture != null
        ? sprite.texture
        : s_WhiteTexture;

    public void Configure(ParticleMode particleMode, Sprite particleSprite)
    {
        mode = particleMode;
        sprite = particleSprite;
        particles.Clear();
        emitAccumulator = 0f;
        SetVerticesDirty();
    }

    public void SetColors(Color primary, Color accent)
    {
        primaryColor = primary;
        accentColor = accent;
        SetVerticesDirty();
    }

    public void PlayLoop()
    {
        if (mode != ParticleMode.Orbit)
        {
            return;
        }

        loopPlaying = true;
        enabled = true;
    }

    public void PlayBurst()
    {
        loopPlaying = false;
        enabled = true;
        particles.Clear();

        int count = UnityEngine.Random.Range(42, 55);
        for (int i = 0; i < count; i++)
        {
            particles.Add(CreateBurstParticle());
        }

        SetVerticesDirty();
    }

    public void Stop()
    {
        loopPlaying = false;
        particles.Clear();
        SetVerticesDirty();
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        if (mode == ParticleMode.Orbit && loopPlaying)
        {
            emitAccumulator += deltaTime * 14f;
            while (emitAccumulator >= 1f && particles.Count < MAX_PARTICLES)
            {
                emitAccumulator -= 1f;
                particles.Add(CreateOrbitParticle());
            }
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            ParticleData particle = particles[i];
            particle.Age += deltaTime;
            if (particle.Age >= particle.Lifetime)
            {
                particles.RemoveAt(i);
                continue;
            }

            float normalizedAge = particle.Age / particle.Lifetime;
            particle.Angle += particle.AngularSpeed * deltaTime;
            particle.Radius += particle.RadialSpeed * deltaTime;
            particle.Position += particle.Velocity * deltaTime;
            particle.Size = EvaluateSize(particle.StartSize, normalizedAge);
            particles[i] = particle;
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = rectTransform.rect;
        for (int i = 0; i < particles.Count; i++)
        {
            ParticleData particle = particles[i];
            float normalizedAge = Mathf.Clamp01(particle.Age / particle.Lifetime);
            Color32 particleColor = EvaluateColor(normalizedAge);
            Vector2 center = ResolvePosition(rect, particle);
            AddQuad(vertexHelper, center, particle.Size, particle.Rotation, particleColor);
        }
    }

    private ParticleData CreateOrbitParticle()
    {
        float minSide = ResolveMinSide();
        float angle = UnityEngine.Random.Range(0f, TWO_PI);
        return new ParticleData
        {
            Lifetime = UnityEngine.Random.Range(2.2f, 3.6f),
            Age = 0f,
            Angle = angle,
            Radius = minSide * UnityEngine.Random.Range(0.44f, 0.5f),
            RadialSpeed = UnityEngine.Random.Range(-2f, 2f),
            AngularSpeed = UnityEngine.Random.Range(0.18f, 0.42f),
            Velocity = Vector2.zero,
            StartSize = UnityEngine.Random.Range(5f, 12f),
            Size = 0f,
            Rotation = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI)
        };
    }

    private ParticleData CreateBurstParticle()
    {
        float angle = UnityEngine.Random.Range(0f, TWO_PI);
        float speed = UnityEngine.Random.Range(90f, 210f);
        return new ParticleData
        {
            Lifetime = UnityEngine.Random.Range(0.34f, 0.78f),
            Age = 0f,
            Angle = angle,
            Radius = ResolveMinSide() * UnityEngine.Random.Range(0f, 0.04f),
            RadialSpeed = 0f,
            AngularSpeed = 0f,
            Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
            StartSize = UnityEngine.Random.Range(10f, 28f),
            Size = 0f,
            Rotation = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI)
        };
    }

    private float ResolveMinSide()
    {
        Rect rect = rectTransform.rect;
        float minSide = Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
        return minSide > 0f ? minSide : 240f;
    }

    private static float EvaluateSize(float startSize, float normalizedAge)
    {
        if (normalizedAge < 0.2f)
        {
            return Mathf.Lerp(0f, startSize, normalizedAge / 0.2f);
        }

        if (normalizedAge < 0.72f)
        {
            return Mathf.Lerp(startSize, startSize * 0.82f, (normalizedAge - 0.2f) / 0.52f);
        }

        return Mathf.Lerp(startSize * 0.82f, 0f, (normalizedAge - 0.72f) / 0.28f);
    }

    private Color32 EvaluateColor(float normalizedAge)
    {
        Color color = Color.Lerp(primaryColor, accentColor, Mathf.SmoothStep(0f, 1f, normalizedAge));
        float alpha;
        if (normalizedAge < 0.16f)
        {
            alpha = Mathf.Lerp(0f, 1f, normalizedAge / 0.16f);
        }
        else if (normalizedAge < 0.68f)
        {
            alpha = mode == ParticleMode.Orbit ? 0.45f : 0.72f;
        }
        else
        {
            alpha = Mathf.Lerp(mode == ParticleMode.Orbit ? 0.45f : 0.72f, 0f, (normalizedAge - 0.68f) / 0.32f);
        }

        color.a *= alpha;
        return color;
    }

    private static Vector2 ResolvePosition(Rect rect, ParticleData particle)
    {
        Vector2 center = rect.center;
        Vector2 radial = new(Mathf.Cos(particle.Angle), Mathf.Sin(particle.Angle));
        return center + radial * particle.Radius + particle.Position;
    }

    private static void AddQuad(VertexHelper vertexHelper, Vector2 center, float size, float rotation, Color32 color)
    {
        if (size <= 0.01f)
        {
            return;
        }

        int index = vertexHelper.currentVertCount;
        float halfSize = size * 0.5f;
        float cos = Mathf.Cos(rotation);
        float sin = Mathf.Sin(rotation);

        Vector2 right = new(cos, sin);
        Vector2 up = new(-sin, cos);
        Vector2 a = center - right * halfSize - up * halfSize;
        Vector2 b = center - right * halfSize + up * halfSize;
        Vector2 c = center + right * halfSize + up * halfSize;
        Vector2 d = center + right * halfSize - up * halfSize;

        vertexHelper.AddVert(a, color, new Vector2(0f, 0f));
        vertexHelper.AddVert(b, color, new Vector2(0f, 1f));
        vertexHelper.AddVert(c, color, new Vector2(1f, 1f));
        vertexHelper.AddVert(d, color, new Vector2(1f, 0f));
        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index + 2, index + 3, index);
    }

    private struct ParticleData
    {
        public float Lifetime;
        public float Age;
        public float Angle;
        public float Radius;
        public float RadialSpeed;
        public float AngularSpeed;
        public Vector2 Position;
        public Vector2 Velocity;
        public float StartSize;
        public float Size;
        public float Rotation;
    }
}
