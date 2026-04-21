using UnityEngine;

[DisallowMultipleComponent]
public class EnemyRuntimeBridge : MonoBehaviour
{
    [SerializeField] private ParticleSystem passAwayParticles;
    [SerializeField] [Min(0f)] private float deathPassAwayDelay;

    private Enemy owner;
    private HealthComponent healthComponent;
    private bool runtimeRegistered;
    private bool passAwayRequested;

    public void Initialize(Enemy enemy, HealthComponent runtimeHealthComponent)
    {
        owner = enemy;
        healthComponent = runtimeHealthComponent;
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += PassAway;
        }

        RegisterRuntime();
    }

    private void OnDisable()
    {
        UnregisterRuntime();

        if (healthComponent != null)
        {
            healthComponent.OnDied -= PassAway;
        }
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

    private void RegisterRuntime()
    {
        if (runtimeRegistered || owner == null)
        {
            return;
        }

        GameEventBus.Publish(new EnemyRuntimeRegisteredEvent(owner, owner.Role));
        runtimeRegistered = true;
    }

    private void UnregisterRuntime()
    {
        if (!runtimeRegistered || owner == null)
        {
            return;
        }

        GameEventBus.Publish(new EnemyRuntimeUnregisteredEvent(owner, owner.Role));
        runtimeRegistered = false;
    }
}
