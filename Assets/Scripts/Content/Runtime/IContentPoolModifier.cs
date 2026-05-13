using System.Collections.Generic;

public interface IContentPoolModifier
{
    int Priority { get; }
    bool AffectsContext(ContentRollContext context);
    void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates);
}
