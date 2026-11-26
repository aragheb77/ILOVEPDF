# ILOVEPDF Desktop

A Windows-first, offline PDF toolkit inspired by common PDF workflows. This is an independent implementation; it does not copy iLovePDF's proprietary source code, assets, or exact interface.

## Product principles
- Local-first: documents are processed on the device; no account, cloud upload, paid API, or service dependency.
- Windows desktop UI, keyboard-friendly and approachable.
- Original files are not overwritten by default.
- Third-party dependencies and their licenses must be reviewed before distribution.

## Implemented in this version
### Organize
- Merge multiple PDFs
- Split into one-page PDF files
- Extract pages by page ranges (e.g. `1,3-5`)
- Remove pages by page ranges
- Rotate all pages by 90 degrees

### Convert (offline)
- Multiple JPG, JPEG, PNG, BMP, GIF, and TIFF images to a multi-page PDF (each image is fitted onto an A4 page)
- PDF to selectable text (.txt) using local text extraction
- TXT to PDF with automatic line wrapping and pagination

### Security
- Protect a PDF with an open password

Also includes native file/folder pickers, output path selection, status messages, and error dialogs.

**Conversion limitation:** PDF-to-text extracts text embedded in the PDF. Scanned/image-only pages require OCR, which is not yet included. PDF-to-image and Office document conversions are not yet implemented in this build and are not represented as completed features.

## Main feature coverage
The wider iLovePDF catalog includes additional workflows such as Office/PDF conversion, PDF-to-image, OCR, compression, page reordering, watermarking, page numbering, forms, signatures, redaction, and repair. These are not all implemented in this version. This release is focused on the offline conversions and PDF operations listed above, without paid services or cloud processing.

## Build
Open `src/ILOVEPDF/ILOVEPDF.csproj` in Visual Studio 2022 with the .NET 8 SDK, or run on Windows:

```powershell
dotnet restore src/ILOVEPDF/ILOVEPDF.csproj
dotnet build src/ILOVEPDF/ILOVEPDF.csproj -c Release
dotnet publish src/ILOVEPDF/ILOVEPDF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

GitHub Actions builds a self-contained Windows x64 single-file EXE and uploads the publish output as a workflow artifact.

## Licensing
The project code is MIT. PDFsharp and PdfPig are third-party open-source dependencies; review their current licenses and all transitive dependencies before distribution. Keep dependency notices with release artifacts. The app is not affiliated with or endorsed by iLovePDF.
