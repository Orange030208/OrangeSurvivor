using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amountText;

    private void OnEnable()
    {
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChange);
        //立即拉取一次
        GameEventBus.Publish<FetchCurrencyEvent>(new FetchCurrencyEvent());
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChange);
    }
    
    private void OnCurrencyChange(CurrencyChangedEvent e)
    {
        amountText.text = e.CurrentCurrency.ToString();
    }
}
