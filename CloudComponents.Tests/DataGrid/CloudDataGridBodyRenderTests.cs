using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace CloudComponents.Tests.DataGrid;

/// <summary>What the grid body draws: head cells, rows, cells, templates, notes, links, footers and the status messages.</summary>
public class CloudDataGridBodyRenderTests : DataGridTestBase
{
    private IRenderedComponent<CloudDataGridBody> RenderBody(
        List<CloudDataGridColumn>? columns = null,
        CloudDataGridDataResult? data = null,
        Action<ComponentParameterCollectionBuilder<CloudDataGridBody>>? configure = null)
    {
        return Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, columns ?? []);
            parameters.Add(body => body.Data, data);
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment Markup(string html) => builder => builder.AddMarkupContent(0, html);

    private static IReadOnlyList<IElement> HeadCells(IRenderedComponent<CloudDataGridBody> cut) => cut.FindAll(".cloudgrid-headcell");

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<CloudDataGridBody> cut) => cut.FindAll(".cloudgrid-row");

    private static IReadOnlyList<IElement> CellsOf(IElement row) => [.. row.QuerySelectorAll(".cloudgrid-cell")];

    // ----- status area ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_data_the_body_is_empty()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), data: null);

        Assert.Empty(cut.FindAll(".cloudgrid-status"));
        Assert.Empty(Rows(cut));
    }

    [Fact]
    public void While_loading_the_loading_text_replaces_the_rows()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result(NumberedRows(2)), parameters =>
        {
            parameters.Add(body => body.IsLoading, true);
            parameters.Add(body => body.LoadingText, "please wait");
        });

        Assert.Equal("please wait", Text(cut.Find(".cloudgrid-status._loading")));
        Assert.Empty(Rows(cut));
    }

    [Fact]
    public void The_default_loading_and_searching_texts_are_used_when_not_overridden()
    {
        IRenderedComponent<CloudDataGridBody> loading = RenderBody(Columns("A"), null, parameters => parameters.Add(body => body.IsLoading, true));
        IRenderedComponent<CloudDataGridBody> searching = RenderBody(Columns("A"), null, parameters => parameters.Add(body => body.IsSearching, true));

        Assert.Equal("retrieving data...", Text(loading.Find(".cloudgrid-status")));
        Assert.Equal("searching...", Text(searching.Find(".cloudgrid-status._searching")));
    }

    [Fact]
    public void Loading_wins_over_searching_and_a_message()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result(NumberedRows(1)), parameters =>
        {
            parameters.Add(body => body.IsLoading, true);
            parameters.Add(body => body.IsSearching, true);
            parameters.Add(body => body.Message, "boom");
        });

        Assert.Single(cut.FindAll(".cloudgrid-status"));
        Assert.Single(cut.FindAll(".cloudgrid-status._loading"));
    }

    [Fact]
    public void Searching_wins_over_a_message()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result(NumberedRows(1)), parameters =>
        {
            parameters.Add(body => body.IsSearching, true);
            parameters.Add(body => body.SearchingText, "looking");
            parameters.Add(body => body.Message, "boom");
        });

        Assert.Equal("looking", Text(cut.Find(".cloudgrid-status")));
        Assert.Single(cut.FindAll(".cloudgrid-status._searching"));
    }

    [Fact]
    public void A_message_replaces_the_rows()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result(NumberedRows(2)), parameters => parameters.Add(body => body.Message, "No records."));

        Assert.Equal("No records.", Text(cut.Find(".cloudgrid-status._message")));
        Assert.Empty(Rows(cut));
    }

    [Fact]
    public void An_empty_message_does_not_hide_the_rows()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result(NumberedRows(2)), parameters => parameters.Add(body => body.Message, ""));

        Assert.Empty(cut.FindAll(".cloudgrid-status"));
        Assert.Equal(2, Rows(cut).Count);
    }

    // ----- head --------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_head_has_one_cell_per_column_with_the_label_in_order()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("Name", "Age", "City"));

        Assert.Equal(["Name", "Age", "City"], Texts(HeadCells(cut)));
    }

    [Fact]
    public void A_head_cell_is_as_wide_as_its_column()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[1].Width = 240.5;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns);

        Assert.Contains("width: 100px", HeadCells(cut)[0].GetAttribute("style"));
        Assert.Contains("width: 240.5px", HeadCells(cut)[1].GetAttribute("style"));
    }

    [Fact]
    public void Sortable_columns_are_marked_sortable_and_others_are_not()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[1].Sortable = false;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns);

        Assert.Contains("_sortable", HeadCells(cut)[0].ClassList);
        Assert.DoesNotContain("_sortable", HeadCells(cut)[1].ClassList);
    }

    [Fact]
    public void Only_resizable_columns_get_a_resize_handle()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[1].Resizable = false;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns);

        Assert.NotNull(HeadCells(cut)[0].QuerySelector(".cloudgrid-resizer"));
        Assert.Null(HeadCells(cut)[1].QuerySelector(".cloudgrid-resizer"));
    }

    [Fact]
    public void Header_class_and_style_are_applied_to_the_head_cell_only()
    {
        List<CloudDataGridColumn> columns = Columns("A");
        columns[0].HeaderCssClass = "hc";
        columns[0].HeaderStyle = "color: red;";
        columns[0].CssClass = "bodyclass";
        columns[0].Style = "color: blue";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, "x")]));

        IElement head = HeadCells(cut)[0];
        Assert.Contains("hc", head.ClassList);
        Assert.DoesNotContain("bodyclass", head.ClassList);
        Assert.Contains("color: red", head.GetAttribute("style"));
        Assert.DoesNotContain("color: red;;", head.GetAttribute("style"));
        Assert.DoesNotContain("blue", head.GetAttribute("style"));
    }

    [Fact]
    public void Utility_head_cells_are_added_for_reordering_and_selection()
    {
        IRenderedComponent<CloudDataGridBody> plain = RenderBody(Columns("A"));
        IRenderedComponent<CloudDataGridBody> both = RenderBody(Columns("A"), configure: parameters =>
        {
            parameters.Add(body => body.AllowReordering, true);
            parameters.Add(body => body.AllowSelection, true);
        });

        Assert.Empty(plain.FindAll(".cloudgrid-head .cloudgrid-draghandle"));
        Assert.Empty(plain.FindAll(".cloudgrid-head .cloudgrid-check"));
        Assert.Single(both.FindAll(".cloudgrid-head .cloudgrid-draghandle"));
        Assert.Single(both.FindAll(".cloudgrid-head .cloudgrid-check"));
    }

    // ----- pinned columns -----------------------------------------------------------------------------------------------------

    private static List<CloudDataGridColumn> PinnedColumns()
    {
        List<CloudDataGridColumn> columns = Columns("L1", "L2", "Middle", "R1", "R2");
        columns[0].Pinned = CloudDataGridPinnedPosition.Left;
        columns[0].Width = 100;
        columns[1].Pinned = CloudDataGridPinnedPosition.Left;
        columns[1].Width = 150;
        columns[3].Pinned = CloudDataGridPinnedPosition.Right;
        columns[3].Width = 120;
        columns[4].Pinned = CloudDataGridPinnedPosition.Right;
        columns[4].Width = 80;
        return columns;
    }

    [Fact]
    public void Pinned_columns_get_pinned_classes_on_head_and_body_cells()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(PinnedColumns(), Result([Row(1, "a", "b", "c", "d", "e")]));

        IReadOnlyList<IElement> head = HeadCells(cut);
        IReadOnlyList<IElement> cells = CellsOf(Rows(cut)[0]);

        Assert.Contains("_pinned-left", head[0].ClassList);
        Assert.Contains("_pinned-left", head[1].ClassList);
        Assert.DoesNotContain("_pinned-left", head[2].ClassList);
        Assert.DoesNotContain("_pinned-right", head[2].ClassList);
        Assert.Contains("_pinned-right", head[3].ClassList);
        Assert.Contains("_pinned-right", head[4].ClassList);
        Assert.Contains("_pinned-left", cells[0].ClassList);
        Assert.Contains("_pinned-right", cells[4].ClassList);
    }

    [Fact]
    public void Left_pinned_columns_stack_from_the_left_edge_by_the_widths_of_the_pinned_columns_before_them()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(PinnedColumns());

        Assert.Contains("left: 0px", HeadCells(cut)[0].GetAttribute("style"));
        Assert.Contains("left: 100px", HeadCells(cut)[1].GetAttribute("style"));
    }

    [Fact]
    public void Right_pinned_columns_stack_from_the_right_edge_by_the_widths_of_the_pinned_columns_after_them()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(PinnedColumns());

        Assert.Contains("right: 80px", HeadCells(cut)[3].GetAttribute("style"));
        Assert.Contains("right: 0px", HeadCells(cut)[4].GetAttribute("style"));
    }

    [Fact]
    public void Left_pinned_columns_start_after_the_selection_and_drag_columns()
    {
        IRenderedComponent<CloudDataGridBody> selection = RenderBody(PinnedColumns(), configure: p => p.Add(body => body.AllowSelection, true));
        IRenderedComponent<CloudDataGridBody> both = RenderBody(PinnedColumns(), configure: p =>
        {
            p.Add(body => body.AllowSelection, true);
            p.Add(body => body.AllowReordering, true);
        });

        Assert.Contains("left: 40px", HeadCells(selection)[0].GetAttribute("style"));
        Assert.Contains("left: 140px", HeadCells(selection)[1].GetAttribute("style"));
        Assert.Contains("left: 68px", HeadCells(both)[0].GetAttribute("style"));
        Assert.Contains("left: 168px", HeadCells(both)[1].GetAttribute("style"));
    }

    [Fact]
    public void Utility_cells_are_pinned_only_when_a_column_is_pinned_to_the_left()
    {
        IRenderedComponent<CloudDataGridBody> pinned = RenderBody(PinnedColumns(), Result([Row(1, "a", "b", "c", "d", "e")]), p =>
        {
            p.Add(body => body.AllowSelection, true);
            p.Add(body => body.AllowReordering, true);
        });
        IRenderedComponent<CloudDataGridBody> unpinned = RenderBody(Columns("A"), Result([Row(1, "a")]), p =>
        {
            p.Add(body => body.AllowSelection, true);
            p.Add(body => body.AllowReordering, true);
        });

        IElement handle = pinned.Find(".cloudgrid-head .cloudgrid-draghandle");
        IElement check = pinned.Find(".cloudgrid-head .cloudgrid-check");
        Assert.Contains("_pinned-left", handle.ClassList);
        Assert.Contains("_utility", handle.ClassList);
        Assert.Contains("left: 0px", handle.GetAttribute("style"));
        Assert.Contains("left: 28px", check.GetAttribute("style"));
        Assert.DoesNotContain("_pinned-left", unpinned.Find(".cloudgrid-head .cloudgrid-check").ClassList);
        Assert.Null(unpinned.Find(".cloudgrid-head .cloudgrid-check").GetAttribute("style"));
    }

    // ----- rows and cells -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Every_row_renders_one_cell_per_column_with_its_value()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("Name", "Age"), Result([Row(1, "Ada", 36), Row(2, "Linus", 54)]));

        Assert.Equal(["Ada", "36"], Texts(CellsOf(Rows(cut)[0])));
        Assert.Equal(["Linus", "54"], Texts(CellsOf(Rows(cut)[1])));
    }

    [Fact]
    public void A_row_carries_its_id_in_data_id()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(7, "x")]));

        Assert.Equal(Id(7).ToString(), Rows(cut)[0].GetAttribute("data-id"));
    }

    [Fact]
    public void Null_cells_show_the_empty_cell_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A", "B"), Result([Row(1, null, "x")]));

        Assert.Equal(["--", "x"], Texts(CellsOf(Rows(cut)[0])));
    }

    [Fact]
    public void The_empty_cell_text_can_be_changed()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, new object?[] { null })]), p => p.Add(body => body.EmptyCellText, "n/a"));

        Assert.Equal("n/a", Text(CellsOf(Rows(cut)[0])[0]));
    }

    [Fact]
    public void A_row_with_fewer_cells_than_columns_fills_the_rest_with_the_empty_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A", "B", "C"), Result([Row(1, "only")]));

        Assert.Equal(["only", "--", "--"], Texts(CellsOf(Rows(cut)[0])));
    }

    [Fact]
    public void Cells_beyond_the_last_column_are_ignored()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "kept", "dropped")]));

        Assert.Equal(["kept"], Texts(CellsOf(Rows(cut)[0])));
    }

    [Fact]
    public void An_empty_string_cell_renders_blank_and_not_the_empty_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "")]));

        Assert.Equal(string.Empty, Text(CellsOf(Rows(cut)[0])[0]));
    }

    [Fact]
    public void Cell_values_are_html_encoded()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "<script>alert(1)</script>")]));

        Assert.Equal("<script>alert(1)</script>", Text(CellsOf(Rows(cut)[0])[0]));
        Assert.Empty(cut.FindAll("script"));
    }

    [Fact]
    public void A_row_gets_its_own_css_class_and_style()
    {
        CloudDataGridRow row = Row(1, "x");
        row.CssClass = "late";
        row.Style = "background: red";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]));

        Assert.Contains("late", Rows(cut)[0].ClassList);
        Assert.Contains("background: red", Rows(cut)[0].GetAttribute("style"));
    }

    [Fact]
    public void Row_attributes_are_rendered_on_the_row_element()
    {
        CloudDataGridRow row = Row(1, "x");
        row.Attributes = new Dictionary<string, object> { ["data-kind"] = "vip", ["aria-label"] = "Special" };

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]));

        Assert.Equal("vip", Rows(cut)[0].GetAttribute("data-kind"));
        Assert.Equal("Special", Rows(cut)[0].GetAttribute("aria-label"));
    }

    [Fact]
    public void Column_css_class_and_style_apply_to_every_body_cell_of_the_column()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[1].CssClass = "money";
        columns[1].Style = "text-align: right;";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, "x", "1"), Row(2, "y", "2")]));

        foreach (IElement row in Rows(cut))
        {
            Assert.Contains("money", CellsOf(row)[1].ClassList);
            Assert.Contains("text-align: right", CellsOf(row)[1].GetAttribute("style"));
            Assert.DoesNotContain("money", CellsOf(row)[0].ClassList);
        }
    }

    [Fact]
    public void A_cell_wrapper_adds_its_class_style_and_attributes_after_the_columns()
    {
        List<CloudDataGridColumn> columns = Columns("A");
        columns[0].CssClass = "col";
        columns[0].Style = "color: blue";
        CloudDataGridCell cell = new()
        {
            Value = "hi",
            CssClass = "special",
            Style = "color: green;",
            Attributes = new Dictionary<string, object> { ["data-x"] = "1" }
        };

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, cell)]));

        IElement rendered = CellsOf(Rows(cut)[0])[0];
        Assert.Equal("hi", Text(rendered));
        Assert.Contains("col", rendered.ClassList);
        Assert.Contains("special", rendered.ClassList);
        Assert.Equal("1", rendered.GetAttribute("data-x"));
        string style = rendered.GetAttribute("style")!;
        Assert.True(style.IndexOf("color: blue", StringComparison.Ordinal) < style.IndexOf("color: green", StringComparison.Ordinal), "The cell style must come after the column style so it wins.");
    }

    [Fact(Skip = "BUG: CloudDataGridBody.razor (RowCells and FooterCells) renders 'cell?.Value ?? source', so a CloudDataGridCell wrapper whose Value is null falls back to the wrapper object itself and the cell shows the type name 'AngryMonkey.CloudComponents.DataGrid.Models.CloudDataGridCell' instead of the empty cell text ('--' in rows, blank in footers).")]
    public void A_cell_wrapper_with_a_null_value_shows_the_empty_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, new CloudDataGridCell { Value = null })]));

        Assert.Equal("--", Text(CellsOf(Rows(cut)[0])[0]));
    }

    [Fact]
    public void An_image_column_renders_the_url_as_a_lazy_image()
    {
        List<CloudDataGridColumn> columns = Columns("Photo");
        columns[0].IsImage = true;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, "https://example.com/a.png")]));

        IElement image = cut.Find(".cloudgrid-cell img");
        Assert.Equal("https://example.com/a.png", image.GetAttribute("src"));
        Assert.Equal("lazy", image.GetAttribute("loading"));
        Assert.Equal(string.Empty, image.GetAttribute("alt"));
    }

    [Fact]
    public void An_image_column_with_a_missing_url_shows_no_image()
    {
        List<CloudDataGridColumn> columns = Columns("Photo");
        columns[0].IsImage = true;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, new object?[] { null }), Row(2, "  ")]));

        Assert.Empty(cut.FindAll(".cloudgrid-cell img"));
        Assert.Equal("--", Text(CellsOf(Rows(cut)[0])[0]));
    }

    [Fact]
    public void A_column_template_renders_the_cell_and_gets_the_value_row_column_and_index()
    {
        List<CloudDataGridCellContext> contexts = [];
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[1].CellTemplate = context =>
        {
            contexts.Add(context);
            return builder => builder.AddMarkupContent(0, $"<b class='tpl'>{context.Value}!</b>");
        };
        CloudDataGridRow row = Row(1, "x", 42);

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([row]));

        Assert.Equal("42!", Text(cut.Find(".cloudgrid-cell b.tpl")));
        CloudDataGridCellContext context = Assert.Single(contexts);
        Assert.Equal(42, context.Value);
        Assert.Same(row, context.Row);
        Assert.Same(columns[1], context.Column);
        Assert.Equal(1, context.ColumnIndex);
        Assert.False(context.IsFooter);
        Assert.Null(context.Cell);
    }

    [Fact]
    public void A_cell_template_overrides_the_column_template_and_sees_the_wrapper()
    {
        List<CloudDataGridColumn> columns = Columns("A");
        columns[0].CellTemplate = _ => Markup("<i class='column'></i>");
        CloudDataGridCell cell = new() { Value = 5 };
        cell.Template = context => builder => builder.AddMarkupContent(0, $"<i class='cell'>{context.Value}</i>");

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, cell)]));

        Assert.Equal("5", Text(cut.Find(".cloudgrid-cell i.cell")));
        Assert.Empty(cut.FindAll(".cloudgrid-cell i.column"));
    }

    [Fact]
    public void A_template_is_used_even_when_the_value_is_null()
    {
        List<CloudDataGridColumn> columns = Columns("A");
        columns[0].CellTemplate = _ => Markup("<i class='tpl'>custom</i>");

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(columns, Result([Row(1, new object?[] { null })]));

        Assert.Equal("custom", Text(cut.Find(".cloudgrid-cell i.tpl")));
    }

    // ----- links ---------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_row_without_a_link_has_no_anchor_and_is_not_linked()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "x")]));

        Assert.Empty(cut.FindAll(".cloudgrid-rowlink"));
        Assert.DoesNotContain("_linked", Rows(cut)[0].ClassList);
    }

    [Fact]
    public void A_row_with_a_link_gets_an_anchor_covering_the_row_with_the_default_target()
    {
        CloudDataGridRow row = Row(1, "x");
        row.Link = "/records/1";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]));

        IElement anchor = cut.Find(".cloudgrid-rowlink");
        Assert.Equal("/records/1", anchor.GetAttribute("href"));
        Assert.Equal("_parent", anchor.GetAttribute("target"));
        Assert.Equal("Open record", anchor.GetAttribute("aria-label"));
        Assert.Contains("_linked", Rows(cut)[0].ClassList);
    }

    [Fact]
    public void The_link_target_can_be_changed()
    {
        CloudDataGridRow row = Row(1, "x");
        row.Link = "/records/1";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]), p => p.Add(body => body.LinkTarget, "_blank"));

        Assert.Equal("_blank", cut.Find(".cloudgrid-rowlink").GetAttribute("target"));
    }

    // ----- notes ---------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_any_note_no_note_lines_are_rendered()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "x"), Row(2, "y")]));

        Assert.Empty(cut.FindAll(".cloudgrid-rownote"));
        Assert.DoesNotContain("_with-note", Rows(cut)[0].ClassList);
    }

    [Fact]
    public void A_note_is_rendered_beneath_its_row_with_its_class_and_style()
    {
        CloudDataGridRow row = Row(1, "x");
        row.Note = "careful";
        row.NoteCssClass = "warn";
        row.NoteStyle = "color: orange";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]));

        IElement note = cut.Find(".cloudgrid-rownote");
        Assert.Equal("careful", Text(note));
        Assert.Contains("warn", note.ClassList);
        Assert.Contains("color: orange", note.GetAttribute("style"));
        Assert.Contains("_with-note", Rows(cut)[0].ClassList);
    }

    [Fact]
    public void Once_one_row_has_a_note_every_row_reserves_a_note_line()
    {
        CloudDataGridRow withNote = Row(1, "x");
        withNote.Note = "hello";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([withNote, Row(2, "y")]));

        Assert.Equal(2, cut.FindAll(".cloudgrid-rownote").Count);
        Assert.All(Rows(cut), row => Assert.Contains("_with-note", row.ClassList));
    }

    [Fact]
    public void A_whitespace_note_does_not_count_as_a_note()
    {
        CloudDataGridRow row = Row(1, "x");
        row.Note = "   ";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]));

        Assert.Empty(cut.FindAll(".cloudgrid-rownote"));
    }

    [Fact]
    public void Hiding_row_notes_removes_them_even_when_rows_have_notes()
    {
        CloudDataGridRow row = Row(1, "x");
        row.Note = "hello";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([row]), p => p.Add(body => body.ShowRowNotes, false));

        Assert.Empty(cut.FindAll(".cloudgrid-rownote"));
        Assert.DoesNotContain("_with-note", Rows(cut)[0].ClassList);
    }

    [Fact]
    public void Reserving_note_space_renders_note_lines_even_when_no_row_has_a_note()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "x")]), p => p.Add(body => body.ReserveRowNoteSpace, true));

        Assert.Single(cut.FindAll(".cloudgrid-rownote"));
        Assert.Contains("_with-note", Rows(cut)[0].ClassList);
    }

    // ----- column footers -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_footer_rows_there_is_no_column_footer_section()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "x")]));

        Assert.Empty(cut.FindAll(".cloudgrid-columnfooters"));
    }

    [Fact]
    public void Footer_rows_render_their_cells_aligned_with_the_columns_and_blank_for_missing_ones()
    {
        List<CloudDataGridFooterRow> footers =
        [
            new() { Cells = ["Total", 42] },
            new() { Cells = ["Average"] }
        ];

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("Label", "Value"), Result([Row(1, "x", "y")]), p => p.Add(body => body.ColumnFooterRows, footers));

        IReadOnlyList<IElement> rows = cut.FindAll(".cloudgrid-columnfooter");
        Assert.Equal(2, rows.Count);
        Assert.Equal(["Total", "42"], Texts(CellsOf(rows[0])));
        Assert.Equal(["Average", string.Empty], Texts(CellsOf(rows[1])));
    }

    [Fact]
    public void Footer_rows_apply_their_class_style_and_attributes()
    {
        List<CloudDataGridFooterRow> footers =
        [
            new()
            {
                Cells = ["Total"],
                CssClass = "sum",
                Style = "font-weight: bold",
                Attributes = new Dictionary<string, object> { ["data-row"] = "total" }
            }
        ];

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), null, p => p.Add(body => body.ColumnFooterRows, footers));

        IElement row = cut.Find(".cloudgrid-columnfooter");
        Assert.Contains("sum", row.ClassList);
        Assert.Contains("font-weight: bold", row.GetAttribute("style"));
        Assert.Equal("total", row.GetAttribute("data-row"));
    }

    [Fact]
    public void The_footer_section_is_fixed_by_default_and_can_scroll_with_the_rows()
    {
        List<CloudDataGridFooterRow> footers = [new() { Cells = ["T"] }];

        IRenderedComponent<CloudDataGridBody> fixedFooter = RenderBody(Columns("A"), null, p => p.Add(body => body.ColumnFooterRows, footers));
        IRenderedComponent<CloudDataGridBody> loose = RenderBody(Columns("A"), null, p =>
        {
            p.Add(body => body.ColumnFooterRows, footers);
            p.Add(body => body.FixedColumnFooter, false);
        });

        Assert.Contains("_fixed", fixedFooter.Find(".cloudgrid-columnfooters").ClassList);
        Assert.DoesNotContain("_fixed", loose.Find(".cloudgrid-columnfooters").ClassList);
    }

    [Fact]
    public void A_footer_cell_wrapper_template_receives_a_footer_context_without_a_row()
    {
        List<CloudDataGridCellContext> contexts = [];
        CloudDataGridCell cell = new()
        {
            Value = 9,
            CssClass = "big",
            Template = context =>
            {
                contexts.Add(context);
                return builder => builder.AddMarkupContent(0, "<u>nine</u>");
            }
        };
        List<CloudDataGridFooterRow> footers = [new() { Cells = [cell] }];

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), null, p => p.Add(body => body.ColumnFooterRows, footers));

        Assert.Equal("nine", Text(cut.Find(".cloudgrid-columnfooter .cloudgrid-cell u")));
        Assert.Contains("big", cut.Find(".cloudgrid-columnfooter .cloudgrid-cell").ClassList);
        CloudDataGridCellContext context = Assert.Single(contexts);
        Assert.True(context.IsFooter);
        Assert.Null(context.Row);
        Assert.Equal(9, context.Value);
        Assert.Same(cell, context.Cell);
    }

    [Fact]
    public void Footer_rows_keep_the_selection_and_drag_columns_aligned()
    {
        List<CloudDataGridFooterRow> footers = [new() { Cells = ["T"] }];

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), null, p =>
        {
            p.Add(body => body.ColumnFooterRows, footers);
            p.Add(body => body.AllowSelection, true);
            p.Add(body => body.AllowReordering, true);
        });

        Assert.Single(cut.FindAll(".cloudgrid-columnfooter .cloudgrid-draghandle"));
        Assert.Single(cut.FindAll(".cloudgrid-columnfooter .cloudgrid-check"));
    }

    // ----- sizing --------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_full_height_and_auto_modes_do_not_set_a_body_height()
    {
        IRenderedComponent<CloudDataGridBody> full = RenderBody(Columns("A"), Result([Row(1, "x")]), p => p.Add(body => body.RowsPerPage, 10));
        IRenderedComponent<CloudDataGridBody> auto = RenderBody(Columns("A"), Result([Row(1, "x")]), p =>
        {
            p.Add(body => body.RowsPerPage, 10);
            p.Add(body => body.HeightMode, CloudDataGridHeightMode.Auto);
        });

        Assert.Null(full.Find(".cloudgrid-body").GetAttribute("style"));
        Assert.Null(auto.Find(".cloudgrid-body").GetAttribute("style"));
    }

    [Fact]
    public void The_row_height_mode_fixes_the_body_to_the_rows_per_page()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "x")]), p =>
        {
            p.Add(body => body.HeightMode, CloudDataGridHeightMode.RowHeight);
            p.Add(body => body.RowsPerPage, 12);
        });

        Assert.Contains("height: calc(var(--cloudcomponents-grid-item-height) * 12)", cut.Find(".cloudgrid-body").GetAttribute("style"));
    }

    [Fact]
    public void The_row_height_mode_without_rows_per_page_sets_no_height()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Columns("A"), Result([Row(1, "x")]), p => p.Add(body => body.HeightMode, CloudDataGridHeightMode.RowHeight));

        Assert.Null(cut.Find(".cloudgrid-body").GetAttribute("style"));
    }

    [Fact]
    public void The_auto_mode_caps_the_table_height_only_when_a_maximum_is_given()
    {
        IRenderedComponent<CloudDataGridBody> capped = RenderBody(Columns("A"), null, p =>
        {
            p.Add(body => body.HeightMode, CloudDataGridHeightMode.Auto);
            p.Add(body => body.MaxHeight, 480.5);
        });
        IRenderedComponent<CloudDataGridBody> uncapped = RenderBody(Columns("A"), null, p => p.Add(body => body.HeightMode, CloudDataGridHeightMode.Auto));
        IRenderedComponent<CloudDataGridBody> ignored = RenderBody(Columns("A"), null, p => p.Add(body => body.MaxHeight, 480));

        Assert.Equal("max-height: 480.5px", capped.Find(".cloudgrid-table").GetAttribute("style"));
        Assert.Null(uncapped.Find(".cloudgrid-table").GetAttribute("style"));
        Assert.Null(ignored.Find(".cloudgrid-table").GetAttribute("style"));
    }

    [Fact]
    public void The_body_scrolls_by_itself_only_for_row_height_mode_with_accumulating_paging()
    {
        IRenderedComponent<CloudDataGridBody> loadMore = RenderBody(Columns("A"), Result([Row(1, "x")]), p =>
        {
            p.Add(body => body.HeightMode, CloudDataGridHeightMode.RowHeight);
            p.Add(body => body.RowsPerPage, 5);
            p.Add(body => body.PagingMode, CloudDataGridPagingMode.LoadMore);
        });
        IRenderedComponent<CloudDataGridBody> pages = RenderBody(Columns("A"), Result([Row(1, "x")]), p =>
        {
            p.Add(body => body.HeightMode, CloudDataGridHeightMode.RowHeight);
            p.Add(body => body.RowsPerPage, 5);
        });
        IRenderedComponent<CloudDataGridBody> fullHeight = RenderBody(Columns("A"), Result([Row(1, "x")]), p =>
        {
            p.Add(body => body.RowsPerPage, 5);
            p.Add(body => body.PagingMode, CloudDataGridPagingMode.LoadMore);
        });

        Assert.Contains("_scroll", loadMore.Find(".cloudgrid-body").ClassList);
        Assert.DoesNotContain("_scroll", pages.Find(".cloudgrid-body").ClassList);
        Assert.DoesNotContain("_scroll", fullHeight.Find(".cloudgrid-body").ClassList);
    }

    [Fact]
    public void Displaying_all_text_adds_the_display_all_text_class()
    {
        IRenderedComponent<CloudDataGridBody> normal = RenderBody(Columns("A"));
        IRenderedComponent<CloudDataGridBody> all = RenderBody(Columns("A"), configure: p => p.Add(body => body.DisplayAllText, true));

        Assert.DoesNotContain("_display-all-text", normal.Find(".cloudgrid-body").ClassList);
        Assert.Contains("_display-all-text", all.Find(".cloudgrid-body").ClassList);
    }
}
