using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    internal enum ChartKind { Column, Bar, Line }

    // One chart: a single series whose categories and values live in cells of
    // `Sheet` (the refs), with the same values repeated as a cache so any
    // viewer — not just desktop Excel — can draw it without recalculating.
    // Anchor columns/rows are 0-based cell coordinates.
    internal sealed record ChartSpec(
        string Sheet,
        string Title,
        ChartKind Kind,
        string CategoryRef,
        IReadOnlyList<string> Categories,
        string ValuesRef,
        IReadOnlyList<double?> Values,
        string SeriesName,
        string ColorHex,
        string NumberFormat,
        double? AxisMin,
        double? AxisMax,
        int FromColumn,
        int FromRow,
        int ToColumn,
        int ToRow);

    // ClosedXML has no chart API, so charts are added afterwards with the Open
    // XML SDK (which ClosedXML already depends on) — real Excel charts bound to
    // the cells, not pictures: they stay live if someone edits a number.
    internal static class ExcelChartWriter
    {
        public static byte[] AddCharts(byte[] workbook, IReadOnlyCollection<ChartSpec> charts)
        {
            if (charts.Count == 0) return workbook;

            using var stream = new MemoryStream();
            stream.Write(workbook, 0, workbook.Length);

            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook has no workbook part.");
                uint shapeId = 1;

                foreach (var group in charts.GroupBy(c => c.Sheet))
                {
                    var sheet = workbookPart.Workbook.Descendants<Sheet>().First(s => s.Name == group.Key);
                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);

                    var drawingsPart = worksheetPart.DrawingsPart ?? worksheetPart.AddNewPart<DrawingsPart>();
                    drawingsPart.WorksheetDrawing ??= new Xdr.WorksheetDrawing();
                    EnsureDrawingElement(worksheetPart, drawingsPart);

                    foreach (var spec in group)
                    {
                        var chartPart = drawingsPart.AddNewPart<ChartPart>();
                        chartPart.ChartSpace = BuildChartSpace(spec);
                        chartPart.ChartSpace.Save();

                        drawingsPart.WorksheetDrawing.Append(BuildAnchor(spec, drawingsPart.GetIdOfPart(chartPart), shapeId++));
                    }

                    drawingsPart.WorksheetDrawing.Save();
                    worksheetPart.Worksheet.Save();
                }
            }

            return stream.ToArray();
        }

        // <drawing r:id> must sit at its schema position in the worksheet —
        // before legacyDrawing/tableParts/extLst — or Excel reports the file
        // as damaged.
        private static void EnsureDrawingElement(WorksheetPart worksheetPart, DrawingsPart drawingsPart)
        {
            var worksheet = worksheetPart.Worksheet;
            if (worksheet.GetFirstChild<Drawing>() != null) return;

            var drawing = new Drawing { Id = worksheetPart.GetIdOfPart(drawingsPart) };
            OpenXmlElement? after = worksheet.ChildElements.FirstOrDefault(e =>
                e is LegacyDrawing or LegacyDrawingHeaderFooter or DrawingHeaderFooter or Picture
                  or OleObjects or Controls or WebPublishItems or TableParts or WorksheetExtensionList);

            if (after != null) worksheet.InsertBefore(drawing, after);
            else worksheet.Append(drawing);
        }

        private static Xdr.TwoCellAnchor BuildAnchor(ChartSpec spec, string chartRelId, uint shapeId) =>
            new(
                new Xdr.FromMarker(
                    new Xdr.ColumnId(spec.FromColumn.ToString(CultureInfo.InvariantCulture)),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId(spec.FromRow.ToString(CultureInfo.InvariantCulture)),
                    new Xdr.RowOffset("0")),
                new Xdr.ToMarker(
                    new Xdr.ColumnId(spec.ToColumn.ToString(CultureInfo.InvariantCulture)),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId(spec.ToRow.ToString(CultureInfo.InvariantCulture)),
                    new Xdr.RowOffset("0")),
                new Xdr.GraphicFrame(
                    new Xdr.NonVisualGraphicFrameProperties(
                        new Xdr.NonVisualDrawingProperties { Id = shapeId + 1, Name = $"Chart {shapeId}" },
                        new Xdr.NonVisualGraphicFrameDrawingProperties()),
                    new Xdr.Transform(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = 0, Cy = 0 }),
                    new A.Graphic(
                        new A.GraphicData(new C.ChartReference { Id = chartRelId })
                        {
                            Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
                        }))
                { Macro = string.Empty },
                new Xdr.ClientData())
            { EditAs = Xdr.EditAsValues.OneCell };

        private static C.ChartSpace BuildChartSpace(ChartSpec spec)
        {
            const uint categoryAxisId = 50010;
            const uint valueAxisId = 50020;

            var chartSpace = new C.ChartSpace();
            chartSpace.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            chartSpace.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            chartSpace.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            chartSpace.Append(new C.EditingLanguage { Val = "en-US" });
            chartSpace.Append(new C.RoundedCorners { Val = false });

            OpenXmlCompositeElement plot = spec.Kind == ChartKind.Line
                ? new C.LineChart(
                    new C.Grouping { Val = C.GroupingValues.Standard },
                    new C.VaryColors { Val = false },
                    LineSeries(spec),
                    DataLabels(spec.NumberFormat, C.DataLabelPositionValues.Top),
                    new C.ShowMarker { Val = true },
                    new C.AxisId { Val = categoryAxisId },
                    new C.AxisId { Val = valueAxisId })
                : new C.BarChart(
                    new C.BarDirection { Val = spec.Kind == ChartKind.Bar ? C.BarDirectionValues.Bar : C.BarDirectionValues.Column },
                    new C.BarGrouping { Val = C.BarGroupingValues.Clustered },
                    new C.VaryColors { Val = false },
                    BarSeries(spec),
                    DataLabels(spec.NumberFormat, C.DataLabelPositionValues.OutsideEnd),
                    new C.GapWidth { Val = 70 },
                    new C.AxisId { Val = categoryAxisId },
                    new C.AxisId { Val = valueAxisId });

            // A horizontal bar chart lays categories down the left side.
            var horizontal = spec.Kind == ChartKind.Bar;

            var plotArea = new C.PlotArea(
                new C.Layout(),
                plot,
                CategoryAxis(categoryAxisId, valueAxisId, horizontal ? C.AxisPositionValues.Left : C.AxisPositionValues.Bottom, horizontal),
                ValueAxis(valueAxisId, categoryAxisId, horizontal ? C.AxisPositionValues.Bottom : C.AxisPositionValues.Left, spec, horizontal));

            var chart = new C.Chart(
                Title(spec.Title),
                new C.AutoTitleDeleted { Val = false },
                plotArea,
                new C.PlotVisibleOnly { Val = true },
                // A month nobody answered is a gap, not a zero.
                new C.DisplayBlanksAs { Val = C.DisplayBlanksAsValues.Gap });

            chartSpace.Append(chart);
            return chartSpace;
        }

        private static C.BarChartSeries BarSeries(ChartSpec spec) =>
            new(
                new C.Index { Val = 0 },
                new C.Order { Val = 0 },
                SeriesText(spec.SeriesName),
                new C.ChartShapeProperties(new A.SolidFill(new A.RgbColorModelHex { Val = spec.ColorHex })),
                new C.InvertIfNegative { Val = false },
                CategoryData(spec),
                ValueData(spec));

        private static C.LineChartSeries LineSeries(ChartSpec spec) =>
            new(
                new C.Index { Val = 0 },
                new C.Order { Val = 0 },
                SeriesText(spec.SeriesName),
                new C.ChartShapeProperties(
                    new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = spec.ColorHex })) { Width = 31750 }),
                new C.Marker(
                    new C.Symbol { Val = C.MarkerStyleValues.Circle },
                    new C.Size { Val = 7 },
                    new C.ChartShapeProperties(new A.SolidFill(new A.RgbColorModelHex { Val = spec.ColorHex }))),
                CategoryData(spec),
                ValueData(spec),
                new C.Smooth { Val = false });

        private static C.SeriesText SeriesText(string name) => new(new C.NumericValue { Text = name });

        private static C.CategoryAxisData CategoryData(ChartSpec spec)
        {
            var cache = new C.StringCache(new C.PointCount { Val = (uint)spec.Categories.Count });
            for (var i = 0; i < spec.Categories.Count; i++)
                cache.Append(new C.StringPoint(new C.NumericValue { Text = spec.Categories[i] }) { Index = (uint)i });

            return new C.CategoryAxisData(new C.StringReference(new C.Formula { Text = spec.CategoryRef }, cache));
        }

        private static C.Values ValueData(ChartSpec spec)
        {
            var cache = new C.NumberingCache(
                new C.FormatCode { Text = spec.NumberFormat },
                new C.PointCount { Val = (uint)spec.Values.Count });
            for (var i = 0; i < spec.Values.Count; i++)
            {
                if (spec.Values[i] is not double v) continue; // blank point → gap
                cache.Append(new C.NumericPoint(new C.NumericValue { Text = v.ToString(CultureInfo.InvariantCulture) }) { Index = (uint)i });
            }

            return new C.Values(new C.NumberReference(new C.Formula { Text = spec.ValuesRef }, cache));
        }

        private static C.DataLabels DataLabels(string numberFormat, C.DataLabelPositionValues position) =>
            new(
                new C.NumberingFormat { FormatCode = numberFormat, SourceLinked = false },
                new C.TextProperties(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.ParagraphProperties(new A.DefaultRunProperties { FontSize = 900, Bold = true }), new A.EndParagraphRunProperties { Language = "en-US" })),
                new C.DataLabelPosition { Val = position },
                new C.ShowLegendKey { Val = false },
                new C.ShowValue { Val = true },
                new C.ShowCategoryName { Val = false },
                new C.ShowSeriesName { Val = false },
                new C.ShowPercent { Val = false },
                new C.ShowBubbleSize { Val = false });

        private static C.CategoryAxis CategoryAxis(uint id, uint crossId, C.AxisPositionValues position, bool reverse) =>
            new(
                new C.AxisId { Val = id },
                // A horizontal bar chart would otherwise list question 1 at the bottom.
                new C.Scaling(new C.Orientation { Val = reverse ? C.OrientationValues.MaxMin : C.OrientationValues.MinMax }),
                new C.Delete { Val = false },
                new C.AxisPosition { Val = position },
                new C.NumberingFormat { FormatCode = "General", SourceLinked = false },
                new C.MajorTickMark { Val = C.TickMarkValues.None },
                new C.MinorTickMark { Val = C.TickMarkValues.None },
                new C.TickLabelPosition { Val = C.TickLabelPositionValues.Low },
                new C.CrossingAxis { Val = crossId },
                new C.Crosses { Val = C.CrossesValues.AutoZero },
                new C.AutoLabeled { Val = true },
                new C.LabelAlignment { Val = C.LabelAlignmentValues.Center },
                new C.LabelOffset { Val = 100 },
                new C.NoMultiLevelLabels { Val = false });

        // `reversedCategories`: the bar chart lists categories top-down, which
        // flips the value axis to the top — crossing at the maximum puts it
        // back underneath.
        private static C.ValueAxis ValueAxis(uint id, uint crossId, C.AxisPositionValues position, ChartSpec spec, bool reversedCategories)
        {
            var scaling = new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax });
            if (spec.AxisMax is double max) scaling.Append(new C.MaxAxisValue { Val = max });
            if (spec.AxisMin is double min) scaling.Append(new C.MinAxisValue { Val = min });

            return new C.ValueAxis(
                new C.AxisId { Val = id },
                scaling,
                new C.Delete { Val = false },
                new C.AxisPosition { Val = position },
                new C.MajorGridlines(new C.ChartShapeProperties(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = "E5E7EB" })))),
                new C.NumberingFormat { FormatCode = spec.NumberFormat, SourceLinked = false },
                new C.MajorTickMark { Val = C.TickMarkValues.None },
                new C.MinorTickMark { Val = C.TickMarkValues.None },
                new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                new C.CrossingAxis { Val = crossId },
                new C.Crosses { Val = reversedCategories ? C.CrossesValues.Maximum : C.CrossesValues.AutoZero },
                new C.CrossBetween { Val = C.CrossBetweenValues.Between });
        }

        private static C.Title Title(string text) =>
            new(
                new C.ChartText(
                    new C.RichText(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(
                            new A.ParagraphProperties(new A.DefaultRunProperties { FontSize = 1200, Bold = true }),
                            new A.Run(new A.RunProperties { Language = "en-US", FontSize = 1200, Bold = true }, new A.Text(text))))),
                new C.Overlay { Val = false });

        // 'Sheet Name'!$B$5:$B$9
        public static string Ref(string sheet, int column, int firstRow, int lastRow)
        {
            var col = ColumnLetter(column);
            return $"'{sheet.Replace("'", "''")}'!${col}${firstRow}:${col}${lastRow}";
        }

        private static string ColumnLetter(int column)
        {
            var name = string.Empty;
            while (column > 0)
            {
                var m = (column - 1) % 26;
                name = (char)('A' + m) + name;
                column = (column - m) / 26;
            }
            return name;
        }
    }
}
