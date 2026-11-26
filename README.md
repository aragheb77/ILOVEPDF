# ILOVEPDF Desktop

A Windows-first, offline PDF toolkit inspired by common PDF workflows. This is an independent implementation; it does not copy iLovePDF's proprietary source code, assets, or exact interface.

## Product principles
- Local-first: documents are processed on the device; no account, cloud upload, paid API, or service dependency.
- Windows desktop UI, keyboard-friendly and approachable.
- Clear status, progress, cancellation where supported, and non-destructive output defaults.
- Third-party dependencies must be reviewed for license compatibility before shipping.
- Publish reproducible builds and test results through GitHub Actions.

## Current implementation
The initial application foundation includes:
- WPF desktop shell with a clean tool dashboard
- Merge PDF
- Split PDF into one-page files
- Protect PDF with a password
- PDF page rotation
- Native Windows open/save dialogs and output folder selection

## Planned feature parity roadmap
### Organize
Merge, split by ranges, extract pages, remove pages, reorder pages, rotate, scan/image-to-PDF, PDF/A export.

### Optimize
Compression presets, repair/recovery, OCR (local Tesseract language packs).

### Convert
Image to PDF, PDF to images/text/HTML/Markdown, Office to PDF and PDF to Office (local conversion engines bundled with the installer).

### Edit
Text/image/shape annotations, page numbers, text/image watermark, crop, fill forms, flatten, metadata editor, redact, compare, e-signature.

### Security
Encrypt/protect, unlock when the user supplies a valid password, remove metadata, verify and display signature information.

### Intelligence (optional local-only)
Local OCR and optional local model integrations. Cloud AI features are not included because the project requirement forbids paid/external services.

## Build
Open `src/ILOVEPDF/ILOVEPDF.csproj` in Visual Studio 2022 with the .NET 8 SDK, or run on Windows:

```powershell
dotnet restore src/ILOVEPDF/ILOVEPDF.csproj
dotnet build src/ILOVEPDF/ILOVEPDF.csproj -c Release
dotnet publish src/ILOVEPDF/ILOVEPDF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The publish output is a self-contained Windows executable. GitHub Actions also builds and uploads the publish directory as an artifact.

## Licensing
The project code is MIT. PDFsharp is an open-source dependency; review its current license and all transitive/native dependencies before distribution. Keep dependency notices with release artifacts. The app is not affiliated with or endorsed by iLovePDF.
