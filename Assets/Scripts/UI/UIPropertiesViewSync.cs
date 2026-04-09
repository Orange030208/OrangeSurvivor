using System.Collections.Generic;
using UnityEngine;

public class UIPropertiesViewSync : MonoBehaviour
{
    [SerializeField] private UIPropertiesViewList propertiesViewList;
    [SerializeField] private PropertiesManager propertiesManager;

    private bool subscribed;

    public void InjectDependencies(PropertiesManager manager, UIPropertiesViewList viewList = null)
    {
        bool shouldResume = subscribed;
        StopSync();

        propertiesManager = manager;
        if (viewList != null)
        {
            propertiesViewList = viewList;
        }

        if (shouldResume)
        {
            StartSync();
        }
    }

    public void StartSync()
    {
        Subscribe();
        Refresh();
    }

    public void StopSync()
    {
        Unsubscribe();
    }

    public void Refresh()
    {
        if (propertiesViewList == null || propertiesManager == null)
        {
            return;
        }

        propertiesViewList.Render(ToPropEntries(propertiesManager.GetAllPropValues()));
    }

    private void OnDisable()
    {
        StopSync();
    }

    private void Subscribe()
    {
        if (subscribed || propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnAllPropertiesChanged += Refresh;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnAllPropertiesChanged -= Refresh;
        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        subscribed = false;
    }

    private void OnPropertyChanged(PropType _, float __)
    {
        Refresh();
    }

    private List<PropEntry> ToPropEntries(Dictionary<PropType, float> props)
    {
        List<PropEntry> entries = new();
        foreach (var kv in props)
        {
            entries.Add(new PropEntry(kv.Key, kv.Value));
        }

        return entries;
    }
}
