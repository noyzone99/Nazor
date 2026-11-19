using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Feedback" group.
    private static IEnumerable<GalleryEntry> FeedbackEntries() =>
    [
        new("progress-bar", "ProgressBar", "Feedback", "Compare determinate and indeterminate bars and a circular indicator.", typeof(ProgressBar), typeof(Examples.Feedback.ProgressBarSample), typeof(Spinner)),
        new("progress-indicators", "Spinner and Meter", "Feedback", "Show an indeterminate operation alongside a measurement with thresholds.", typeof(Spinner), typeof(Examples.Feedback.ProgressIndicatorsSample), typeof(Meter)),
        new("messages", "InlineMessage and Banner", "Feedback", "Show a dismissible form message and a page-wide announcement.", typeof(InlineMessage), typeof(Examples.Feedback.MessagesSample), typeof(Banner)),
        new("avatars-badges", "Avatar, Badge and Tag", "Feedback", "Show a team, an unread count and a removable status tag.", typeof(AvatarGroup), typeof(Examples.Feedback.AvatarsBadgesSample), typeof(Avatar), typeof(Badge), typeof(Tag)),
        new("loading-states", "Skeleton and EmptyState", "Feedback", "Switch between placeholder content and an empty result.", typeof(EmptyState), typeof(Examples.Feedback.LoadingStatesSample), typeof(Skeleton)),
        new("toast", "Toast and ToastHost", "Feedback", "Display a standalone toast and queue notifications in the window.", typeof(ToastHost), typeof(Examples.Feedback.ToastSample), typeof(Toast)),
    ];
}
