using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Builders and queries shared by the data grid test classes.</summary>
public abstract class DataGridTestBase : BunitContext
{
    protected DataGridTestBase()
    {
        // The icons are plain SVG, but Virtualize, FocusAsync and the export module all talk to JS.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>A stable, readable row id: 1 becomes 00000001-0000-0000-0000-000000000000.</summary>
    protected static Guid Id(int number) => new($"{number:D8}-0000-0000-0000-000000000000");

    protected static List<CloudDataGridColumn> Columns(params string[] labels) =>
        [.. labels.Select(label => new CloudDataGridColumn { Label = label })];

    protected static CloudDataGridRow Row(int id, params object?[] cells) => new() { Id = Id(id), Cells = [.. cells] };

    protected static CloudDataGridDataResult Result(IEnumerable<CloudDataGridRow> rows, int page = 1, int pageSize = 30, int? total = null)
    {
        List<CloudDataGridRow> list = [.. rows];

        return new CloudDataGridDataResult
        {
            Rows = list,
            Page = page,
            PageSize = pageSize,
            Total = total ?? list.Count
        };
    }

    /// <summary>Rows 1..count with a single "Row n" cell, ids 1..count.</summary>
    protected static List<CloudDataGridRow> NumberedRows(int count, int firstId = 1) =>
        [.. Enumerable.Range(firstId, count).Select(number => Row(number, $"Row {number}"))];

    protected static string Text(IElement element) => element.TextContent.Trim();

    protected static IReadOnlyList<string> Texts(IEnumerable<IElement> elements) => [.. elements.Select(Text)];

    /// <summary>The text of the first cell of each data row, in display order.</summary>
    protected static IReadOnlyList<string> FirstCells<TComponent>(IRenderedComponent<TComponent> cut) where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        Texts(cut.FindAll(".cloudgrid-row").Select(row => row.QuerySelector(".cloudgrid-cell")!));
}
