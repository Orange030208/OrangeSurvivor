using UnityEngine;

[DisallowMultipleComponent]
public class EnemyRuntimeBridge : MonoBehaviour
{
    [SerializeField] private ParticleSystem passAwayParticles;

    private Enemy owner;
    private HealthComponent healthComponent;
    private bool runtimeRegistered;

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
        PassAwayAfterWave();
    }

    public void PassAwayAfterWave()
    {
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
