using System.Globalization;
using AngryMonkey.CloudComponents.DataGrid.Models;

namespace CloudComponents.Tests.DataGrid;

/// <summary>The CSV field writer and the documented defaults of the grid's plain models.</summary>
public class DataGridModelTests
{
    // ----- CSV fields -------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_csv_field_is_always_wrapped_in_double_quotes()
    {
        Assert.Equal("\"hello\"", CloudDataGridCsv.Field("hello"));
    }

    [Fact]
    public void A_null_csv_field_is_an_empty_quoted_field()
    {
        Assert.Equal("\"\"", CloudDataGridCsv.Field(null));
    }

    [Fact]
    public void An_empty_string_csv_field_is_an_empty_quoted_field()
    {
        Assert.Equal("\"\"", CloudDataGridCsv.Field(string.Empty));
    }

    [Fact]
    public void Double_quotes_inside_a_csv_field_are_doubled()
    {
        Assert.Equal("\"say \"\"hi\"\"\"", CloudDataGridCsv.Field("say \"hi\""));
    }

    [Fact]
    public void Commas_and_line_breaks_stay_inside_the_quoted_csv_field()
    {
        Assert.Equal("\"a,b\r\nc\"", CloudDataGridCsv.Field("a,b\r\nc"));
    }

    [Fact]
    public void A_number_is_written_with_the_current_culture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("\"1,5\"", CloudDataGridCsv.Field(1.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_guid_and_a_boolean_are_written_with_their_text_form()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Assert.Equal("\"11111111-2222-3333-4444-555555555555\"", CloudDataGridCsv.Field(id));
        Assert.Equal("\"True\"", CloudDataGridCsv.Field(true));
    }

    [Theory]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("=HYPERLINK(\"http://evil\")", "\"'=HYPERLINK(\"\"http://evil\"\")\"")]
    [InlineData("@SUM(A1)", "\"'@SUM(A1)\"")]
    [InlineData("+SUM(A1)", "\"'+SUM(A1)\"")]
    [InlineData("-cmd|' /C calc'!A0", "\"'-cmd|' /C calc'!A0\"")]
    public void A_csv_field_that_a_spreadsheet_would_run_as_a_formula_gets_a_leading_apostrophe(string value, string expected)
    {
        Assert.Equal(expected, CloudDataGridCsv.Field(value));
    }

    // ----- formula neutralising ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("=", "'=")]
    [InlineData("@user", "'@user")]
    [InlineData("\tindented", "'\tindented")]
    [InlineData("\rreturn", "'\rreturn")]
    public void Text_starting_with_a_formula_character_is_prefixed(string text, string expected)
    {
        Assert.Equal(expected, CloudDataGridCsv.Neutralize(text));
    }

    [Theory]
    [InlineData("+961 70 123 456")]
    [InlineData("+1 (555) 010-9999")]
    [InlineData("-12")]
    [InlineData("-12.50")]
    [InlineData("-1,234.56")]
    [InlineData("+49")]
    public void A_leading_plus_or_minus_on_a_number_or_phone_number_is_left_alone(string text)
    {
        Assert.Equal(text, CloudDataGridCsv.Neutralize(text));
    }

    [Theory]
    [InlineData("-abc")]
    [InlineData("+A1")]
    [InlineData("-1;DROP")]
    [InlineData("+12abc")]
    [InlineData("-1\n")]
    public void A_leading_plus_or_minus_followed_by_anything_but_number_characters_is_prefixed(string text)
    {
        Assert.Equal("'" + text, CloudDataGridCsv.Neutralize(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain text")]
    [InlineData("a=b")]
    [InlineData("1+1")]
    [InlineData(" =1")]
    [InlineData("\"=1\"")]
    public void Ordinary_text_is_left_alone(string text)
    {
        Assert.Equal(text, CloudDataGridCsv.Neutralize(text));
    }

    [Fact]
    public void Only_the_first_character_decides_whether_text_is_neutralised()
    {
        Assert.Equal("total=5", CloudDataGridCsv.Neutralize("total=5"));
    }

    // ----- sort ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_sort_key_is_the_column_key_when_the_column_has_one()
    {
        CloudDataGridSort sort = new()
        {
            Column = new CloudDataGridColumn { Label = "Full name", Key = "fullName" },
            ColumnIndex = 2,
            Direction = CloudDataGridSortDirection.Descending
        };

        Assert.Equal("fullName", sort.Key);
    }

    [Fact]
    public void A_sort_key_falls_back_to_the_column_label()
    {
        CloudDataGridSort sort = new()
        {
            Column = new CloudDataGridColumn { Label = "Full name" },
            ColumnIndex = 0,
            Direction = CloudDataGridSortDirection.Ascending
        };

        Assert.Equal("Full name", sort.Key);
    }

    // ----- documented defaults -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_column_is_100_wide_sortable_resizable_and_not_pinned()
    {
        CloudDataGridColumn column = new() { Label = "x" };

        Assert.Equal(100, column.Width);
        Assert.Equal(60, column.MinWidth);
        Assert.True(column.Sortable);
        Assert.True(column.Resizable);
        Assert.False(column.IsImage);
        Assert.Equal(CloudDataGridPinnedPosition.None, column.Pinned);
        Assert.Null(column.Key);
    }

    [Fact]
    public void A_new_action_is_a_header_button_that_is_not_in_the_more_menu_nor_on_rows()
    {
        CloudDataGridAction action = new() { Key = "k" };

        Assert.Equal(CloudDataGridActionType.Button, action.Type);
        Assert.True(action.ShowOnHeader);
        Assert.False(action.ShowInMore);
        Assert.False(action.ShowOnBulkHeader);
        Assert.False(action.ShowOnRow);
        Assert.False(action.VisibleOnSelectionOnly);
        Assert.False(action.IconOnly);
    }

    [Fact]
    public void New_header_options_enable_refresh_and_export_but_not_search()
    {
        CloudDataGridHeaderOptions options = new();

        Assert.True(options.AllowRefresh);
        Assert.True(options.AllowExport);
        Assert.False(options.AllowSearch);
        Assert.Equal(300, options.SearchDebounceMilliseconds);
        Assert.Empty(options.Actions);
    }

    [Fact]
    public void A_new_data_request_asks_for_the_first_page_without_search_sort_or_append()
    {
        CloudDataGridDataRequest request = new();

        Assert.Equal(1, request.Page);
        Assert.Null(request.Search);
        Assert.Null(request.Sort);
        Assert.False(request.IsAppend);
        Assert.Equal(0, request.Total);
    }

    [Fact]
    public void A_new_data_result_is_empty_on_page_one_without_an_error()
    {
        CloudDataGridDataResult result = new();

        Assert.Empty(result.Rows);
        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.Total);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void A_new_row_has_no_cells_link_or_category()
    {
        CloudDataGridRow row = new();

        Assert.Empty(row.Cells);
        Assert.Null(row.Link);
        Assert.Null(row.CategoryKey);
        Assert.Null(row.Note);
    }

    [Fact]
    public void New_action_event_args_carry_no_record_ids_until_filled()
    {
        CloudDataGridActionEventArgs args = new() { Action = new CloudDataGridAction { Key = "k" } };

        Assert.Empty(args.RecordIds);
    }

    [Fact]
    public void A_new_footer_row_has_no_cells()
    {
        Assert.Empty(new CloudDataGridFooterRow().Cells);
    }
}
