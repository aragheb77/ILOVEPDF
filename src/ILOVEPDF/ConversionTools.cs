using Microsoft.Win32;
using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ILOVEPDF;

public partial class MainWindow
{
    private void ImagesToPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new OpenFileDialog
            {
                Title = "Choose images to convert",
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff",
                Multiselect = true,
                CheckFileExists = true
            };
            if (picker.ShowDialog(this) != true || picker.FileNames.Length == 0) return;
            var outputPath = SavePdf("images.pdf");
            if (outputPath is null) return;

            using var output = new PdfDocument();
            foreach (var file in picker.FileNames)
            {
                var decoder = BitmapDecoder.Create(new Uri(file), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0]));
                using var pngStream = new MemoryStream();
                encoder.Save(pngStream);
                pngStream.Position = 0;

                using var image = XImage.FromStream(pngStream);
                var page = output.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                using var graphics = XGraphics.FromPdfPage(page);
                const double margin = 28;
                var availableWidth = page.Width.Point - margin * 2;
                var availableHeight = page.Height.Point - margin * 2;
                var scale = Math.Min(availableWidth / image.PixelWidth, availableHeight / image.PixelHeight);
                var width = image.PixelWidth * scale;
                var height = image.PixelHeight * scale;
                graphics.DrawImage(image, (page.Width.Point - width) / 2, (page.Height.Point - height) / 2, width, height);
            }

            output.Save(outputPath);
            SetStatus($"Converted {picker.FileNames.Length} images to PDF: {outputPath}");
            MessageBox.Show(this, $"Created a PDF with {picker.FileNames.Length} image page(s).", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Images to PDF", ex); }
    }

    private void PdfToText_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = PickPdfs();
            if (files.Length == 0) return;
            var save = new SaveFileDialog
            {
                Title = "Save extracted text",
                Filter = "Text files (*.txt)|*.txt",
                DefaultExt = ".txt",
                AddExtension = true,
                FileName = Path.GetFileNameWithoutExtension(files[0]) + ".txt"
            };
            if (save.ShowDialog(this) != true) return;

            var text = new StringBuilder();
            using (var pdf = UglyToad.PdfPig.PdfDocument.Open(files[0]))
            {
                foreach (var page in pdf.GetPages())
                {
                    text.AppendLine(page.Text);
                    text.AppendLine();
                }
            }
            File.WriteAllText(save.FileName, text.ToString(), new UTF8Encoding(false));
            SetStatus($"Extracted text to: {save.FileName}");
            MessageBox.Show(this, "Text extraction completed. Scanned pages require OCR and may not contain selectable text.", "ILOVEPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("PDF to text", ex); }
    }

    private void TextToPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new OpenFileDialog
            {
                Title = "Choose a text file",
                Filter = "Text files (*.txt)|*.txt",
                CheckFileExists = true
            };
            if (picker.ShowDialog(this) != true) return;
            var text = File.ReadAllText(picker.FileName);
            var outputPath = SavePdf(Path.GetFileNameWithoutExtension(picker.FileName) + ".pdf");
            if (outputPath is null) return;

            using var pdf = new PdfDocument();
            var font = new XFont("Arial", 11);
            const double margin = 42;
            const double lineHeight = 16;
            PdfPage? page = null;
            XGraphics? graphics = null;
            double y = margin;
            void NewPage()
            {
                graphics?.Dispose();
                page = pdf.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                graphics = XGraphics.FromPdfPage(page);
                y = margin;
            }

            NewPage();
            const double maxWidth = 595.28 - margin * 2;
            foreach (var sourceLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split(new[] { '\n' }, StringSplitOptions.None))
            {
                if (sourceLine.Length == 0)
                {
                    y += lineHeight;
                    if (y > page!.Height.Point - margin) NewPage();
                    continue;
                }
                var words = sourceLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var line = new StringBuilder();
                foreach (var word in words)
                {
                    var candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && graphics!.MeasureString(candidate, font).Width > maxWidth)
                    {
                        if (y > page!.Height.Point - margin) NewPage();
                        graphics!.DrawString(line.ToString(), font, XBrushes.Black, margin, y);
                        y += lineHeight;
                        line.Clear();
                        line.Append(word);
                    }
                    else line.Clear().Append(candidate);
                }
                if (line.Length > 0)
                {
                    if (y > page!.Height.Point - margin) NewPage();
                    graphics!.DrawString(line.ToString(), font, XBrushes.Black, margin, y);
                    y += lineHeight;
                }
            }
            graphics?.Dispose();
            pdf.Save(outputPath);
            SetStatus($"Converted text to PDF: {outputPath}");
        }
        catch (Exception ex) { ShowError("Text to PDF", ex); }
    }
}
