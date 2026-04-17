using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffIconItem : MonoBehaviour, ITooltipDataSource
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private Color positiveColor = new(0.4f, 0.8f, 0.45f, 1f);
    [SerializeField] private Color neutralColor = new(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color negativeColor = new(0.85f, 0.35f, 0.35f, 1f);

    private ActiveBuffSnapshot snapshot;

    public void Configure(ActiveBuffSnapshot snapshot)
    {
        this.snapshot = snapshot;

        if (iconImage != null)
        {
            iconImage.sprite = snapshot.Icon;
            iconImage.enabled = snapshot.Icon != null;
        }

        if (borderImage != null)
        {
            borderImage.color = ResolveBorderColor(snapshot.Polarity);
        }

        if (stackText != null)
        {
            bool showStack = snapshot.StackCount > 1;
            stackText.gameObject.SetActive(showStack);
            stackText.text = snapshot.StackCount.ToString();
        }

        if (durationText != null)
        {
            durationText.gameObject.SetActive(snapshot.HasDuration);
            durationText.text = snapshot.HasDuration ? snapshot.RemainingDurationSeconds.ToString("0.0") : string.Empty;
        }
    }

    public TooltipDisplayData BuildTooltipData()
    {
        return TooltipDataFactory.CreateFromBuff(snapshot);
    }

    private Color ResolveBorderColor(BuffPolarity polarity)
    {
        return polarity switch
        {
            BuffPolarity.Positive => positiveColor,
            BuffPolarity.Negative => negativeColor,
            _ => neutralColor
        };
    }
}
