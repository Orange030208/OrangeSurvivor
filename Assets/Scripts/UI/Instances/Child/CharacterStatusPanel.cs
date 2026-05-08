using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatusPanel : ViewPartBase
{
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI upgradePointText;

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
        RefreshCharacterIdentity(player);

        if (player == null)
        {
            SetHealth(0f, 0f);
            return;
        }

        playerHealthComponent = player.GetComponent<HealthComponent>();
        if (playerHealthComponent == null)
        {
            throw new MissingComponentException($"{nameof(CharacterStatusPanel)} requires {nameof(HealthComponent)} on player '{player.name}'.");
        }

        playerHealthComponent.OnHealthChanged += OnPlayerHealthChanged;
        SetHealth(playerHealthComponent.CurrentHealth, playerHealthComponent.MaxHealth);
    }

    public override void Unbind()
    {
        UnbindPlayerHealth();
        RefreshCharacterIdentity(null);
        SetHealth(0f, 0f);
        SetLevel(0);
        SetXp(0, 0);
        SetUpgradePoint(0);
    }

    public void RefreshCharacterIdentity(Player player)
    {
        CharacterDataSO characterData = player != null ? player.CharacterData : null;

        characterNameText.text = characterData != null && !string.IsNullOrWhiteSpace(characterData.CharacterName)
            ? characterData.CharacterName
            : "-";

        characterIconImage.sprite = characterData != null ? characterData.CharacterIcon : null;
        characterIconImage.enabled = characterIconImage.sprite != null;
    }

    public void SetHealth(float currentHealth, float maxHealth)
    {
        healthFillImage.fillAmount = maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        healthText.text = $"{(int)currentHealth} / {(int)maxHealth}";
    }

    public void SetLevel(int level)
    {
        levelText.text = "lvl" + level;
    }

    public void SetXp(int currentXp, int requiredXp)
    {
        xpFillImage.fillAmount = requiredXp <= 0 ? 0f : (float)currentXp / requiredXp;
    }

    public void SetUpgradePoint(int unspentUpgradePoints)
    {
        upgradePointText.text = unspentUpgradePoints > 0
            ? $"UP {unspentUpgradePoints}"
            : string.Empty;
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
        if (characterIconImage == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing character icon image.");
        }

        if (characterNameText == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing character name text.");
        }

        if (healthFillImage == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing health fill image.");
        }

        if (healthText == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing health text.");
        }

        if (xpFillImage == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing xp fill image.");
        }

        if (levelText == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing level text.");
        }

        if (upgradePointText == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterStatusPanel)} '{name}' is missing upgrade point text.");
        }
    }
}
