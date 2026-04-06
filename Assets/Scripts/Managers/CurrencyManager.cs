using UnityEngine;

public class CurrencyManager : MonoSingletonBase<CurrencyManager>
{
    [field: SerializeField] public int Currency{get;private set;}
    
    public void AddCurrency(int amount)
    {
       Currency += amount;
    }
}