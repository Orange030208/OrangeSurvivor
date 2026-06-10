using System;
using System.Collections.Generic;
using UnityEngine;

public enum InfoTone
{
    Neutral = 0,
    Positive = 1,
    Negative = 2,
    Warning = 3,
    Emphasis = 4,
    Disabled = 5
}

public enum InfoItemType
{
    Title = 0,
    SectionHeader = 1,
    Text = 2,
    TagText = 3,
    Property = 4,
    Image = 5,
    LineBreak = 6,
    Spacer = 7
}

public sealed class InfoDocument
{
    public InfoDocument(
        string id,
        IReadOnlyList<InfoItem> items)
    {
        Id = id ?? string.Empty;
        Items = items ?? Array.Empty<InfoItem>();
    }

    public string Id { get; }
    public IReadOnlyList<InfoItem> Items { get; }
}

public readonly struct InfoItem
{
    private readonly IInfoItemContentDecoder decoder;

    public InfoItem(
        InfoItemType type,
        string content,
        InfoTone tone = InfoTone.Neutral,
        IInfoItemContentDecoder decoder = null)
    {
        Type = type;
        Content = content ?? string.Empty;
        Tone = tone;
        this.decoder = decoder;
    }

    public InfoItemType Type { get; }
    public string Content { get; }
    public InfoTone Tone { get; }
    public IInfoItemContentDecoder Decoder => decoder ?? PlainDecoder.Instance;
}

public interface IInfoItemContentDecoder
{
    string DecodeText(string content);
    bool TryDecode<T>(string content, out T value);
}

public sealed class PlainDecoder : IInfoItemContentDecoder
{
    public static readonly PlainDecoder Instance = new();

    private PlainDecoder()
    {
    }

    public string DecodeText(string content)
    {
        return content ?? string.Empty;
    }

    public bool TryDecode<T>(string content, out T value)
    {
        if (typeof(T) == typeof(string))
        {
            value = (T)(object)(content ?? string.Empty);
            return true;
        }

        value = default;
        return false;
    }
}

public sealed class PropertyDecoder : IInfoItemContentDecoder
{
    public static readonly PropertyDecoder Instance = new();

    private PropertyDecoder()
    {
    }

    public string DecodeText(string content)
    {
        InfoPropertyPresentation presentation = ResolvePresentation(content);
        return string.IsNullOrWhiteSpace(presentation.DisplayName)
            ? string.Empty
            : $"{presentation.DisplayName}: ";
    }

    public bool TryDecode<T>(string content, out T value)
    {
        if (typeof(T) == typeof(InfoPropertyPresentation))
        {
            value = (T)(object)ResolvePresentation(content);
            return true;
        }

        if (typeof(T) == typeof(string))
        {
            value = (T)(object)DecodeText(content);
            return true;
        }

        value = default;
        return false;
    }

    private static InfoPropertyPresentation ResolvePresentation(string propertyId)
    {
        string normalizedId = string.IsNullOrWhiteSpace(propertyId) ? string.Empty : propertyId.Trim();
        if (string.IsNullOrEmpty(normalizedId))
        {
            return new InfoPropertyPresentation(string.Empty, string.Empty, null);
        }

        if (Enum.TryParse(normalizedId, out PropType propType) &&
            GameContentRuntime.TryGetPropPresentationEntry(propType, out PropPresentationEntry propEntry))
        {
            string displayName = string.IsNullOrWhiteSpace(propEntry.ChineseName)
                ? normalizedId
                : propEntry.ChineseName;
            return new InfoPropertyPresentation(normalizedId, displayName, propEntry.Icon);
        }

        if (GameContentRuntime.TryGetPropPresentationEntry(normalizedId, out PropPresentationEntry entry))
        {
            string displayName = string.IsNullOrWhiteSpace(entry.ChineseName)
                ? normalizedId
                : entry.ChineseName;
            return new InfoPropertyPresentation(normalizedId, displayName, entry.Icon);
        }

        return new InfoPropertyPresentation(normalizedId, normalizedId, null);
    }
}

