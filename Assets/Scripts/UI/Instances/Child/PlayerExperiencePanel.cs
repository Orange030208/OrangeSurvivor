using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerExperiencePanel : ViewPartBase
{
    [SerializeField] private Image xpFillImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI currentXpText;
    [SerializeField] private TextMeshProUGUI maxXpText;

    private void Awake()
    {
        ValidateConfiguration();
    }

    public override void Unbind()
    {
        SetLevel(0);
        SetExperience(0, 0);
    }

    public void SetLevel(int level)
    {
        levelText.text = "lvl" + Mathf.Max(0, level);
    }

    public void SetExperience(int currentXp, int maxXp)
    {
        int safeMaxXp = Mathf.Max(0, maxXp);
        int safeCurrentXp = Mathf.Clamp(currentXp, 0, safeMaxXp);

        xpFillImage.fillAmount = safeMaxXp <= 0 ? 0f : (float)safeCurrentXp / safeMaxXp;
        currentXpText.text = safeCurrentXp.ToString();
        maxXpText.text = safeMaxXp.ToString();
    }

    private void ValidateConfiguration()
    {
        if (xpFillImage == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerExperiencePanel)} '{name}' is missing xp fill image.");
        }

        if (levelText == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerExperiencePanel)} '{name}' is missing level text.");
        }

        if (currentXpText == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerExperiencePanel)} '{name}' is missing current xp text.");
        }

        if (maxXpText == null)
        {
            throw new MissingReferenceException($"{nameof(PlayerExperiencePanel)} '{name}' is missing max xp text.");
        }
    }
}
