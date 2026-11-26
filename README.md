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
- Word, Excel, and PowerPoint files (DOC/DOCX/DOCM/ODT/RTF, XLS/XLSX/XLSM/ODS/CSV, PPT/PPTX/PPTM/ODP) to PDF
- PDF to editable Word (DOCX) using local text extraction, and PowerPoint (PPTX) through LibreOffice
- PDF text to Excel (XLSX), separated into worksheets by PDF page; column/table reconstruction is heuristic

### Security
- Protect a PDF with an open password

Also includes native file/folder pickers, output path selection, status messages, and error dialogs.

**Conversion limitations:** PDF-to-text extracts selectable text only; scanned/image-only pages require OCR, which is not included yet. PDF-to-Excel extracts text into worksheets and does not guarantee original table layout. PDF-to-Word reconstructs editable text but does not preserve the original layout; PDF-to-PowerPoint may lose formatting because PDF does not preserve the original Office structure.

## Main feature coverage
The wider iLovePDF catalog includes additional workflows such as PDF-to-image, OCR, compression, page reordering, watermarking, page numbering, forms, signatures, redaction, and repair. These are not all implemented yet. Office conversions use the free, open-source LibreOffice engine bundled in the portable package; it is a sizeable companion folder next to the app executable, not embedded inside the .NET EXE. All processing remains local, without paid services or cloud uploads.

## Build
Open `src/ILOVEPDF/ILOVEPDF.csproj` in Visual Studio 2022 with the .NET 8 SDK, or run on Windows:

```powershell
dotnet restore src/ILOVEPDF/ILOVEPDF.csproj
dotnet build src/ILOVEPDF/ILOVEPDF.csproj -c Release
dotnet publish src/ILOVEPDF/ILOVEPDF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

GitHub Actions builds a self-contained Windows x64 single-file EXE, bundles LibreOffice under `LibreOffice/`, and uploads a portable package artifact. Keep the EXE and `LibreOffice` folder together so Office conversions work.

## Licensing
The project code is MIT. PDFsharp, PdfPig, ClosedXML, and DocumentFormat.OpenXml are third-party open-source dependencies; review their current licenses and all transitive dependencies before distribution. LibreOffice is bundled separately under its applicable MPL-2.0 and LGPLv3+ licenses; preserve its upstream notices. Keep dependency notices with release artifacts. The app is not affiliated with or endorsed by iLovePDF.