public abstract class ImageDecoder : IInfoItemContentDecoder
{
    private readonly Sprite fallbackSprite;

    protected ImageDecoder(Sprite fallbackSprite = null)
    {
        this.fallbackSprite = fallbackSprite;
    }

    public string DecodeText(string content)
    {
        return string.Empty;
    }

    public bool TryDecode<T>(string content, out T value)
    {
        if (typeof(T) == typeof(Sprite))
        {
            Sprite sprite = Resolve(content);
            if (sprite != null)
            {
                value = (T)(object)sprite;
                return true;
            }
        }

        if (typeof(T) == typeof(string))
        {
            value = (T)(object)(content ?? string.Empty);
            return true;
        }

        value = default;
        return false;
    }

    protected Sprite FallbackSprite => fallbackSprite;

    protected abstract Sprite Resolve(string content);

    protected static string Normalize(string content)
    {
        return string.IsNullOrWhiteSpace(content) ? string.Empty : content.Trim();
    }
}

public sealed class WeaponImage : ImageDecoder
{
    public static readonly WeaponImage Instance = new();

    public WeaponImage(Sprite fallbackSprite = null) : base(fallbackSprite)
    {
    }

    protected override Sprite Resolve(string content)
    {
        string weaponId = Normalize(content);
        if (!string.IsNullOrWhiteSpace(weaponId) &&
            GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            IReadOnlyList<WeaponDataSO> weapons = provider.Weapons;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDataSO weapon = weapons[i];
                if (weapon != null && string.Equals(weapon.WeaponId, weaponId, StringComparison.Ordinal))
                {
                    return weapon.ItemIcon != null ? weapon.ItemIcon : FallbackSprite;
                }
            }
        }

        return FallbackSprite;
    }
}

public sealed class AccessoryImage : ImageDecoder
{
    public static readonly AccessoryImage Instance = new();

    public AccessoryImage(Sprite fallbackSprite = null) : base(fallbackSprite)
    {
    }

    protected override Sprite Resolve(string content)
    {
        string accessoryId = Normalize(content);
        if (!string.IsNullOrWhiteSpace(accessoryId) &&
            GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            IReadOnlyList<AccessoryDataSO> accessories = provider.Accessories;
            for (int i = 0; i < accessories.Count; i++)
            {
                AccessoryDataSO accessory = accessories[i];
                if (accessory != null && string.Equals(accessory.AccessoryId, accessoryId, StringComparison.Ordinal))
                {
                    return accessory.ItemIcon != null ? accessory.ItemIcon : FallbackSprite;
                }
            }
        }

        return FallbackSprite;
    }
}

public sealed class BuffImage : ImageDecoder
{
    public static readonly BuffImage Instance = new();

    public BuffImage(Sprite fallbackSprite = null) : base(fallbackSprite)
    {
    }

    protected override Sprite Resolve(string content)
    {
        string buffId = Normalize(content);
        if (!string.IsNullOrWhiteSpace(buffId) &&
            GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            Sprite sprite = ResolveFromBuffs(buffId, provider.Buffs);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return FallbackSprite;
    }

    private static Sprite ResolveFromBuffs(string buffId, IReadOnlyList<BuffDataSO> buffs)
    {
        if (buffs == null)
        {
            return null;
        }

        for (int i = 0; i < buffs.Count; i++)
        {
            BuffDataSO buffData = buffs[i];
            if (buffData != null &&
                string.Equals(buffData.BuffId, buffId, StringComparison.Ordinal))
            {
                return buffData.Icon;
            }
        }

        return null;
    }
}

public sealed class RewardCardImage : ImageDecoder
{
    public static readonly RewardCardImage Instance = new();

    public RewardCardImage(Sprite fallbackSprite = null) : base(fallbackSprite)
    {
    }

