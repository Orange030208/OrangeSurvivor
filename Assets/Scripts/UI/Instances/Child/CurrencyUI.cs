using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private CurrencyType currencyType = CurrencyType.Currency;
    [SerializeField] private TextMeshProUGUI amountText;

    private void OnEnable()
    {
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChange);
        GameEventBus.Publish(new RequestCurrencySnapshotEvent(currencyType));
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChange);
    }

    private void OnCurrencyChange(CurrencyChangedEvent eventData)
    {
        if (eventData.CurrencyType != currencyType)
        {
            return;
        }

        amountText.text = eventData.CurrentAmount.ToString();
    }
}
