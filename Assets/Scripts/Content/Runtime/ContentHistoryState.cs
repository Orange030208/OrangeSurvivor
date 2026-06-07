using System.Collections.Generic;
using UnityEngine;

public sealed class ContentHistoryState
{
    private readonly Dictionary<ContentHistoryScope, ContentHistoryBucket> buckets = new();

    public int GetRollCount(ContentHistoryScope scope, string entryId)
    {
        return string.IsNullOrWhiteSpace(entryId) || !buckets.TryGetValue(scope, out ContentHistoryBucket bucket)
            ? 0
            : bucket.GetRollCount(entryId);
    }

    public int GetPickCount(ContentHistoryScope scope, string entryId)
    {
        return string.IsNullOrWhiteSpace(entryId) || !buckets.TryGetValue(scope, out ContentHistoryBucket bucket)
            ? 0
            : bucket.GetPickCount(entryId);
    }

    public int GetContentPickCount(ContentHistoryScope scope, Object content)
    {
        return content == null || !buckets.TryGetValue(scope, out ContentHistoryBucket bucket)
            ? 0
            : bucket.GetContentPickCount(content);
    }

    public int GetUpgradeCardTagPickCount(ContentHistoryScope scope, CardTag requiredTag)
    {
        return !buckets.TryGetValue(scope, out ContentHistoryBucket bucket)
            ? 0
            : bucket.GetUpgradeCardTagPickCount(requiredTag);
    }

    public int GetUpgradeCardTagPickCount(
        ContentHistoryScope scope,
        CardTag requiredTags,
        ContentTagMatchMode matchMode)
    {
        return !buckets.TryGetValue(scope, out ContentHistoryBucket bucket)
            ? 0
            : bucket.GetUpgradeCardTagPickCount(requiredTags, matchMode);
    }

    public bool WasPreviouslyRolled(ContentHistoryScope scope, string entryId)
    {
        return !string.IsNullOrWhiteSpace(entryId) &&
               buckets.TryGetValue(scope, out ContentHistoryBucket bucket) &&
               bucket.WasPreviouslyRolled(entryId);
    }

    public bool WasPreviouslyOffered(ContentHistoryScope scope, string entryId)
    {
        return !string.IsNullOrWhiteSpace(entryId) &&
               buckets.TryGetValue(scope, out ContentHistoryBucket bucket) &&
               bucket.WasPreviouslyOffered(entryId);
    }

    public IReadOnlyList<string> GetPreviousOfferEntryIds(ContentHistoryScope scope)
    {
        return buckets.TryGetValue(scope, out ContentHistoryBucket bucket)
            ? bucket.PreviousOfferEntryIds
            : System.Array.Empty<string>();
    }

    public void RecordRoll(ContentHistoryScope scope, IReadOnlyList<ContentRollItem> items)
    {
        GetOrCreateBucket(scope).RecordRoll(items);
    }

    public void RecordPick(ContentHistoryScope scope, ContentRollItem item)
    {
        GetOrCreateBucket(scope).RecordPick(item);
    }

    public void RecordPick(ContentHistoryScope scope, string entryId)
    {
        GetOrCreateBucket(scope).RecordPick(entryId);
    }

    private ContentHistoryBucket GetOrCreateBucket(ContentHistoryScope scope)
    {
        if (!buckets.TryGetValue(scope, out ContentHistoryBucket bucket))
        {
            bucket = new ContentHistoryBucket();
            buckets.Add(scope, bucket);
        }

        return bucket;
    }

    private sealed class ContentHistoryBucket
    {
        private readonly Dictionary<string, int> rollCountsByEntryId = new(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> pickCountsByEntryId = new(System.StringComparer.Ordinal);
        private readonly Dictionary<Object, int> pickCountsByContent = new();
        private readonly Dictionary<CardTag, int> upgradeCardTagPickCounts = new();
        private readonly Dictionary<CardTag, int> upgradeCardTagMaskPickCounts = new();
        private readonly HashSet<string> previousRollEntryIds = new(System.StringComparer.Ordinal);
        private readonly List<string> previousOfferEntryIds = new();

        public IReadOnlyList<string> PreviousOfferEntryIds => previousOfferEntryIds;

        public int GetRollCount(string entryId)
        {
            return rollCountsByEntryId.GetValueOrDefault(entryId, 0);
        }

        public int GetPickCount(string entryId)
        {
            return pickCountsByEntryId.GetValueOrDefault(entryId, 0);
        }

        public int GetContentPickCount(Object content)
        {
            return pickCountsByContent.GetValueOrDefault(content, 0);
        }

        public int GetUpgradeCardTagPickCount(CardTag requiredTag)
        {
            return upgradeCardTagPickCounts.GetValueOrDefault(requiredTag, 0);
        }

        public int GetUpgradeCardTagPickCount(CardTag requiredTags, ContentTagMatchMode matchMode)
        {
            if (requiredTags != CardTag.None && IsSingleBit(requiredTags) &&
                matchMode is ContentTagMatchMode.Any or ContentTagMatchMode.All)
            {
                return GetUpgradeCardTagPickCount(requiredTags);
            }

            int count = 0;
            foreach (KeyValuePair<CardTag, int> pair in upgradeCardTagMaskPickCounts)
            {
                if (ContentTagMatchUtility.Matches(pair.Key, requiredTags, matchMode))
                {
                    count += pair.Value;
                }
            }

            return count;
        }

        public bool WasPreviouslyRolled(string entryId)
        {
            return previousRollEntryIds.Contains(entryId);
        }

        public bool WasPreviouslyOffered(string entryId)
        {
            return previousOfferEntryIds.Contains(entryId);
        }

        public void RecordRoll(IReadOnlyList<ContentRollItem> items)
        {
            previousRollEntryIds.Clear();
            previousOfferEntryIds.Clear();
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                string entryId = items[i].EntryId;
                if (string.IsNullOrWhiteSpace(entryId))
                {
                    continue;
                }

                previousRollEntryIds.Add(entryId);
                previousOfferEntryIds.Add(entryId);
                rollCountsByEntryId[entryId] = GetRollCount(entryId) + 1;
            }
        }

        public void RecordPick(ContentRollItem item)
        {
            string entryId = item.EntryId;
            if (!string.IsNullOrWhiteSpace(entryId))
            {
                RecordPick(entryId);
            }

            Object content = item.Content;
            if (content == null)
            {
                return;
            }

            pickCountsByContent[content] = GetContentPickCount(content) + 1;
            if (content is RewardCardSO rewardCard)
            {
                if (rewardCard.Tags != CardTag.None)
                {
                    upgradeCardTagMaskPickCounts[rewardCard.Tags] =
                        upgradeCardTagMaskPickCounts.GetValueOrDefault(rewardCard.Tags, 0) + 1;
                }

                CardTag[] tags = rewardCard.TagList;
                for (int i = 0; i < tags.Length; i++)
                {
                    CardTag tag = tags[i];
                    upgradeCardTagPickCounts[tag] = GetUpgradeCardTagPickCount(tag) + 1;
                }
            }
        }

        public void RecordPick(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return;
            }

            pickCountsByEntryId[entryId] = GetPickCount(entryId) + 1;
        }

        private static bool IsSingleBit(CardTag tag)
        {
            int value = (int)tag;
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
