using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace IskurAuto.Service.Reports;

/// <summary>Rapor tablosunu CSV veya Excel (.xlsx) dosyasına çevirir. Dış kütüphane gerektirmez.</summary>
public static class ReportFileWriter
{
    public const string CsvContentType = "text/csv; charset=utf-8";
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const char CsvSeparator = ';';   // Türkçe bölgesel ayarlarda Excel ondalık için virgül, alan ayırıcı için noktalı virgül kullanır

    public static byte[] ToCsv(ReportTable table)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join(CsvSeparator, table.Columns.Select(CsvCell)));
        foreach (var row in table.Rows)
            text.AppendLine(string.Join(CsvSeparator, row.Select(v => CsvCell(Format(v, decimalComma: true)))));

        var preamble = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble();
        return preamble.Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }

    public static byte[] ToXlsx(ReportTable table)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml);
            AddEntry(zip, "_rels/.rels", RootRelsXml);
            AddEntry(zip, "xl/workbook.xml", WorkbookXml(SheetName(table.Title)));
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
            AddEntry(zip, "xl/styles.xml", StylesXml);
            AddBytes(zip, "xl/worksheets/sheet1.xml", SheetXml(table));
        }
        return buffer.ToArray();
    }

    // ── CSV ──────────────────────────────────────────────────────────────────

    private static string CsvCell(string value)
    {
        // Excel'in "=" ile başlayan hücreyi formül saymasını önler (CSV enjeksiyonu).
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' && !IsNumber(value))
            value = "'" + value;

        return value.IndexOfAny(new[] { CsvSeparator, '"', '\n', '\r' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static bool IsNumber(string value)
        => decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static string Format(object? value, bool decimalComma)
    {
        var text = value switch
        {
            null => "",
            string s => s,
            bool b => b ? "Evet" : "Hayır",
            DateOnly d => d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            DateTime t => t.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
        return decimalComma && value is decimal or double or float ? text.Replace('.', ',') : text;
    }

    // ── XLSX ─────────────────────────────────────────────────────────────────

    private static byte[] SheetXml(ReportTable table)
    {
        using var buffer = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), CheckCharacters = false };
        using (var xml = XmlWriter.Create(buffer, settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            xml.WriteStartElement("sheetData");

            WriteRow(xml, 1, table.Columns.Cast<object?>().ToList(), headerStyle: true);
            for (var i = 0; i < table.Rows.Count; i++)
                WriteRow(xml, i + 2, table.Rows[i], headerStyle: false);

            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
        return buffer.ToArray();
    }

    private static void WriteRow(XmlWriter xml, int rowNumber, IReadOnlyList<object?> values, bool headerStyle)
    {
        xml.WriteStartElement("row");
        xml.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));

        for (var column = 0; column < values.Count; column++)
        {
            xml.WriteStartElement("c");
            xml.WriteAttributeString("r", ColumnName(column) + rowNumber);
            if (headerStyle) xml.WriteAttributeString("s", "1");

            var value = values[column];
            if (value is int or long or decimal or double or float)
            {
                xml.WriteElementString("v", Format(value, decimalComma: false));
            }
            else
            {
                xml.WriteAttributeString("t", "inlineStr");
                xml.WriteStartElement("is");
                xml.WriteElementString("t", Clean(Format(value, decimalComma: false)));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    /// <summary>0 → A, 25 → Z, 26 → AA.</summary>
    public static string ColumnName(int index)
    {
        var name = "";
        for (var n = index + 1; n > 0; n = (n - 1) / 26)
            name = (char)('A' + (n - 1) % 26) + name;
        return name;
    }

    private static string SheetName(string title)
    {
        var cleaned = new string(title.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "Rapor";
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    /// <summary>XML'de geçersiz denetim karakterlerini atar.</summary>
    private static string Clean(string text)
        => new(text.Where(c => c is '\t' or '\n' or '\r' || c >= ' ').ToArray());

    private static void AddEntry(ZipArchive zip, string path, string content)
        => AddBytes(zip, path, new UTF8Encoding(false).GetBytes(content));

    private static void AddBytes(ZipArchive zip, string path, byte[] bytes)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes, 0, bytes.Length);
    }

    private const string ContentTypesXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
        "</Types>";

    private const string RootRelsXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private const string WorkbookRelsXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
        "</Relationships>";

    private const string StylesXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
        "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
        "</styleSheet>";

    private static string WorkbookXml(string sheetName)
        => "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
           "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
           "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
           "<sheets><sheet name=\"" + System.Security.SecurityElement.Escape(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
}
