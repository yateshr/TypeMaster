using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    DateTime exerciseStarted;
    string exerciseSourceText = "";
    internal void StartExercise(string text, string title, int seconds, string exerciseKind = "Practice", string? id = null)
    {
        if (!restoringDraft) ClearExerciseDraft();
        timer.Stop(); watch.Reset(); restoredElapsed = 0; attemptId = Guid.NewGuid();
        pendingCompletion = null; typingFeedback = null;
        exerciseStarted = DateTime.Now;
        exerciseSourceText = text;
        paragraphSpacing = exerciseKind == "Test" && Regex.IsMatch(text.Trim(), @"\r?\n\s*\r?\n");
        target = exerciseKind == "Test" ? Regex.Replace(text.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' '), @" *\n(?:\s*\n)*", "\n").Trim() : Assets.Normalize(text);
        numericPassage = exerciseKind == "Test" && target.Contains('\n') && target.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || ".,+-*/=()".Contains(c));
        if (numericPassage) paragraphSpacing = false;
        countdownPie = null;
        if (target.Length == 0) { Notify("This exercise has no text."); return; }
        typed = ""; exerciseTitle = title; duration = seconds; kind = exerciseKind; lessonId = id; attempts = errors = 0; sessionMistakes = []; paused = false; lastInput = DateTime.UtcNow;
        exercising = true; Navigation.Visibility = Visibility.Collapsed; NavColumn.Width = new GridLength(0);
        if (exerciseKind == "ReviewKeys") { BuildKeyDrill(); return; }
        var canvas = ReferencePage(null, true); var paper = new Border(); paper.SetResourceReference(Border.BackgroundProperty, "SoftBrush"); Position(canvas, paper, 0, 0, 568, 529);
        var side = new Border { Background = Current!.Dark ? new SolidColorBrush(Color.FromRgb(35, 57, 87)) : new SolidColorBrush(Color.FromRgb(213, 229, 250)) }; Position(canvas, side, 568, 0, 210, 529);
        targetBlock = new TextBlock { FontFamily = new FontFamily("Georgia"), FontSize = 19, LineHeight = 25, TextWrapping = TextWrapping.Wrap }; targetBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        targetBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        targetViewport = new ScrollViewer { Content = targetBlock, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
        Position(canvas, targetViewport, 37, 27, 480, 225);
        TypingBox = new TextBox { FontFamily = new FontFamily("Georgia"), FontSize = 19, Padding = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap, AcceptsReturn = exerciseKind == "Test", IsUndoEnabled = false, AllowDrop = false, Background = Brushes.Transparent, BorderBrush = Brushes.RoyalBlue, BorderThickness = new Thickness(0, 1, 0, 0), VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        TypingBox.Margin = new Thickness(0); var decorator = new AdornerDecorator { Child = TypingBox }; Position(canvas, decorator, 37, 270, 492, 111);
        foreach (var textControl in new System.Windows.DependencyObject[] { targetBlock, TypingBox }) { TextOptions.SetTextFormattingMode(textControl, TextFormattingMode.Display); TextOptions.SetTextHintingMode(textControl, TextHintingMode.Fixed); }
        TypingBox.SelectionChanged += (_, _) => { if (exercising && !restoringDraft) Paint(); };
        var input = TypingBox; input.Loaded += (_, _) => { if (ReferenceEquals(input, TypingBox) && kind == "Test" && AdornerLayer.GetAdornerLayer(input) is { } layer) { typingFeedback = new TypingFeedback(input); layer.Add(typingFeedback); typingFeedback.Refresh(target); } };
        DataObject.AddPastingHandler(TypingBox, (_, e) => e.CancelCommand()); TypingBox.TextChanged += (_, _) => InputChanged();
        Position(canvas, new Ellipse { Fill = new SolidColorBrush(Color.FromRgb(174, 216, 110)) }, 144, 375, 22, 22);
        Position(canvas, new TextBlock { Text = "▶", FontSize = 12, Foreground = Brushes.White }, 149, 377, 18, 18);
        Position(canvas, RefText("Type the above text here", 13), 174, 377, 280, 23).Foreground = new SolidColorBrush(Color.FromRgb(121, 155, 49));
        Position(canvas, RefText("Time", 14, true, true), 585, 48, 180, 22); Rule(canvas, 584, 67, 180, Brushes.WhiteSmoke);
        clockBlock = RefText("", 24, true); clockBlock.FontFamily = new FontFamily("Tahoma"); Position(canvas, clockBlock, 585, 70, 180, 36);
        var oval = new Ellipse { Stroke = new SolidColorBrush(Color.FromRgb(166, 196, 57)), StrokeThickness = .7 }; oval.SetResourceReference(Shape.FillProperty, "SoftBrush"); Position(canvas, oval, 609, 104, 122, 67);
        if (exerciseKind == "Test") { countdownPie = new System.Windows.Shapes.Path { Fill = new SolidColorBrush(Color.FromRgb(155, 193, 85)), IsHitTestVisible = false }; Position(canvas, countdownPie, 609, 104, 122, 67); }
        speedBlock = RefText("", 18, true); speedBlock.TextAlignment = TextAlignment.Center; Position(canvas, speedBlock, 609, 117, 122, 27);
        accuracyBlock = RefText("", 11); accuracyBlock.TextAlignment = TextAlignment.Center; Position(canvas, accuracyBlock, 609, 145, 122, 22);
        if (exerciseKind == "Test") { speedBlock.Visibility = Visibility.Collapsed; accuracyBlock.Visibility = Visibility.Collapsed; }
        void Leave() { exercising = false; timer.Stop(); if (id != null && openedCourse != null) ShowChapter(openedCourse, openedChapter); else ShowPage(page); }
        void Cancel() { if (pendingCompletion != null) { pendingCompletion = null; Leave(); return; } if (typed.Length == 0 || MessageBox.Show(this, "Cancel this exercise without saving?", "TypeMaster", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { ClearExerciseDraft(); Leave(); } }
        Position(canvas, RefButton("Next", () => { if (pendingCompletion != null) ContinueCompleted(); else FinishExercise(); }), 584, 449, 180, 34); var cancel = RefButton("Cancel", Cancel, true); cancel.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, cancel, 584, 493, 180, 34);
        ReferenceClose(canvas, 746, 13, Cancel);
        var menu = new ContextMenu(); var pause = new MenuItem { Header = "Pause / resume exercise", IsCheckable = true }; pause.Click += (_, _) => { paused = pause.IsChecked; TypingBox.IsEnabled = !paused; if (paused) watch.Stop(); else { if (typed.Length > 0) watch.Start(); TypingBox.Focus(); } }; menu.Items.Add(pause); canvas.ContextMenu = menu;
        clockBlock.ToolTip = "Right-click to pause or resume.";
        liveKeyboard = null; if (id != null) { liveKeyboard = new KeyboardView(LoadKeyboard()) { MaxHeight = 105, Stretch = Stretch.Fill }; Position(canvas, liveKeyboard, 30, 418, 508, 105); liveKeyboard.Highlight(target[0].ToString()); }
        Paint(); UpdateMetrics(); TypingBox.Focus();
    }
}


