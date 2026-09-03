using System.Collections.Generic;

public sealed class UIPatchApplier
{
    public void Apply(
        IUIPresentationRefProvider refs, 
        IReadOnlyList<IUIPatch> patches)
    {
        foreach (IUIPatch patch in patches) 
            patch?.Apply(refs);
    }
}