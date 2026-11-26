using Microsoft.Win32;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Windows;
using System.IO;

namespace ILOVEPDF;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private string[] PickPdfs(bool multiple = false)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Multiselect = multiple,
            CheckFileExists = true,
            Title = "Choose PDF file(s)"
        };
        return dialog.ShowDialog(this) == true ? dialog.FileNames : [];
    }

    private string? SavePdf(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = suggestedName,
            AddExtension = true
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private void Home_Click(object sender, RoutedEventArgs e) => SetStatus("Ready — choose a PDF tool.");

    private void Merge_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs(true);
            if (files.Length < 2) { SetStatus("Select at least two PDF files to merge."); return; }
            var output = SavePdf("merged.pdf");
            if (output is null) return;
            using var merged = new PdfDocument();
            foreach (var file in files)
            {
                using var input = PdfReader.Open(file, PdfDocumentOpenMode.Import);
                foreach (var page in input.Pages) merged.AddPage(page);
            }
            merged.Save(output);
            SetStatus($"Merged {files.Length} files successfully. Saved to: {output}");
            MessageBox.Show(this, "PDFs merged successfully.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Merge PDF", ex); }
    }

    private void Split_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            using var input = PdfReader.Open(files[0], PdfDocumentOpenMode.Import);
            var folderDialog = new OpenFolderDialog { Title = "Choose output folder" };
            if (folderDialog.ShowDialog(this) != true) return;
            var baseName = Path.GetFileNameWithoutExtension(files[0]);
            for (int i = 0; i < input.PageCount; i++)
            {
                using var output = new PdfDocument();
                output.AddPage(input.Pages[i]);
                output.Save(Path.Combine(folderDialog.FolderName, $"{baseName}_page_{i + 1:000}.pdf"));
            }
            SetStatus($"Split into {input.PageCount} one-page PDFs in: {folderDialog.FolderName}");
            MessageBox.Show(this, $"Created {input.PageCount} PDF files.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Split PDF", ex); }
    }

    private void Rotate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            using var input = PdfReader.Open(files[0], PdfDocumentOpenMode.Modify);
            foreach (var page in input.Pages)
                page.Rotate = (page.Rotate + 90) % 360;
            var output = SavePdf(Path.GetFileNameWithoutExtension(files[0]) + "_rotated.pdf");
            if (output is null) return;
            input.Save(output);
            SetStatus($"Rotated {input.PageCount} pages. Saved to: {output}");
        }
        catch (Exception ex) { ShowError("Rotate PDF", ex); }
    }

    private void Protect_Click(object sender, RoutedEventArgs e)
    {
        var files = PickPdfs();
        if (files.Length == 0) return;
        var password = PasswordPrompt.Show(this);
        if (string.IsNullOrWhiteSpace(password)) { SetStatus("Password protection cancelled."); return; }
        try
        {
            using var input = PdfReader.Open(files[0], PdfDocumentOpenMode.Modify);
            input.SecuritySettings.UserPassword = password;
            input.SecuritySettings.OwnerPassword = Guid.NewGuid().ToString("N");
            input.SecuritySettings.PermitPrint = true;
            input.SecuritySettings.PermitModifyDocument = false;
            input.SecuritySettings.PermitExtractContent = false;
            var output = SavePdf(Path.GetFileNameWithoutExtension(files[0]) + "_protected.pdf");
            if (output is null) return;
            input.Save(output);
            SetStatus($"Password protected PDF saved to: {output}");
        }
        catch (Exception ex) { ShowError("Protect PDF", ex); }
    }

    private void Extract_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            var range = PageRangePrompt.Show(this, "Extract pages", "Pages to extract (e.g. 1,3-5):");
            if (string.IsNullOrWhiteSpace(range)) return;
            using var input = PdfReader.Open(files[0], PdfDocumentOpenMode.Import);
            var pages = ParsePageRanges(range, input.PageCount);
            var outputPath = SavePdf(Path.GetFileNameWithoutExtension(files[0]) + "_extracted.pdf");
            if (outputPath is null) return;
            using var output = new PdfDocument();
            foreach (var pageNumber in pages) output.AddPage(input.Pages[pageNumber - 1]);
            output.Save(outputPath);
            SetStatus($"Extracted {pages.Count} pages to: {outputPath}");
        }
        catch (Exception ex) { ShowError("Extract pages", ex); }
    }

    private void RemovePages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            var range = PageRangePrompt.Show(this, "Remove pages", "Pages to remove (e.g. 2,4-6):");
            if (string.IsNullOrWhiteSpace(range)) return;
            using var input = PdfReader.Open(files[0], PdfDocumentOpenMode.Import);
            var remove = ParsePageRanges(range, input.PageCount).ToHashSet();
            if (remove.Count == input.PageCount)
            {
                MessageBox.Show(this, "You cannot remove every page from a PDF.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var outputPath = SavePdf(Path.GetFileNameWithoutExtension(files[0]) + "_pages_removed.pdf");
            if (outputPath is null) return;
            using var output = new PdfDocument();
            for (int i = 0; i < input.PageCount; i++)
                if (!remove.Contains(i + 1)) output.AddPage(input.Pages[i]);
            output.Save(outputPath);
            SetStatus($"Removed {remove.Count} pages. Saved to: {outputPath}");
        }
        catch (Exception ex) { ShowError("Remove pages", ex); }
    }

    private static List<int> ParsePageRanges(string range, int pageCount)
    {
        var pages = new SortedSet<int>();
        foreach (var part in range.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bounds = part.Split('-', StringSplitOptions.TrimEntries);
            if (bounds.Length is < 1 or > 2 ||
                !int.TryParse(bounds[0], out var start) ||
                (bounds.Length == 2 && !int.TryParse(bounds[1], out _)))
                throw new ArgumentException($"Invalid page range: {part}");
            var end = start;
            if (bounds.Length == 2) end = int.Parse(bounds[1]);
            if (start < 1 || end < start || end > pageCount)
                throw new ArgumentOutOfRangeException(nameof(range), $"Page range {part} is outside 1-{pageCount}.");
            for (int page = start; page <= end; page++) pages.Add(page);
        }
        if (pages.Count == 0) throw new ArgumentException("Enter at least one page number.");
        return pages.ToList();
    }

    private void ShowError(string operation, Exception ex)
    {
        SetStatus($"{operation} failed: {ex.Message}");
        MessageBox.Show(this, $"{operation} could not be completed.\n\n{ex.Message}", "ILOVEPDF — Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

internal static class PasswordPrompt
{
    public static string? Show(Window owner)
    {
        var dialog = new Window
        {
            Title = "Set PDF password",
            Width = 380, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner, ResizeMode = ResizeMode.NoResize, Background = System.Windows.Media.Brushes.White
        };
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Enter an open password for the output PDF:", Margin = new Thickness(0, 0, 0, 10) });
        var box = new System.Windows.Controls.PasswordBox { MinWidth = 300, Padding = new Thickness(8) };
        panel.Children.Add(box);
        var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var ok = new System.Windows.Controls.Button { Content = "Protect", IsDefault = true, Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new System.Windows.Controls.Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(18, 8, 18, 8) };
        string? result = null;
        ok.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(box.Password)) { result = box.Password; dialog.DialogResult = true; } };
        buttons.Children.Add(ok); buttons.Children.Add(cancel); panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
        return result;
    }
}

internal static class PageRangePrompt
{
    public static string? Show(Window owner, string title, string label)
    {
        var dialog = new Window
        {
            Title = title, Width = 420, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner, ResizeMode = ResizeMode.NoResize, Background = System.Windows.Media.Brushes.White
        };
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 10) });
        var box = new System.Windows.Controls.TextBox { MinWidth = 340, Padding = new Thickness(8) };
        panel.Children.Add(box);
        var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var ok = new System.Windows.Controls.Button { Content = "Continue", IsDefault = true, Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new System.Windows.Controls.Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(18, 8, 18, 8) };
        string? result = null;
        ok.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(box.Text)) { result = box.Text; dialog.DialogResult = true; } };
        buttons.Children.Add(ok); buttons.Children.Add(cancel); panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
        return result;
    }
}
