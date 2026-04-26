using UnityEngine;

[DisallowMultipleComponent]
public class EnemyRuntimeBridge : EntityComponentBase
{
    [SerializeField] private ParticleSystem passAwayParticles;
    [SerializeField] [Min(0f)] private float deathPassAwayDelay;

    private HealthComponent healthComponent;
    private bool passAwayRequested;
    private Enemy owner;

    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        this.owner = owner as Enemy;
        healthComponent = this.owner.HealthComponent;
    }

    public override void OnEnableComponent()
    {
        healthComponent.OnDied += PassAway;
    }

    public override void OnDisableComponent()
    {
        healthComponent.OnDied -= PassAway;
    }

    public void PassAway()
    {
        if (passAwayRequested)
        {
            return;
        }

        passAwayRequested = true;

        if (deathPassAwayDelay <= 0f)
        {
            PassAwayAfterWave();
            return;
        }

        CancelInvoke(nameof(PassAwayAfterWave));
        Invoke(nameof(PassAwayAfterWave), deathPassAwayDelay);
    }

    public void PassAwayAfterWave()
    {
        if (this == null)
        {
            return;
        }

        if (passAwayParticles != null)
        {
            passAwayParticles.transform.SetParent(null);
            passAwayParticles.Play();
        }

        Destroy(gameObject);
    }
}