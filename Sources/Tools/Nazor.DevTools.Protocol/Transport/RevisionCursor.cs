namespace Nazor.DevTools.Protocol;

public enum RevisionOutcome { Accepted, Stale, ResyncRequired }

public sealed class RevisionCursor
{
    private readonly RequestContext context;
    private long tree;
    private long frames;
    private bool treeGap = true;
    private bool frameGap;

    public RevisionCursor(RequestContext context)
    {
        DevToolsJson.Write(context);
        this.context = context;
    }

    public RevisionOutcome Accept(TreeSnapshot snapshot)
    {
        DevToolsJson.Write(snapshot);
        if (!Matches(snapshot.Capture) || snapshot.Revision < tree) return RevisionOutcome.Stale;
        tree = snapshot.Revision;
        treeGap = false;
        return RevisionOutcome.Accepted;
    }

    public RevisionOutcome Accept(TreeDelta delta)
    {
        DevToolsJson.Write(delta);
        if (!Matches(delta.Capture) || delta.Revision <= tree) return RevisionOutcome.Stale;
        if (treeGap || delta.BaseRevision != tree) { treeGap = true; return RevisionOutcome.ResyncRequired; }
        tree = delta.Revision;
        return RevisionOutcome.Accepted;
    }

    public RevisionOutcome Accept(FrameBatch batch, bool resynchronize = false)
    {
        DevToolsJson.Write(batch);
        if (!Matches(batch.Capture) || batch.Revision <= frames) return RevisionOutcome.Stale;
        if (resynchronize) { frames = batch.Revision; frameGap = false; return RevisionOutcome.Accepted; }
        if (frameGap || batch.BaseRevision != frames) { frameGap = true; return RevisionOutcome.ResyncRequired; }
        frames = batch.Revision;
        return RevisionOutcome.Accepted;
    }

    private bool Matches(CaptureStamp stamp) => stamp.SessionId == context.SessionId && stamp.Generation == context.Generation && stamp.StopId == context.StopId;
}
