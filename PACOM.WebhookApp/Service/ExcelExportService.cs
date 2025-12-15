using OfficeOpenXml;
using PACOM.WebhookApp.Data;

namespace PACOM.WebhookApp.Service
{
    public class ExcelExportService
    {
        public byte[] ExportTransactionEvents(List<ActivityEvent> events)
        {
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Transaction Events");

            // Headers
            ws.Cells[1, 1].Value = "Datetime";
            ws.Cells[1, 2].Value = "Event Type";
            ws.Cells[1, 3].Value = "Reader";
            ws.Cells[1, 4].Value = "Cardholder Name";
            ws.Cells[1, 5].Value = "Jabatan/Agensi";
            ws.Cells[1, 6].Value = "Card No";

            using (var range = ws.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

            int row = 2;
            foreach (var item in events)
            {
                ws.Cells[row, 1].Value =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(item.UtcTime, DateTimeKind.Utc), tz
                    ).ToString("dd-MM-yyyy HH:mm:ss");

                ws.Cells[row, 2].Value = item.EventName?.Split('.').Last();
                ws.Cells[row, 3].Value = item.ReaderName;
                ws.Cells[row, 4].Value = item.UserName;
                ws.Cells[row, 5].Value = item.Organization;
                ws.Cells[row, 6].Value = item.CredentialNumber;

                row++;
            }

            ws.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}
