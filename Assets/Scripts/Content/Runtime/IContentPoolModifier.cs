using System.Collections.Generic;

public interface IContentPoolModifier
{
    int Priority { get; }
    bool AffectsPurpose(ContentPoolPurpose purpose);
    void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates);
}
