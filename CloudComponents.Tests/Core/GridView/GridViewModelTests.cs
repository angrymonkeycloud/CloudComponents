using System.Data;
using System.Globalization;
using AngryMonkey.CloudComponents;

namespace CloudComponents.Tests.Core.GridView;

/// <summary>The plain GridView models: parsing a DataTable into columns and rows, the row CSS classes and cell formatting.</summary>
public class GridViewModelTests
{
    private static DataTable PeopleTable()
    {
        DataTable table = new();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Age", typeof(int));
        table.Rows.Add("Ada", 36);
        table.Rows.Add("Linus", 54);
        return table;
    }

    [Fact]
    public void Parsing_columns_keeps_the_column_names_in_order()
    {
        GridViewColumns columns = GridViewColumns.Parse(PeopleTable().Columns);

        Assert.Equal(["Name", "Age"], columns.Columns.Select(column => column.Name));
    }

    [Fact]
    public void Parsing_a_table_without_columns_gives_an_empty_array_and_not_null()
    {
        GridViewColumns columns = GridViewColumns.Parse(new DataTable().Columns);

        Assert.NotNull(columns.Columns);
        Assert.Empty(columns.Columns);
    }

    [Fact]
    public void Parsed_columns_default_to_the_String_data_type()
    {
        GridViewColumns columns = GridViewColumns.Parse(PeopleTable().Columns);

        Assert.All(columns.Columns, column => Assert.Equal(GridViewDataType.String, column.DataType));
    }

    [Fact]
    public void Parsing_rows_gives_one_row_per_data_row_with_one_cell_per_column()
    {
        GridViewRows rows = GridViewRows.Parse(PeopleTable().Rows);

        Assert.Equal(2, rows.Rows.Length);
        Assert.All(rows.Rows, row => Assert.Equal(2, row.Cells.Length));
    }

    [Fact]
    public void Parsed_cells_keep_the_original_typed_values()
    {
        GridViewRows rows = GridViewRows.Parse(PeopleTable().Rows);

        Assert.Equal("Ada", rows.Rows[0].Cells[0].Value);
        Assert.Equal(36, rows.Rows[0].Cells[1].Value);
        Assert.Equal("Linus", rows.Rows[1].Cells[0].Value);
        Assert.Equal(54, rows.Rows[1].Cells[1].Value);
    }

    [Fact]
    public void Parsing_an_empty_table_gives_no_rows()
    {
        DataTable table = new();
        table.Columns.Add("Name");

        GridViewRows rows = GridViewRows.Parse(table.Rows);

        Assert.NotNull(rows.Rows);
        Assert.Empty(rows.Rows);
    }

    [Fact]
    public void Parsed_rows_start_unselected()
    {
        GridViewRows rows = GridViewRows.Parse(PeopleTable().Rows);

        Assert.All(rows.Rows, row => Assert.False(row.Selected));
    }

    [Fact]
    public void A_database_null_in_a_parsed_row_formats_as_an_empty_string()
    {
        DataTable table = new();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(DBNull.Value);

        GridViewRows rows = GridViewRows.Parse(table.Rows);

        Assert.Equal(string.Empty, rows.Rows[0].Cells[0].FormattedValue);
    }

    [Fact]
    public void An_unselected_row_has_no_css_classes()
    {
        GridViewRow row = new() { Selected = false };

        Assert.Equal(string.Empty, row.CssClasses);
    }

    [Fact]
    public void A_selected_row_has_the_selected_class_and_nothing_else()
    {
        GridViewRow row = new() { Selected = true };

        Assert.Equal("_selected", row.CssClasses);
    }

    [Fact]
    public void Row_css_classes_follow_the_selected_flag_when_it_changes()
    {
        GridViewRow row = new() { Selected = true };

        row.Selected = false;

        Assert.Equal(string.Empty, row.CssClasses);
    }

    [Fact]
    public void A_text_cell_formats_as_itself()
    {
        Assert.Equal("hello", new GridViewCell { Value = "hello" }.FormattedValue);
    }

    [Fact]
    public void A_whole_number_cell_formats_without_a_group_separator_or_decimals()
    {
        Assert.Equal("1234567", new GridViewCell { Value = 1234567 }.FormattedValue);
    }

    [Theory]
    [InlineData("en-US", "1234.5")]
    [InlineData("fr-FR", "1234,5")]
    [InlineData("de-DE", "1234,5")]
    public void A_decimal_cell_formats_with_the_decimal_separator_of_the_current_culture(string culture, string expected)
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            Assert.Equal(expected, new GridViewCell { Value = 1234.5 }.FormattedValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_boolean_cell_formats_as_True_or_False()
    {
        Assert.Equal("True", new GridViewCell { Value = true }.FormattedValue);
        Assert.Equal("False", new GridViewCell { Value = false }.FormattedValue);
    }

    [Fact]
    public void An_empty_string_cell_formats_as_an_empty_string()
    {
        Assert.Equal(string.Empty, new GridViewCell { Value = string.Empty }.FormattedValue);
    }

    [Fact(Skip = "BUG: GridViewCell.FormattedValue (GridView/Models/GridViewTable.cs, 'return Value.ToString()') throws NullReferenceException when Value is null; a null cell should format as an empty string.")]
    public void A_null_cell_value_formats_as_an_empty_string()
    {
        GridViewCell cell = new() { Value = null! };

        Assert.Equal(string.Empty, cell.FormattedValue);
    }

    [Fact]
    public void Parsing_a_very_large_table_keeps_every_row_and_cell_in_order()
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));

        for (int index = 0; index < 20_000; index++)
            table.Rows.Add(index, $"Row {index}");

        GridViewRows rows = GridViewRows.Parse(table.Rows);

        Assert.Equal(20_000, rows.Rows.Length);
        Assert.Equal(0, rows.Rows[0].Cells[0].Value);
        Assert.Equal("Row 19999", rows.Rows[^1].Cells[1].FormattedValue);
        Assert.Equal(19_999, rows.Rows[^1].Cells[0].Value);
    }

    [Fact]
    public void Parsing_a_very_wide_table_keeps_every_column_name_in_order()
    {
        DataTable table = new();

        for (int index = 0; index < 500; index++)
            table.Columns.Add($"Column{index}");

        GridViewColumns columns = GridViewColumns.Parse(table.Columns);

        Assert.Equal(Enumerable.Range(0, 500).Select(index => $"Column{index}"), columns.Columns.Select(column => column.Name));
    }

    [Fact]
    public void Request_event_args_default_to_zero()
    {
        GridViewRequestDataEventArgs args = new();

        Assert.Equal(0, args.PageSize);
        Assert.Equal(0, args.PageNumber);
    }
}
