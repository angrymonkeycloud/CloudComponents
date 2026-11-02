using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>The footer bar: range text, page label, previous/next availability and the selected count.</summary>
public class CloudDataGridFooterTests : DataGridTestBase
{
    private IRenderedComponent<CloudDataGridFooter> RenderFooter(
        CloudDataGridDataResult? data,
        CloudDataGridPagingMode mode = CloudDataGridPagingMode.Pages,
        int rangeStart = 1,
        int rangeEnd = 3,
        bool hasPrevious = false,
        bool hasNext = false,
        int selected = 0,
        List<CloudDataGridPaginationType>? clicks = null)
    {
        return Render<CloudDataGridFooter>(parameters =>
        {
            parameters.Add(footer => footer.Data, data);
            parameters.Add(footer => footer.PagingMode, mode);
            parameters.Add(footer => footer.RangeStart, rangeStart);
            parameters.Add(footer => footer.RangeEnd, rangeEnd);
            parameters.Add(footer => footer.HasPreviousPage, hasPrevious);
            parameters.Add(footer => footer.HasNextPage, hasNext);
            parameters.Add(footer => footer.SelectedCount, selected);

            if (clicks != null)
                parameters.Add(footer => footer.OnPageChanged, (CloudDataGridPaginationType type) => clicks.Add(type));
        });
    }

    private static IElement Previous(IRenderedComponent<CloudDataGridFooter> cut) => cut.Find("button[title='Previous page']");

    private static IElement Next(IRenderedComponent<CloudDataGridFooter> cut) => cut.Find("button[title='Next page']");

    [Fact]
    public void With_rows_the_range_label_shows_start_end_and_total()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result(NumberedRows(3), total: 90), rangeStart: 31, rangeEnd: 33);

        Assert.Equal("31 - 33 out of 90", Text(cut.Find(".cloudgridfooter-info > span")));
    }

    [Fact]
    public void With_rows_the_pager_shows_the_page_number_of_the_data()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result(NumberedRows(3), page: 4));

        Assert.Equal("Page 4", Text(cut.Find(".cloudgridfooter-pager > span")));
    }

    [Fact]
    public void Without_data_the_labels_are_blank_and_the_pager_is_hidden()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(null);

        Assert.Contains("_hidden", cut.Find(".cloudgridfooter-pager").ClassList);
        Assert.Equal(" ", cut.Find(".cloudgridfooter-pager > span").TextContent);
        Assert.Equal(" ", cut.Find(".cloudgridfooter-info > span").TextContent);
    }

    [Fact]
    public void With_an_empty_page_the_labels_are_blank_and_the_pager_is_hidden()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result([], total: 0));

        Assert.Contains("_hidden", cut.Find(".cloudgridfooter-pager").ClassList);
        Assert.Equal(" ", cut.Find(".cloudgridfooter-info > span").TextContent);
    }

    [Fact]
    public void With_rows_in_pages_mode_the_pager_is_visible()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result(NumberedRows(3)));

        Assert.DoesNotContain("_hidden", cut.Find(".cloudgridfooter-pager").ClassList);
    }

    [Theory]
    [InlineData(CloudDataGridPagingMode.LoadMore)]
    [InlineData(CloudDataGridPagingMode.InfiniteScroll)]
    public void In_accumulating_modes_the_pager_is_hidden_but_the_range_is_still_shown(CloudDataGridPagingMode mode)
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result(NumberedRows(3), total: 10), mode, rangeStart: 1, rangeEnd: 3);

        Assert.Contains("_hidden", cut.Find(".cloudgridfooter-pager").ClassList);
        Assert.Equal("1 - 3 out of 10", Text(cut.Find(".cloudgridfooter-info > span")));
    }

    [Fact]
    public void Previous_is_disabled_without_a_previous_page_and_enabled_with_one()
    {
        IRenderedComponent<CloudDataGridFooter> without = RenderFooter(Result(NumberedRows(3)), hasPrevious: false);
        IRenderedComponent<CloudDataGridFooter> with = RenderFooter(Result(NumberedRows(3)), hasPrevious: true);

        Assert.True(Previous(without).HasAttribute("disabled"));
        Assert.False(Previous(with).HasAttribute("disabled"));
    }

    [Fact]
    public void Next_is_disabled_without_a_next_page_and_enabled_with_one()
    {
        IRenderedComponent<CloudDataGridFooter> without = RenderFooter(Result(NumberedRows(3)), hasNext: false);
        IRenderedComponent<CloudDataGridFooter> with = RenderFooter(Result(NumberedRows(3)), hasNext: true);

        Assert.True(Next(without).HasAttribute("disabled"));
        Assert.False(Next(with).HasAttribute("disabled"));
    }

    [Fact]
    public void Both_buttons_are_disabled_when_there_is_no_data_even_if_pages_are_flagged()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(null, hasPrevious: true, hasNext: true);

        Assert.True(Previous(cut).HasAttribute("disabled"));
        Assert.True(Next(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Clicking_previous_raises_a_left_arrow_page_change()
    {
        List<CloudDataGridPaginationType> clicks = [];
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result(NumberedRows(3)), hasPrevious: true, hasNext: true, clicks: clicks);

        Previous(cut).Click();

        Assert.Equal([CloudDataGridPaginationType.LeftArrow], clicks);
    }

    [Fact]
    public void Clicking_next_raises_a_right_arrow_page_change()
    {
        List<CloudDataGridPaginationType> clicks = [];
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(Result(NumberedRows(3)), hasPrevious: true, hasNext: true, clicks: clicks);

        Next(cut).Click();

        Assert.Equal([CloudDataGridPaginationType.RightArrow], clicks);
    }

    [Fact]
    public void The_selected_count_is_shown_only_when_something_is_selected()
    {
        IRenderedComponent<CloudDataGridFooter> none = RenderFooter(Result(NumberedRows(3)), selected: 0);
        IRenderedComponent<CloudDataGridFooter> some = RenderFooter(Result(NumberedRows(3)), selected: 4);

        Assert.Empty(none.FindAll(".cloudgridfooter-selected"));
        Assert.Equal("4 selected", Text(some.Find(".cloudgridfooter-selected")));
    }

    [Fact]
    public void The_selected_count_is_shown_even_when_no_rows_are_loaded()
    {
        IRenderedComponent<CloudDataGridFooter> cut = RenderFooter(null, selected: 2);

        Assert.Equal("2 selected", Text(cut.Find(".cloudgridfooter-selected")));
    }
}