    protected override Sprite Resolve(string content)
    {
        string cardId = Normalize(content);
        if (!string.IsNullOrWhiteSpace(cardId) &&
            GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            Sprite starterSprite = ResolveFromCards(provider.StarterCards, cardId);
            if (starterSprite != null)
            {
                return starterSprite;
            }

            Sprite rewardSprite = ResolveFromCards(provider.RewardCards, cardId);
            if (rewardSprite != null)
            {
                return rewardSprite;
            }
        }

        return FallbackSprite;
    }

    private static Sprite ResolveFromCards(IReadOnlyList<RewardCardSO> rewardCards, string cardId)
    {
        if (rewardCards == null)
        {
            return null;
        }

        for (int i = 0; i < rewardCards.Count; i++)
        {
            RewardCardSO rewardCard = rewardCards[i];
            if (rewardCard != null && string.Equals(rewardCard.Id, cardId, StringComparison.Ordinal))
            {
                return rewardCard.Icon;
            }
        }

        return null;
    }
}

public readonly struct InfoPropertyPresentation
{
    public InfoPropertyPresentation(string id, string displayName, Sprite icon)
    {
        Id = id ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Icon = icon;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public Sprite Icon { get; }
}

public static class InfoDocumentUtility
{
    public static InfoItem CreateTitle(
        string title,
        InfoTone tone = InfoTone.Emphasis,
        IInfoItemContentDecoder decoder = null)
    {
        return new InfoItem(InfoItemType.Title, title, tone, decoder);
    }

    public static InfoItem CreateSectionHeader(
        string title,
        InfoTone tone = InfoTone.Emphasis,
        IInfoItemContentDecoder decoder = null)
    {
        return new InfoItem(InfoItemType.SectionHeader, title, tone, decoder);
    }

    public static InfoItem CreateText(
        string text,
        InfoTone tone = InfoTone.Neutral,
        IInfoItemContentDecoder decoder = null)
    {
        return new InfoItem(InfoItemType.Text, text, tone, decoder);
    }

    public static InfoItem CreateTagText(
        string text,
        InfoTone tone = InfoTone.Disabled,
        IInfoItemContentDecoder decoder = null)
    {
        return new InfoItem(InfoItemType.TagText, text, tone, decoder);
    }

    public static InfoItem CreateLineBreak()
    {
        return new InfoItem(InfoItemType.LineBreak, string.Empty);
    }

    public static InfoItem CreateSpacer()
    {
        return new InfoItem(InfoItemType.Spacer, string.Empty);
    }

    public static InfoItem CreateImage(
        string content,
        ImageDecoder decoder,
        InfoTone tone = InfoTone.Neutral)
    {
        return new InfoItem(InfoItemType.Image, content, tone, decoder);
    }

    public static InfoItem CreateImage(string content, InfoTone tone = InfoTone.Neutral)
    {
        return new InfoItem(InfoItemType.Image, content, tone);
    }

    public static InfoItem CreateProperty(
        string propertyId,
        InfoTone tone = InfoTone.Neutral,
        IInfoItemContentDecoder decoder = null)
    {
        return new InfoItem(
            InfoItemType.Property,
            propertyId,
            tone,
            decoder ?? PropertyDecoder.Instance);
    }

    public static void AppendTextLine(List<InfoItem> items, string text, InfoTone tone = InfoTone.Neutral)
    {
        if (items == null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        items.Add(CreateText(text, tone));
        items.Add(CreateLineBreak());
    }

    public static void AppendPropertyLine(
        List<InfoItem> items,
        string propertyId,
        string valueText,
        InfoTone valueTone = InfoTone.Neutral)
    {
        if (items == null || string.IsNullOrWhiteSpace(propertyId))
        {
            return;
        }

        items.Add(CreateProperty(propertyId));
        if (!string.IsNullOrWhiteSpace(valueText))
        {
            items.Add(CreateText(valueText, valueTone));
        }

        items.Add(CreateLineBreak());
    }
}
