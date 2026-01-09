using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PACOM.WebhookApp.Data;

namespace PACOM.WebhookApp.Service
{
    public class PdfExportService
    {
        public byte[] ExportToPdf<T>(List<T> data, string title, string[] headers, string[] propertyNames)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text(title)
                        .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var _ in headers)
                                {
                                    columns.RelativeColumn();
                                }
                            });

                            table.Header(header =>
                            {
                                foreach (var headerText in headers)
                                {
                                    header.Cell().Element(CellStyle).Text(headerText).SemiBold();
                                }

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold())
                                        .PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            var properties = typeof(T).GetProperties();
                            foreach (var item in data)
                            {
                                foreach (var propName in propertyNames)
                                {
                                    var prop = properties.FirstOrDefault(p => p.Name == propName);
                                    if (prop != null)
                                    {
                                        var value = prop.GetValue(item);
                                        var displayValue = value is DateTime dateTime
                                            ? dateTime.ToString("dd MMM yyyy HH:mm")
                                            : value?.ToString() ?? "";

                                        table.Cell().Element(CellStyle).Text(displayValue);
                                    }
                                }

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(5);
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportTransactionEventsToPdf(List<ActivityEvent> events)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("Transaction Event Logs")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

                        column.Item().AlignCenter().Text($"Generated on: {DateTime.Now:dd MMM yyyy HH:mm:ss}")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingVertical(5).Element(container =>
                        {
                            container.Height(1).Background(Colors.Grey.Lighten1);
                        });
                    });

                    // Content
                    page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f); // Datetime
                            columns.RelativeColumn(1f);   // Event Type
                            columns.RelativeColumn(1.2f); // Reader
                            columns.RelativeColumn(1.5f); // Cardholder
                            columns.RelativeColumn(1.5f); // Organization
                            columns.RelativeColumn(0.8f); // Card No
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Date & Time");
                            header.Cell().Element(HeaderStyle).Text("Event Type");
                            header.Cell().Element(HeaderStyle).Text("Reader");
                            header.Cell().Element(HeaderStyle).Text("Cardholder");
                            header.Cell().Element(HeaderStyle).Text("Organization");
                            header.Cell().Element(HeaderStyle).Text("Card No");

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container
                                    .Background(Colors.Blue.Lighten3)
                                    .Padding(5)
                                    .DefaultTextStyle(x => x.SemiBold().FontSize(9));
                            }
                        });

                        // Data rows
                        foreach (var evt in events)
                        {
                            var localTime = TimeZoneInfo.ConvertTimeFromUtc(
                                evt.UtcTime,
                                TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time")
                            );

                            var eventName = evt.EventName?.Split('.')?.Last() ?? "Unknown";

                            table.Cell().Element(CellStyle).Text(localTime.ToString("dd MMM yyyy\nHH:mm:ss"));
                            table.Cell().Element(CellStyle).Text(eventName);
                            table.Cell().Element(CellStyle).Text(evt.ReaderName ?? "");
                            table.Cell().Element(CellStyle).Text(evt.UserName ?? "");
                            table.Cell().Element(CellStyle).Text(evt.Organization ?? "");
                            table.Cell().Element(CellStyle).Text(evt.CredentialNumber ?? "");
                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    });

                    // Footer (FIXED)
                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().PaddingVertical(5).Element(container =>
                        {
                            container.Height(1).Background(Colors.Grey.Lighten1);
                        });

                        column.Item().Row(row =>
                        {
                            // FIX 1: Use .DefaultTextStyle(...) instead of .FontSize(...)
                            row.RelativeItem().AlignLeft().DefaultTextStyle(x => x.FontSize(9)).Text(x =>
                            {
                                x.Span("Total Records: ").SemiBold();
                                x.Span(events.Count.ToString());
                            });

                            // FIX 2: Use .DefaultTextStyle(...) instead of .FontSize(...)
                            row.RelativeItem().AlignRight().DefaultTextStyle(x => x.FontSize(9)).Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
