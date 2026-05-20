using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthPanel : ViewPartBase
{
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI currentHealthText;
    [SerializeField] private TextMeshProUGUI maxHealthText;

    private HealthComponent playerHealthComponent;

    private void Awake()
    {
        ValidateConfiguration();
    }

    private void OnDisable()
    {
        UnbindPlayerHealth();
    }

    public void BindPlayer(Player player)
    {
        UnbindPlayerHealth();

        if (player == null)
        {
            SetHealth(0f, 0f);
            return;
        }

        playerHealthComponent = player.GetComponent<HealthComponent>();
        if (playerHealthComponent == null)
        {
            throw new MissingComponentException($"{nameof(PlayerHealthPanel)} requires {nameof(HealthComponent)} on player '{player.name}'.");
        }

        playerHealthComponent.OnHealthChanged += OnPlayerHealthChanged;
        SetHealth(playerHealthComponent.CurrentHealth, playerHealthComponent.MaxHealth);
    }

    public override void Unbind()
    {
        UnbindPlayerHealth();
        SetHealth(0f, 0f);
    }

    public void SetHealth(float currentHealth, float maxHealth)
    {
        float safeMaxHealth = Mathf.Max(0f, maxHealth);
        float safeCurrentHealth = Mathf.Clamp(currentHealth, 0f, safeMaxHealth);

        healthFillImage.fillAmount = safeMaxHealth <= 0f ? 0f : safeCurrentHealth / safeMaxHealth;
        currentHealthText.text = Mathf.RoundToInt(safeCurrentHealth).ToString();
        maxHealthText.text = Mathf.RoundToInt(safeMaxHealth).ToString();
    }

    private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        SetHealth(currentHealth, maxHealth);
    }

    private void UnbindPlayerHealth()
    {
        if (playerHealthComponent == null)
        {
            return;
        }

        playerHealthComponent.OnHealthChanged -= OnPlayerHealthChanged;
        playerHealthComponent = null;
    }

    private void ValidateConfiguration()
    {
        if (healthFillImage == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerHealthPanel)} '{name}' is missing health fill image.");
        }

        if (currentHealthText == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerHealthPanel)} '{name}' is missing current health text.");
        }

        if (maxHealthText == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerHealthPanel)} '{name}' is missing max health text.");
        }
    }
}
