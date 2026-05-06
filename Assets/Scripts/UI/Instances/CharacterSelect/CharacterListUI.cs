using System.Collections.Generic;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class CharacterListUI : ViewPartBase
{
    [SerializeField] private CharacterButton itemPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;

    private readonly List<CharacterButton> activeItems = new();
    private int selectedIndex = -1;
    private System.Action<int> characterSelected;

    private void Awake()
    {
        ResolveReferences();
        ClearExistingItems();
    }

    public void Render(CharacterDataSO[] characters, int currentSelectedIndex, System.Action<int> onCharacterSelected)
    {
        ResolveReferences();

        selectedIndex = currentSelectedIndex;
        characterSelected = onCharacterSelected;

        int targetCount = characters?.Length ?? 0;
        EnsureItemCount(targetCount);

        for (int i = 0; i < activeItems.Count; i++)
        {
            CharacterButton button = activeItems[i];
            bool shouldShow = i < targetCount;
            button.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            int itemIndex = i;
            CharacterDataSO characterData = characters[i];
            Sprite characterIcon = characterData != null ? characterData.CharacterIcon : null;
            button.Configure(characterIcon, () => characterSelected?.Invoke(itemIndex));
            button.SetSelected(i == selectedIndex);
        }

        ResetScrollPosition();
    }

    public void SetSelectedIndex(int newSelectedIndex)
    {
        if (selectedIndex == newSelectedIndex)
        {
            return;
        }

        selectedIndex = newSelectedIndex;

        for (int i = 0; i < activeItems.Count; i++)
        {
            CharacterButton button = activeItems[i];
            if (button == null || !button.gameObject.activeSelf)
            {
                continue;
            }

            button.SetSelected(i == selectedIndex);
        }
    }

    public void Clear()
    {
        selectedIndex = -1;
        characterSelected = null;

        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] != null)
            {
                Destroy(activeItems[i].gameObject);
            }
        }

        activeItems.Clear();
        ResetScrollPosition();
    }

    private void EnsureItemCount(int targetCount)
    {
        RemoveDestroyedItems();

        while (activeItems.Count < targetCount)
        {
            CharacterButton item = Instantiate(itemPrefab, contentRoot);
            activeItems.Add(item);
        }
    }

    private void ClearExistingItems()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        activeItems.Clear();
    }

    private void RemoveDestroyedItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] == null)
            {
                activeItems.RemoveAt(i);
            }
        }
    }

    private void ResetScrollPosition()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        if (scrollRect.horizontal)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        if (scrollRect.vertical)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ResolveReferences()
    {
        if (contentRoot == null)
        {
            contentRoot = transform;
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterListUI)} '{name}' is missing item prefab.");
        }

        if (contentRoot == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterListUI)} '{name}' is missing content root.");
        }
    }
}
