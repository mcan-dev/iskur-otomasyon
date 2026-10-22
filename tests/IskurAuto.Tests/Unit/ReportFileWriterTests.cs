using System.IO.Compression;
using System.Text;
using IskurAuto.Service.Reports;
using Xunit;

namespace IskurAuto.Tests.Unit;

public class ReportFileWriterTests
{
    private static ReportTable Table(params object?[][] rows) => new()
    {
        Title = "Deneme",
        Columns = new[] { "Ad", "Saat" },
        Rows = rows
    };

    private static string Csv(ReportTable table)
    {
        var bytes = ReportFileWriter.ToCsv(table);
        return Encoding.UTF8.GetString(bytes).TrimStart('﻿');
    }

    private static string ReadEntry(byte[] xlsx, string path)
    {
        using var zip = new ZipArchive(new MemoryStream(xlsx), ZipArchiveMode.Read);
        using var reader = new StreamReader(zip.GetEntry(path)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void Csv_StartsWithBom_AndUsesSemicolons()
    {
        var bytes = ReportFileWriter.ToCsv(Table(new object?[] { "Ayşe", 8m }));

        Assert.True(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Equal("Ad;Saat\r\nAyşe;8\r\n".Replace("\r\n", Environment.NewLine), Csv(Table(new object?[] { "Ayşe", 8m })));
    }

    [Fact]
    public void Csv_WritesDecimalsWithCommaAndDatesTurkishStyle()
    {
        var text = Csv(Table(new object?[] { new DateOnly(2026, 10, 5), 12.5m }));
        Assert.Contains("05.10.2026;12,5", text);
    }

    [Fact]
    public void Csv_QuotesFieldsThatContainSeparatorsQuotesOrNewlines()
    {
        var text = Csv(Table(new object?[] { "a;b", 1 }, new object?[] { "ona \"dedi\"", 2 }));

        Assert.Contains("\"a;b\";1", text);
        Assert.Contains("\"ona \"\"dedi\"\"\";2", text);
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+1+1")]
    [InlineData("@SUM(A1)")]
    [InlineData("-cmd")]
    public void Csv_NeutralisesFormulaInjection(string value)
    {
        var text = Csv(Table(new object?[] { value, 1 }));
        Assert.Contains("'" + value, text);
    }

    [Fact]
    public void Csv_KeepsNegativeNumbersAsNumbers()
    {
        Assert.Contains("-2,5", Csv(Table(new object?[] { "x", -2.5m })));
        Assert.DoesNotContain("'-2,5", Csv(Table(new object?[] { "x", -2.5m })));
    }

    [Fact]
    public void Xlsx_IsAZipWithTheExpectedParts()
    {
        var bytes = ReportFileWriter.ToXlsx(Table(new object?[] { "Ayşe", 8m }));

        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.FullName).ToArray();
        foreach (var part in new[] { "[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml" })
            Assert.True(names.Contains(part), $"{part} eksik");
    }

    [Fact]
    public void Xlsx_WritesHeaderTextAndNumericCells()
    {
        var sheet = ReadEntry(ReportFileWriter.ToXlsx(Table(new object?[] { "Ayşe", 8.5m }, new object?[] { "Veli", 3 })),
            "xl/worksheets/sheet1.xml");

        Assert.Contains("<t>Ad</t>", sheet);
        Assert.Contains("<t>Ayşe</t>", sheet);
        Assert.Contains("<c r=\"B2\"><v>8.5</v></c>", sheet);     // sayılar sayı olarak, metin değil
        Assert.Contains("<c r=\"B3\"><v>3</v></c>", sheet);
        Assert.Contains("r=\"A1\" s=\"1\"", sheet);               // başlık hücresi kalın stil
    }

    [Fact]
    public void Xlsx_EscapesMarkupAndDropsInvalidCharacters()
    {
        var sheet = ReadEntry(ReportFileWriter.ToXlsx(Table(new object?[] { "<a&b>\u0001", 1 })), "xl/worksheets/sheet1.xml");

        Assert.Contains("&lt;a&amp;b&gt;", sheet);
        Assert.DoesNotContain("\u0001", sheet);
    }

    [Fact]
    public void Xlsx_SheetNameIsCleanedAndShortened()
    {
        var table = new ReportTable { Title = "Aylık: çalışma/raporu [Ekim] " + new string('x', 40), Columns = new[] { "A" }, Rows = Array.Empty<IReadOnlyList<object?>>() };

        var workbook = ReadEntry(ReportFileWriter.ToXlsx(table), "xl/workbook.xml");
        var name = workbook.Split("name=\"")[1].Split('"')[0];

        Assert.True(name.Length <= 31);
        Assert.False(name.IndexOfAny(new[] { ':', '/', '[', ']' }) >= 0);
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ColumnNames_FollowTheSpreadsheetConvention(int index, string expected)
        => Assert.Equal(expected, ReportFileWriter.ColumnName(index));
}
