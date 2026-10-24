using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TypeMaster.Native;
public static class TestImport
{
    const int Limit = 500_000;
    public static string Read(string file)
    {
        if (new FileInfo(file).Length > 4_000_000) throw new InvalidDataException("Choose a document smaller than 4 MB.");
        if (Path.GetExtension(file).Equals(".docx", StringComparison.OrdinalIgnoreCase)) { using var input = File.OpenRead(file); return Word(input); }
        return Clean(Assets.Read(file), Path.GetExtension(file));
    }
    static string Word(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true); var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("This Word document has no main body.");
        if (entry.Length > 4_000_000) throw new InvalidDataException("Choose a smaller Word document.");
        using var body = entry.Open(); using var xml = XmlReader.Create(body, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4_000_000 }); var document = XDocument.Load(xml); XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        return Validate(string.Join("\n\n", document.Descendants(w + "p").Select(p => string.Concat(p.Descendants().Where(n => n.Name == w + "t" || n.Name == w + "tab" || n.Name == w + "br").Select(n => n.Name == w + "tab" ? " " : n.Name == w + "br" ? "\n" : n.Value)))));
    }
    static string Clean(string text, string extension)
    {
        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)) { text = Regex.Replace(text, @"<(script|style)\b[^>]*>.*?</\1>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline); text = Regex.Replace(text, @"<\s*(?:br\b[^>]*|/p|/div|/li)\s*>", "\n", RegexOptions.IgnoreCase); text = WebUtility.HtmlDecode(Regex.Replace(text, "<[^>]+>", "")); }
        return Validate(text);
    }
    public static string Validate(string text) { text = text.Trim().TrimStart('\uFEFF'); if (text.Length == 0 || text.Length > Limit) throw new InvalidDataException("Choose a non-empty test with at most 500,000 characters."); return text; }
    public static string Save(string directory, string title, string text)
    {
        text = Validate(text); Directory.CreateDirectory(directory); title = string.Concat(title.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim(); if (title.Length == 0) title = "Custom Test"; if (title.Length > 90) title = title[..90];
        string path = Path.Combine(directory, title + "-" + Guid.NewGuid().ToString("N")[..6] + ".txt"); File.WriteAllText(path, text, Encoding.UTF8); return path;
    }
    public static int Pack(string file, string directory)
    {
        using var archive = ZipFile.OpenRead(file); string[] formats = [".txt", ".exm", ".html", ".htm", ".docx"]; var entries = archive.Entries.Where(e => formats.Contains(Path.GetExtension(e.Name).ToLowerInvariant()) && e.Name.Length > 0).ToList();
        if (entries.Count == 0 || entries.Count > 100 || entries.Sum(e => e.Length) > 25_000_000 || entries.Any(e => e.Length > 4_000_000)) throw new InvalidDataException("A test pack must contain 1-100 supported documents and at most 25 MB of uncompressed data.");
        var texts = new List<(string Name, string Text)>();
        foreach (var entry in entries) { using var stream = entry.Open(); string text; if (Path.GetExtension(entry.Name).Equals(".docx", StringComparison.OrdinalIgnoreCase)) { using var memory = new MemoryStream(); stream.CopyTo(memory); memory.Position = 0; text = Word(memory); } else { using var reader = new StreamReader(stream, Encoding.UTF8, true); text = Clean(reader.ReadToEnd(), Path.GetExtension(entry.Name)); } texts.Add((Path.GetFileNameWithoutExtension(entry.Name), text)); }
        foreach (var text in texts) Save(directory, text.Name, text.Text); return texts.Count;
    }
}
public partial class MainWindow
{
    bool testAllLanguages;
    string CustomTextsDirectory => Path.Combine(Store.DirectoryPath, "Texts");
    void AddCustomTest()
    {
        var dialog = new Window { Title = "Add Custom Test", Width = 650, Height = 490, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.White, Foreground = Brushes.Black, FontFamily = new FontFamily("Tahoma"), FontSize = 14 };
        var root = new DockPanel { Margin = new Thickness(18) }; dialog.Content = root;
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top); top.Children.Add(new TextBlock { Text = "Test name", Margin = new Thickness(0, 0, 0, 5) }); var title = new TextBox { Text = "My Custom Test", MaxLength = 90, Padding = new Thickness(5) }; top.Children.Add(title); top.Children.Add(new TextBlock { Text = "Test passage", Margin = new Thickness(0, 5, 0, 8) });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions); var text = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(8), MaxLength = 500_000 };
        actions.Children.Add(Button("Save test", () => { try { TestImport.Save(CustomTextsDirectory, title.Text, text.Text); dialog.DialogResult = true; } catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Custom test"); } })); actions.Children.Add(Outline("Cancel", () => dialog.Close())); root.Children.Add(text); if (dialog.ShowDialog() == true) Tests();
    }
    void ImportTestDocument(bool pack = false)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = pack ? "Test pack|*.zip" : "Test documents|*.txt;*.exm;*.docx;*.html;*.htm", Multiselect = !pack }; if (dialog.ShowDialog(this) != true) return;
        try { int count = 0; if (pack) count = TestImport.Pack(dialog.FileName, CustomTextsDirectory); else foreach (string file in dialog.FileNames) { TestImport.Save(CustomTextsDirectory, Path.GetFileNameWithoutExtension(file), TestImport.Read(file)); count++; } Tests(); Notify(count + " test(s) imported.", "Import tests"); } catch (Exception ex) { Notify(ex.Message, "Import could not be completed"); }
    }
}

