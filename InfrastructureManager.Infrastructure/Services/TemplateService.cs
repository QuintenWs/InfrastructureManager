using ClosedXML.Excel;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public interface ITemplateService
{
    Task<byte[]> GenerateImportTemplateAsync();
}

public class TemplateService : ITemplateService
{
    private readonly AppDbContext _context;

    // ── Kleuren — rustig zakelijk blauw/grijs ───────────────────────────────────
    private static readonly XLColor HeaderBg      = XLColor.FromHtml("#4A5A70");
    private static readonly XLColor HeaderText    = XLColor.White;
    private static readonly XLColor TitleColor    = XLColor.FromHtml("#33414F");
    private static readonly XLColor SubtleGray    = XLColor.FromHtml("#6B7280");
    private static readonly XLColor BodyGray      = XLColor.FromHtml("#374151");
    private static readonly XLColor BorderGray    = XLColor.FromHtml("#D8DCE3");
    private static readonly XLColor CustomFieldBg = XLColor.FromHtml("#6E7E93");
    private static readonly XLColor RequiredMark  = XLColor.FromHtml("#B45309");

    public TemplateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateImportTemplateAsync()
    {
        var definitions = await _context.DeviceTypeDefinitions
            .Include(d => d.Fields.OrderBy(f => f.SortOrder))
            .OrderBy(d => d.Name)
            .ToListAsync();

        var maxFields = definitions.Count > 0
            ? Math.Max(1, definitions.Max(d => d.Fields.Count))
            : 1;

        using var wb = new XLWorkbook();

        var listsSheet = AddListsSheet(wb, definitions);

        AddInstructionsSheet(wb, definitions);
        AddLocationsSheet(wb);
        AddDepartmentsSheet(wb);
        AddNetworksSheet(wb);
        AddFieldReferenceSheet(wb, definitions, maxFields);
        AddDevicesSheet(wb, definitions, listsSheet, maxFields);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Instructies ───────────────────────────────────────────────────────────

    private static void AddInstructionsSheet(IXLWorkbook wb, List<Domain.Entities.DeviceTypeDefinition> definitions)
    {
        var ws = wb.Worksheets.Add("Instructions");

        ws.Cell("A1").Value = "InfrastructureManager — Import Template";
        ws.Cell("A1").Style.Font.Bold      = true;
        ws.Cell("A1").Style.Font.FontSize  = 15;
        ws.Cell("A1").Style.Font.FontColor = TitleColor;
        ws.Cell("A1").Style.Font.FontName  = "Arial";

        var rules = new[]
        {
            "Vul de tabbladen in deze volgorde in: Locations → Departments → Networks → Devices.",
            "Rij 1 op elk tabblad is de header — niet aanpassen.",
            "Alle rijen moeten geldig zijn — één fout annuleert de volledige import.",
            "Alle apparaten staan samen op één tabblad 'Devices' — kies het type via de dropdown in kolom C.",
            "Bekijk het tabblad 'Veldoverzicht' om te zien wat Custom Field 1, 2, 3... betekenen per apparaattype.",
            "Devices 'Department Name': gebruik enkel de naam (bv. 'IT Operations').",
            "  Als twee departementen dezelfde naam delen, voeg de locatie toe: 'IT Operations – Antwerpen'.",
            "Devices 'Status': Active, Offline, Maintenance of Retired (dropdown in kolom D).",
            "Devices 'Network Name': moet overeenkomen met een netwerk op het Networks-tabblad (optioneel).",
            "Custom Field-kolommen die niet van toepassing zijn voor een bepaald type, laat je gewoon leeg.",
        };

        int r = 3;
        foreach (var rule in rules)
        {
            ws.Cell(r, 1).Value               = rule;
            ws.Cell(r, 1).Style.Font.FontName  = "Arial";
            ws.Cell(r, 1).Style.Font.FontSize  = 10;
            ws.Cell(r, 1).Style.Font.FontColor = BodyGray;
            r++;
        }

        r += 1;
        ws.Cell(r, 1).Value                = $"Er zijn {definitions.Count} apparaattype(s) beschikbaar — het volledige overzicht met velden staat op tabblad 'Veldoverzicht'.";
        ws.Cell(r, 1).Style.Font.Italic    = true;
        ws.Cell(r, 1).Style.Font.FontColor = SubtleGray;
        ws.Cell(r, 1).Style.Font.FontName  = "Arial";
        ws.Cell(r, 1).Style.Font.FontSize  = 9.5;

        ws.Column(1).Width = 100;
    }

    // ── Verborgen lijst-tabblad (dropdown-bron voor Device Type) ────────────────

    private static IXLWorksheet AddListsSheet(IXLWorkbook wb, List<Domain.Entities.DeviceTypeDefinition> definitions)
    {
        var ws = wb.Worksheets.Add("Lijsten");
        ws.Cell(1, 1).Value = "DeviceTypeNames";

        for (int i = 0; i < definitions.Count; i++)
            ws.Cell(i + 2, 1).Value = definitions[i].Name;

        // Verborgen — dit tabblad is enkel de bron voor de dropdown, niet bedoeld om in te vullen.
        ws.Visibility = XLWorksheetVisibility.Hidden;
        return ws;
    }

    // ── Locations ─────────────────────────────────────────────────────────────

    private static void AddLocationsSheet(IXLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Locations");
        AddHeader(ws,
            new[] { "Name *", "City *", "Country *", "Notes" },
            new[] { 24, 20, 20, 40 });
    }

    // ── Departments ───────────────────────────────────────────────────────────

    private static void AddDepartmentsSheet(IXLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Departments");
        AddHeader(ws,
            new[] { "Location Name *", "Name *", "Address *", "Description", "Notes" },
            new[] { 24, 30, 44, 40, 40 });
    }

    // ── Networks ──────────────────────────────────────────────────────────────

    private static void AddNetworksSheet(IXLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Networks");
        AddHeader(ws,
            new[]
            {
                "Department Name *", "Name *", "Network Address *", "Subnet Mask *",
                "CIDR *", "Gateway *", "Primary DNS *", "Secondary DNS",
                "Is DHCP Enabled *", "Is Internet Accessible *",
                "VLAN ID", "ISP Name", "Notes"
            },
            new[] { 28, 24, 22, 18, 8, 18, 18, 18, 22, 26, 10, 20, 40 });

        AddDropdown(ws, "I", "\"TRUE,FALSE\"");
        AddDropdown(ws, "J", "\"TRUE,FALSE\"");
    }

    // ── Veldoverzicht — welk Custom Field hoort bij welk apparaattype ───────────

    private static void AddFieldReferenceSheet(
        IXLWorkbook wb,
        List<Domain.Entities.DeviceTypeDefinition> definitions,
        int maxFields)
    {
        var ws = wb.Worksheets.Add("Veldoverzicht");

        var headers = new[] { "Apparaattype" }
            .Concat(Enumerable.Range(1, maxFields).Select(i => $"Custom Field {i}"))
            .ToArray();
        var widths = new[] { 26 }
            .Concat(Enumerable.Repeat(22, maxFields))
            .ToArray();

        AddHeader(ws, headers, widths);

        int row = 2;
        foreach (var def in definitions)
        {
            ws.Cell(row, 1).Value               = def.Name;
            ws.Cell(row, 1).Style.Font.Bold     = true;
            ws.Cell(row, 1).Style.Font.FontName = "Arial";
            ws.Cell(row, 1).Style.Font.FontSize = 10;

            var fields = def.Fields.OrderBy(f => f.SortOrder).ToList();
            for (int i = 0; i < fields.Count; i++)
            {
                var cell = ws.Cell(row, i + 2);
                cell.Value               = fields[i].Label + (fields[i].IsRequired ? " *" : "");
                cell.Style.Font.FontName = "Arial";
                cell.Style.Font.FontSize = 10;
                if (fields[i].IsRequired)
                    cell.Style.Font.FontColor = RequiredMark;
            }
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(1);

        var note = ws.Cell(row + 1, 1);
        note.Value                = "* = verplicht veld voor dat apparaattype";
        note.Style.Font.Italic    = true;
        note.Style.Font.FontSize  = 9;
        note.Style.Font.FontColor = SubtleGray;
        note.Style.Font.FontName  = "Arial";
    }

    // ── Devices — één centraal tabblad voor alle apparaattypes ───────────────────

    private static void AddDevicesSheet(
        IXLWorkbook wb,
        List<Domain.Entities.DeviceTypeDefinition> definitions,
        IXLWorksheet listsSheet,
        int maxFields)
    {
        var ws = wb.Worksheets.Add("Devices");

        var fixedHeaders = new[]
        {
            "Department Name *", "Device Name *", "Device Type *",
            "Status *", "Network Name", "Notes"
        };
        var fixedWidths = new[] { 28, 26, 22, 16, 24, 36 };

        var customHeaders = Enumerable.Range(1, maxFields).Select(i => $"Custom Field {i}").ToArray();
        var customWidths  = Enumerable.Repeat(20, maxFields).ToArray();

        var allHeaders = fixedHeaders.Concat(customHeaders).ToArray();
        var allWidths  = fixedWidths.Concat(customWidths).ToArray();

        AddHeader(ws, allHeaders, allWidths);

        // Custom field-kolommen krijgen een iets lichtere tint, zodat ze herkenbaar
        // zijn als "afhankelijk van het gekozen type" — zie tabblad Veldoverzicht.
        for (int i = fixedHeaders.Length + 1; i <= allHeaders.Length; i++)
            ws.Cell(1, i).Style.Fill.BackgroundColor = CustomFieldBg;

        // Subtitel rij
        ws.Row(2).Height = 18;
        var subtitle = ws.Cell(2, 1);
        subtitle.Value = "← Eén rij per apparaat. Kies het Apparaattype uit de lijst — zie tabblad 'Veldoverzicht' voor de betekenis van de Custom Field-kolommen bij dat type.";
        subtitle.Style.Font.Italic    = true;
        subtitle.Style.Font.FontSize  = 9;
        subtitle.Style.Font.FontColor = SubtleGray;
        subtitle.Style.Font.FontName  = "Arial";
        ws.Range(2, 1, 2, allHeaders.Length).Merge();
        ws.SheetView.FreezeRows(2);
        ws.SheetView.FreezeColumns(2);

        // Dropdowns
        if (definitions.Count > 0)
            AddDropdownFromRange(ws, "C", listsSheet.Range($"A2:A{definitions.Count + 1}"));

        AddDropdown(ws, "D", "\"Active,Offline,Maintenance,Retired\"");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddHeader(IXLWorksheet ws, string[] headers, int[] widths)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold                = true;
            cell.Style.Font.FontColor           = HeaderText;
            cell.Style.Font.FontName            = "Arial";
            cell.Style.Font.FontSize            = 10;
            cell.Style.Fill.BackgroundColor     = HeaderBg;
            cell.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder      = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = BorderGray;
            ws.Column(i + 1).Width              = widths.Length > i ? widths[i] : 20;
        }
        ws.Row(1).Height = 20;
        ws.SheetView.FreezeRows(1);
    }

    private static void AddDropdown(IXLWorksheet ws, string col, string formula)
    {
        // Start vanaf rij 3 op tabbladen met een subtitel (rij 2), anders rij 2
        var startRow = ws.Row(2).Cell(1).GetString().StartsWith("←") ? 3 : 2;
        var dv = ws.Range($"{col}{startRow}:{col}1048576").CreateDataValidation();
        dv.AllowedValues    = XLAllowedValues.List;
        dv.Value            = formula;
        dv.ShowErrorMessage = true;
        dv.ErrorTitle       = "Ongeldige waarde";
        dv.ErrorMessage     = "Kies een waarde uit de lijst.";
    }

    private static void AddDropdownFromRange(IXLWorksheet ws, string col, IXLRange sourceRange)
    {
        var startRow = ws.Row(2).Cell(1).GetString().StartsWith("←") ? 3 : 2;
        var dv = ws.Range($"{col}{startRow}:{col}1048576").CreateDataValidation();
        dv.List(sourceRange, true);
        dv.ShowErrorMessage = true;
        dv.ErrorTitle       = "Ongeldige waarde";
        dv.ErrorMessage     = "Kies een geldig apparaattype uit de lijst.";
    }
}