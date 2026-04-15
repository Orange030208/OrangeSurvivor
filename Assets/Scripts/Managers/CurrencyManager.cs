using System.Collections.Generic;
using UnityEngine;

public enum CurrencyType
{
    Currency,
    Dollar,
}

public class CurrencyManager : MonoBehaviour
{
    private readonly Dictionary<CurrencyType, int> currency = new();

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestCurrencySnapshotEvent>(OnRequestCurrencySnapshot);
        GameEventBus.Subscribe<CurrencyChangeRequestedEvent>(OnCurrencyChangeRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestCurrencySnapshotEvent>(OnRequestCurrencySnapshot);
        GameEventBus.Unsubscribe<CurrencyChangeRequestedEvent>(OnCurrencyChangeRequested);
    }

    private void OnRequestCurrencySnapshot(RequestCurrencySnapshotEvent eventData)
    {
        PublishCurrencyChanged(eventData.CurrencyType, 0);
    }

    private void OnCurrencyChangeRequested(CurrencyChangeRequestedEvent eventData)
    {
        ChangeCurrency(eventData.CurrencyType, eventData.Amount);
    }

    private void ChangeCurrency(CurrencyType currencyType, int amount)
    {
        currency.TryGetValue(currencyType, out int currentAmount);
        currentAmount += amount;
        currency[currencyType] = currentAmount;
        PublishCurrencyChanged(currencyType, amount);
    }

    private void PublishCurrencyChanged(CurrencyType currencyType, int changeAmount)
    {
        currency.TryGetValue(currencyType, out int currentAmount);
        GameEventBus.Publish(new CurrencyChangedEvent(currencyType, currentAmount, changeAmount));
    }
}
