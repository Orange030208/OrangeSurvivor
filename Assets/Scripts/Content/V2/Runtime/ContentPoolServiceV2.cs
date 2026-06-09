using System;
using System.Collections.Generic;
using UnityEngine;

public interface IContentPoolService
{
    ContentRollOutcome Roll(ContentRollRequest request);
}

public sealed class ContentPoolServiceV2 : IContentPoolService
{
    private readonly IContentRandom defaultRandom;
    private readonly IContentModifierSource modifierSource;
    private readonly List<ContentRollCandidate> candidateBuffer = new();
    private readonly List<ContentRollSelection> resultBuffer = new();
    private readonly List<ContentPoolEntryDefinition> selectedEntries = new();
    private readonly HashSet<string> selectedEntryIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> selectedRollCounts = new(StringComparer.Ordinal);

    public ContentPoolServiceV2(
        IContentRandom defaultRandom = null,
        IContentModifierSource modifierSource = null)
    {
        this.defaultRandom = defaultRandom ?? new UnityContentRandom();
        this.modifierSource = modifierSource ?? LegacyGlobalContentModifierSource.Instance;
    }

    public ContentRollOutcome Roll(ContentRollRequest request)
    {
        if (request == null)
        {
            resultBuffer.Clear();
            return new ContentRollOutcome(resultBuffer);
        }

        candidateBuffer.Clear();
        resultBuffer.Clear();
        selectedEntries.Clear();
        selectedEntryIds.Clear();
        selectedRollCounts.Clear();

        BuildCandidates(request);
        modifierSource?.ModifyCandidates(request, candidateBuffer);

        IContentRandom random = request.Random ?? defaultRandom;
        for (int i = 0; i < request.RollCount; i++)
        {
            ContentRollCandidate selected = PickCandidate(request.AllowDuplicateResults, random);
            if (selected == null)
            {
                break;
            }

            resultBuffer.Add(selected.ToSelection());
            selectedEntries.Add(selected.Entry);
            AddSelectedRollCount(selected.Entry.EntryId);
            if (!request.AllowDuplicateResults)
            {
                selectedEntryIds.Add(selected.Entry.EntryId);
                selected.Remove();
            }

            RefreshCandidateAvailability(request);
        }

        ContentRollOutcome outcome = new(resultBuffer);
        request.History?.RecordRoll(request.Scope, outcome);
        return outcome;
    }

    private void BuildCandidates(ContentRollRequest request)
    {
        IReadOnlyList<ContentPoolEntryDefinition> entries = request.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            ContentPoolEntryDefinition entry = entries[i];
            if (entry == null || entry.Content == null || !CanUseEntry(request, entry))
            {
                continue;
            }

            if (request.EntryFilter != null && !request.EntryFilter(entry))
            {
                continue;
            }

            if (entry.BaseWeight <= 0f)
            {
                continue;
            }

            candidateBuffer.Add(new ContentRollCandidate(entry, entry.BaseWeight));
        }
    }

    private bool CanUseEntry(ContentRollRequest request, ContentPoolEntryDefinition entry)
    {
        int currentRollCount = GetSelectedRollCount(entry.EntryId);
        RunContentHistory history = request.History;
        if (history != null)
        {
            if (entry.MaxRollCount > 0 &&
                history.GetRollCount(request.Scope, entry.EntryId) + currentRollCount >= entry.MaxRollCount)
            {
                return false;
            }

            if (entry.MaxPickCount > 0 && history.GetPickCount(request.Scope, entry.EntryId) >= entry.MaxPickCount)
            {
                return false;
            }
        }
        else if (entry.MaxRollCount > 0 && currentRollCount >= entry.MaxRollCount)
        {
            return false;
        }

        return true;
    }

    private void AddSelectedRollCount(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return;
        }

        selectedRollCounts.TryGetValue(entryId, out int count);
        selectedRollCounts[entryId] = count + 1;
    }

    private int GetSelectedRollCount(string entryId)
    {
        return !string.IsNullOrWhiteSpace(entryId) &&
               selectedRollCounts.TryGetValue(entryId, out int count)
            ? count
            : 0;
    }

    private void RefreshCandidateAvailability(ContentRollRequest request)
    {
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            ContentRollCandidate candidate = candidateBuffer[i];
            if (candidate == null || candidate.IsRemoved || candidate.Entry == null)
            {
                continue;
            }

            if (!CanUseEntry(request, candidate.Entry) ||
                !SelectedEntriesAllowCandidate(candidate.Entry))
            {
                candidate.Remove();
            }
        }
    }

    private bool SelectedEntriesAllowCandidate(ContentPoolEntryDefinition candidateEntry)
    {
        for (int i = 0; i < selectedEntries.Count; i++)
        {
            ContentPoolEntryDefinition selectedEntry = selectedEntries[i];
            if (EntriesAreMutuallyExclusive(selectedEntry, candidateEntry))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EntriesAreMutuallyExclusive(
        ContentPoolEntryDefinition selectedEntry,
        ContentPoolEntryDefinition candidateEntry)
    {
        if (selectedEntry == null || candidateEntry == null)
        {
            return false;
        }

        return ContainsEntryId(selectedEntry.MutuallyExclusiveEntryIds, candidateEntry.EntryId) ||
               ContainsEntryId(candidateEntry.MutuallyExclusiveEntryIds, selectedEntry.EntryId);
    }

    private static bool ContainsEntryId(IReadOnlyList<string> entryIds, string entryId)
    {
        if (entryIds == null || string.IsNullOrWhiteSpace(entryId))
        {
            return false;
        }

        for (int i = 0; i < entryIds.Count; i++)
        {
            if (string.Equals(entryIds[i], entryId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private ContentRollCandidate PickCandidate(bool allowDuplicateResults, IContentRandom random)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            ContentRollCandidate candidate = candidateBuffer[i];
            if (IsCandidateAvailable(candidate, allowDuplicateResults))
            {
                totalWeight += Mathf.Max(0f, candidate.Weight);
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float cursor = random.Value01() * totalWeight;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            ContentRollCandidate candidate = candidateBuffer[i];
            if (!IsCandidateAvailable(candidate, allowDuplicateResults))
            {
                continue;
            }

            cursor -= Mathf.Max(0f, candidate.Weight);
            if (cursor <= 0f)
            {
                return candidate;
            }
        }

        for (int i = candidateBuffer.Count - 1; i >= 0; i--)
        {
            ContentRollCandidate candidate = candidateBuffer[i];
            if (IsCandidateAvailable(candidate, allowDuplicateResults))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsCandidateAvailable(ContentRollCandidate candidate, bool allowDuplicateResults)
    {
        if (candidate == null || candidate.IsRemoved || candidate.Entry == null || candidate.Content == null)
        {
            return false;
        }

        if (!allowDuplicateResults && selectedEntryIds.Contains(candidate.Entry.EntryId))
        {
            return false;
        }

        return candidate.Weight > 0f;
    }
}

public interface IContentModifierSource
{
    void ModifyCandidates(ContentRollRequest request, List<ContentRollCandidate> candidates);
}

public sealed class NullContentModifierSource : IContentModifierSource
{
    public static NullContentModifierSource Instance { get; } = new();

    private NullContentModifierSource()
    {
    }

    public void ModifyCandidates(ContentRollRequest request, List<ContentRollCandidate> candidates)
    {
    }
}
