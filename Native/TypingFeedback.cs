using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace TypeMaster.Native;
sealed class TypingFeedback : Adorner
{
    readonly TextBox input;
    List<(int Start, int Length)> mistakes = [];
    string analyzed = "";
    public TypingFeedback(TextBox input) : base(input) { this.input = input; IsHitTestVisible = false; input.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => InvalidateVisual())); }
    public void Refresh(string original, bool includeLast = false)
    {
        if (!includeLast && input.Text.StartsWith(analyzed, StringComparison.Ordinal) && input.Text[analyzed.Length..].All(c => !char.IsWhiteSpace(c))) return;
        analyzed = input.Text;
        var words = Regex.Matches(input.Text, @"\S+").ToArray(); var incorrect = ReportFactory.IncorrectWords(original, input.Text, live: !includeLast); mistakes.Clear();
        for (int i = 0; i < words.Length; i++) if (incorrect[i] && (includeLast || i + 1 < words.Length || char.IsWhiteSpace(input.Text[^1]))) mistakes.Add((words[i].Index, words[i].Length));
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext draw)
    {
        long renderStarted = TypingPerformance.Profiling ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        // An empty overlay must not ask TextBox to calculate line geometry.
        if (mistakes.Count == 0 && input.Text.AsSpan().IndexOfAny('\r', '\n') < 0) return;
        int first = input.GetFirstVisibleLineIndex(), last = input.GetLastVisibleLineIndex(); if (first < 0 || last < 0 || first >= input.LineCount || last >= input.LineCount) return;
        int start = input.GetCharacterIndexFromLineIndex(first), end = last + 1 < input.LineCount ? input.GetCharacterIndexFromLineIndex(last + 1) : input.Text.Length;
        draw.PushClip(new RectangleGeometry(new Rect(new Point(), input.RenderSize))); var pen = new Pen(Brushes.Crimson, 1);
        var marker = new FormattedText("↵", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(input.FontFamily, input.FontStyle, input.FontWeight, input.FontStretch), input.FontSize, input.Foreground, VisualTreeHelper.GetDpi(input).PixelsPerDip);
        for (int i = start; i < end; i++)
        {
            if (input.Text[i] != '\r' && (input.Text[i] != '\n' || i > 0 && input.Text[i - 1] == '\r')) continue;
            Rect position = input.GetRectFromCharacterIndex(i); if (!position.IsEmpty) draw.DrawText(marker, new Point(position.X, position.Y));
        }
        foreach (var range in mistakes) for (int i = Math.Max(start, range.Start); i < Math.Min(end, range.Start + range.Length); i++)
        {
            Rect left = input.GetRectFromCharacterIndex(i), right = input.GetRectFromCharacterIndex(i, true); if (left.IsEmpty || right.IsEmpty) continue;
            draw.DrawLine(pen, new Point(Math.Min(left.X, right.X), left.Bottom - 1), new Point(Math.Max(left.X, right.X), left.Bottom - 1));
        }
        draw.Pop();
        if (renderStarted != 0) TypingPerformance.FeedbackMilliseconds += System.Diagnostics.Stopwatch.GetElapsedTime(renderStarted).TotalMilliseconds;
    }
}
public partial class MainWindow
{
    TypingFeedback? typingFeedback;
    TypingResult? pendingCompletion;
    internal bool CompletionWaiting => pendingCompletion != null;
    void ShowTestCompleted(TypingResult result)
    {
        pendingCompletion = result; typingFeedback?.Refresh(target, true);
        if (PageContent.Children.OfType<Canvas>().FirstOrDefault() is not { } canvas) { ContinueCompleted(); return; }
        var panel = new Canvas { Background = new SolidColorBrush(Color.FromRgb(239, 239, 255)) }; Position(canvas, new Border { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Child = panel }, 50, 70, 307, 108);
        Position(panel, RefTitle("Exercise completed."), 8, 2, 290, 34).FontSize = 23; Position(panel, RefText("Press Enter to continue.", 14, true, true), 8, 40, 285, 28);
        UpdateCountdownPie(1);
        if (TypingBox != null) { TypingBox.IsReadOnly = true; TypingBox.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter && pendingCompletion == result) { e.Handled = true; ContinueCompleted(); } }; TypingBox.Focus(); }
    }
    internal void ContinueCompleted() { if (pendingCompletion is { } result) { pendingCompletion = null; ShowResult(result); } }
}

