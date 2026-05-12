public enum NPCTraitType
{
    Honest,
    Neutral,
    Liar
}

public readonly struct NPCTraitTokenProfile
{
    public int TruthTokens { get; }
    public int LieTokens { get; }
    public int MinDetectiveTokens { get; }
    public int MaxDetectiveTokens { get; }

    public NPCTraitTokenProfile(int truthTokens, int lieTokens, int minDetectiveTokens, int maxDetectiveTokens)
    {
        TruthTokens = truthTokens;
        LieTokens = lieTokens;
        MinDetectiveTokens = minDetectiveTokens;
        MaxDetectiveTokens = maxDetectiveTokens;
    }
}
