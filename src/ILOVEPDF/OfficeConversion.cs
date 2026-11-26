using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace ILOVEPDF;

public partial class MainWindow
{
    private static readonly string[] OfficeExtensions =
        [".doc", ".docx", ".docm", ".odt", ".rtf", ".xls", ".xlsx", ".xlsm", ".ods", ".csv", ".ppt", ".pptx", ".pptm", ".odp"];

    private void OfficeToPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new OpenFileDialog
            {
                Title = "Choose Word, Excel, or PowerPoint files",
                Filter = "Office documents|*.doc;*.docx;*.docm;*.odt;*.rtf;*.xls;*.xlsx;*.xlsm;*.ods;*.csv;*.ppt;*.pptx;*.pptm;*.odp",
                Multiselect = true,
                CheckFileExists = true
            };
            if (picker.ShowDialog(this) != true || picker.FileNames.Length == 0) return;
            var folder = new OpenFolderDialog { Title = "Choose folder for converted PDF files" };
            if (folder.ShowDialog(this) != true) return;

            var converted = 0;
            foreach (var file in picker.FileNames)
            {
                if (!OfficeExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    throw new InvalidOperationException($"Unsupported Office file: {Path.GetFileName(file)}");
                var output = Path.Combine(folder.FolderName, Path.GetFileNameWithoutExtension(file) + ".pdf");
                var tempFolder = Path.Combine(Path.GetTempPath(), "ILOVEPDF", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempFolder);
                try
                {
                    ConvertWithLibreOffice(file, "pdf", tempFolder);
                    var generated = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(file) + ".pdf");
                    if (!File.Exists(generated)) throw new IOException($"LibreOffice did not create a PDF for {Path.GetFileName(file)}.");
                    File.Copy(generated, output, true);
                }
                finally { try { Directory.Delete(tempFolder, true); } catch { } }
                converted++;
            }
            SetStatus($"Converted {converted} Office file(s) to PDF in {folder.FolderName}");
            MessageBox.Show(this, $"Converted {converted} file(s) to PDF.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Office to PDF", ex); }
    }

    private void PdfToWord_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            var save = new SaveFileDialog { Title = "Save converted Word document", Filter = "Word document (*.docx)|*.docx", DefaultExt = ".docx", AddExtension = true, FileName = Path.GetFileNameWithoutExtension(files[0]) + ".docx" };
            if (save.ShowDialog(this) != true) return;

            using var pdf = PdfDocument.Open(files[0]);
            using var word = WordprocessingDocument.Create(save.FileName, WordprocessingDocumentType.Document);
            var mainPart = word.AddMainDocumentPart();
            var body = new W.Body();
            mainPart.Document = new W.Document(body);
            var pages = pdf.GetPages().ToArray();
            for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                var lines = pages[pageIndex].Text.Replace("\r", "").Split('\n');
                foreach (var line in lines)
                {
                    var text = new W.Text(line) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve };
                    body.Append(new W.Paragraph(new W.Run(text)));
                }
                if (pageIndex < pages.Length - 1)
                    body.Append(new W.Paragraph(new W.Run(new W.Break { Type = W.BreakValues.Page })));
            }
            body.Append(new W.SectionProperties(
                new W.PageSize { Width = 11906, Height = 16838 },
                new W.PageMargin { Top = 720, Right = 720, Bottom = 720, Left = 720 }));
            mainPart.Document.Save();
            SetStatus($"Extracted PDF text into Word: {save.FileName}");
            MessageBox.Show(this, "PDF text was converted to an editable Word document. Original page layout and formatting are not fully preserved.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("PDF to Word", ex); }
    }
    private void PdfToPowerPoint_Click(object sender, RoutedEventArgs e) => ConvertPdfToOffice("pptx", "PowerPoint presentation (*.pptx)|*.pptx", ".pptx", "PDF to PowerPoint");

    private void ConvertPdfToOffice(string extension, string filter, string defaultExt, string title)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            var save = new SaveFileDialog { Title = "Save converted Office file", Filter = filter, DefaultExt = defaultExt, AddExtension = true, FileName = Path.GetFileNameWithoutExtension(files[0]) + defaultExt };
            if (save.ShowDialog(this) != true) return;
            var tempFolder = Path.Combine(Path.GetTempPath(), "ILOVEPDF", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            try
            {
                ConvertWithLibreOffice(files[0], extension, tempFolder);
                var generated = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(files[0]) + "." + extension);
                if (!File.Exists(generated)) throw new IOException("LibreOffice could not convert this PDF to the selected Office format. Some PDFs may not convert well because PDF does not preserve the original document structure.");
                File.Copy(generated, save.FileName, true);
            }
            finally { try { Directory.Delete(tempFolder, true); } catch { } }
            SetStatus($"Converted PDF to {extension.ToUpperInvariant()}: {save.FileName}");
            MessageBox.Show(this, $"{title} completed.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(title, ex); }
    }

    private void PdfToExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            var save = new SaveFileDialog { Title = "Save extracted spreadsheet", Filter = "Excel workbook (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true, FileName = Path.GetFileNameWithoutExtension(files[0]) + ".xlsx" };
            if (save.ShowDialog(this) != true) return;

            using var pdf = PdfDocument.Open(files[0]);
            using var workbook = new XLWorkbook();
            foreach (var page in pdf.GetPages())
            {
                var sheetName = $"Page {page.Number}";
                var ws = workbook.Worksheets.Add(sheetName);
                var lines = page.Text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var row = 1;
                foreach (var line in lines)
                {
                    var cells = Regex.Split(line.Trim(), @"\s{2,}|\t").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    if (cells.Length == 0) continue;
                    for (var col = 0; col < cells.Length; col++)
                        ws.Cell(row, col + 1).Value = cells[col].Trim();
                    row++;
                }
                ws.Columns().AdjustToContents();
            }
            workbook.SaveAs(save.FileName);
            SetStatus($"Extracted PDF text into Excel: {save.FileName}");
            MessageBox.Show(this, "PDF text was placed into Excel worksheets by page. Table layout may need manual adjustment.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("PDF to Excel", ex); }
    }

    private static void ConvertWithLibreOffice(string inputFile, string targetExtension, string outputFolder)
    {
        var soffice = FindLibreOffice();
        if (soffice is null)
            throw new FileNotFoundException("LibreOffice conversion engine was not found. Use the portable ILOVEPDF package (with its LibreOffice folder) or install LibreOffice locally. No paid service is required.");
        Directory.CreateDirectory(outputFolder);
        var profile = Path.Combine(Path.GetTempPath(), "ILOVEPDF_LO_Profile_" + Guid.NewGuid().ToString("N"));
        var start = new ProcessStartInfo
        {
            FileName = soffice,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-env:UserInstallation=" + new Uri(profile).AbsoluteUri);
        start.ArgumentList.Add("--headless");
        start.ArgumentList.Add("--convert-to");
        start.ArgumentList.Add(targetExtension);
        start.ArgumentList.Add("--outdir");
        start.ArgumentList.Add(outputFolder);
        start.ArgumentList.Add(inputFile);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start LibreOffice.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(300000))
        {
            try { process.Kill(true); } catch { }
            throw new TimeoutException("LibreOffice conversion exceeded 5 minutes.");
        }
        var output = stdout.GetAwaiter().GetResult();
        var error = stderr.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"LibreOffice conversion failed. {error} {output}".Trim());
    }

    private static string? FindLibreOffice()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "LibreOffice", "program", "soffice.exe"),
            Path.Combine(baseDir, "tools", "LibreOffice", "program", "soffice.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program", "soffice.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LibreOffice", "program", "soffice.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
