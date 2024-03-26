using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Integrator.Shared.FluentImpex
{
    public class ExcelGenerator
    {
        public Stream Generate(string title, List<string> header, List<List<string?>> source)
        {
            var stream = new MemoryStream();
            using var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);

            WorkbookPart workbookPart1 = document.AddWorkbookPart();
            Workbook workbook1 = new();
            Sheets sheets1 = new();
            Sheet sheet1 = new() { Name = title, SheetId = (UInt32Value)1U, Id = "rId1" };
            sheets1.Append(sheet1);
            workbook1.Append(sheets1);
            workbookPart1.Workbook = workbook1;

            WorksheetPart worksheetPart1 = workbookPart1.AddNewPart<WorksheetPart>("rId1");
            Worksheet worksheet1 = new();

            SheetData sheetData1 = new();

            Row headRow = new();
            foreach (var name in header)
            {
                Cell cell = new()
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(name)
                };

                headRow.Append(cell);
            }
            sheetData1.Append(headRow);

            foreach (var row in source)
            {
                Row workRow = new();

                foreach (var item in row)
                {
                    Cell cell = new()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(item)
                    };

                    workRow.Append(cell);
                }

                sheetData1.Append(workRow);
            }

            worksheet1.Append(sheetData1);
            worksheetPart1.Worksheet = worksheet1;

            return stream;
        }
    }
}
