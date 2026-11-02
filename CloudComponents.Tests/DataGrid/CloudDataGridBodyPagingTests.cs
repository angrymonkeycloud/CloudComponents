using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>The load-more button, its pending and exhausted states, and the virtualised infinite-scroll mode.</summary>
public class CloudDataGridBodyPagingTests : DataGridTestBase
{
    private IRenderedComponent<CloudDataGridBody> RenderLoadMore(
        CloudDataGridDataResult data,
        Func<CloudDataGridPaginationType, Task>? onPage = null,
        Action<ComponentParameterCollectionBuilder<CloudDataGridBody>>? configure = null)
    {
        return Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, data);
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.LoadMore);

            if (onPage != null)
                parameters.Add(body => body.OnPageChanged, onPage);

            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<IElement> LoadMoreButtons(IRenderedComponent<CloudDataGridBody> cut) => cut.FindAll("button.cloudgrid-loadmore");

    // ----- load more --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_load_more_button_is_shown_while_fewer_rows_than_the_total_are_loaded()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), _ => Task.CompletedTask);

        Assert.Equal("load more", Text(Assert.Single(LoadMoreButtons(cut))));
    }

    [Fact]
    public void The_load_more_button_uses_the_given_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), _ => Task.CompletedTask,
            p => p.Add(body => body.LoadMoreText, "show more"));

        Assert.Equal("show more", Text(Assert.Single(LoadMoreButtons(cut))));
    }

    [Fact]
    public void The_load_more_button_is_hidden_once_every_row_is_loaded()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 3), _ => Task.CompletedTask);

        Assert.Empty(LoadMoreButtons(cut));
    }

    [Fact]
    public void There_is_no_load_more_button_in_pages_mode()
    {
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(3), total: 10));
        });

        Assert.Empty(LoadMoreButtons(cut));
    }

    [Fact]
    public void Clicking_load_more_asks_for_the_next_page_with_a_right_arrow()
    {
        List<CloudDataGridPaginationType> asked = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), type =>
        {
            asked.Add(type);
            return Task.CompletedTask;
        });

        LoadMoreButtons(cut)[0].Click();

        Assert.Equal([CloudDataGridPaginationType.RightArrow], asked);
    }

    [Fact]
    public void While_the_next_page_loads_the_button_is_disabled_and_shows_the_loading_text()
    {
        TaskCompletionSource finish = new();
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), _ => finish.Task,
            p => p.Add(body => body.LoadingText, "fetching"));

        LoadMoreButtons(cut)[0].Click();

        IElement pending = Assert.Single(LoadMoreButtons(cut));
        Assert.True(pending.HasAttribute("disabled"));
        Assert.Equal("fetching", Text(pending));

        finish.SetResult();
    }

    [Fact]
    public void Once_the_page_has_loaded_the_button_is_enabled_again_with_its_normal_text()
    {
        CloudDataGridDataResult data = Result(NumberedRows(3), total: 10);
        TaskCompletionSource finish = new();
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(data, async _ =>
        {
            await finish.Task;
            data.Rows.AddRange(NumberedRows(3, firstId: 4));
        });
        LoadMoreButtons(cut)[0].Click();

        finish.SetResult();

        cut.WaitForAssertion(() =>
        {
            IElement button = Assert.Single(LoadMoreButtons(cut));
            Assert.False(button.HasAttribute("disabled"));
            Assert.Equal("load more", Text(button));
        });
    }

    [Fact]
    public void A_second_click_while_one_is_pending_does_not_ask_again()
    {
        int asked = 0;
        TaskCompletionSource finish = new();
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), _ =>
        {
            asked++;
            return finish.Task;
        });

        LoadMoreButtons(cut)[0].Click();
        LoadMoreButtons(cut)[0].Click();
        finish.SetResult();

        Assert.Equal(1, asked);
    }

    [Fact]
    public void If_the_consumer_adds_no_rows_the_button_disappears_and_further_requests_stop()
    {
        int asked = 0;
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), _ =>
        {
            asked++;
            return Task.CompletedTask;
        });

        LoadMoreButtons(cut)[0].Click();

        cut.WaitForAssertion(() => Assert.Empty(LoadMoreButtons(cut)));
        Assert.Equal(1, asked);
    }

    [Fact]
    public void New_data_with_a_different_shape_brings_the_exhausted_button_back()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10), _ => Task.CompletedTask);
        LoadMoreButtons(cut)[0].Click();
        cut.WaitForAssertion(() => Assert.Empty(LoadMoreButtons(cut)));

        cut.Render(p => p.Add(body => body.Data, Result(NumberedRows(4), total: 12)));

        Assert.Single(LoadMoreButtons(cut));
    }

    [Fact]
    public void Clicking_load_more_without_a_page_handler_does_nothing()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderLoadMore(Result(NumberedRows(3), total: 10));

        LoadMoreButtons(cut)[0].Click();

        Assert.Single(LoadMoreButtons(cut));
    }

    [Fact]
    public void The_load_more_button_follows_the_rows_and_is_not_shown_while_loading_or_with_a_message()
    {
        IRenderedComponent<CloudDataGridBody> loading = RenderLoadMore(Result(NumberedRows(3), total: 10), _ => Task.CompletedTask,
            p => p.Add(body => body.IsLoading, true));
        IRenderedComponent<CloudDataGridBody> message = RenderLoadMore(Result(NumberedRows(3), total: 10), _ => Task.CompletedTask,
            p => p.Add(body => body.Message, "oops"));

        Assert.Empty(LoadMoreButtons(loading));
        Assert.Empty(LoadMoreButtons(message));
    }

    // ----- infinite scroll ----------------------------------------------------------------------------------------------------------

    [Fact]
    public void Infinite_scroll_marks_the_body_as_virtualized_and_has_no_load_more_button()
    {
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(3), total: 3));
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
        });

        Assert.Contains("_virtualized", cut.Find(".cloudgrid-body").ClassList);
        Assert.Empty(LoadMoreButtons(cut));
    }

    [Fact]
    public void Infinite_scroll_renders_the_loaded_rows_through_the_virtualizer()
    {
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(3), total: 3));
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
        });

        cut.WaitForAssertion(() => Assert.Equal(["Row 1", "Row 2", "Row 3"], FirstCells(cut)));
    }

    [Fact]
    public void Infinite_scroll_asks_the_consumer_for_more_rows_when_the_viewport_needs_rows_that_are_not_loaded()
    {
        CloudDataGridDataResult data = Result(NumberedRows(3), total: 8);
        List<CloudDataGridPaginationType> asked = [];

        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, data);
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
            parameters.Add(body => body.OnPageChanged, (CloudDataGridPaginationType type) =>
            {
                asked.Add(type);
                data.Rows.AddRange(NumberedRows(5, firstId: 4));
            });
        });

        cut.WaitForAssertion(() => Assert.NotEmpty(asked));
        Assert.All(asked, type => Assert.Equal(CloudDataGridPaginationType.RightArrow, type));
        cut.WaitForAssertion(() => Assert.Equal(8, FirstCells(cut).Count));
    }

    [Fact]
    public void Infinite_scroll_stops_asking_when_the_consumer_adds_no_rows()
    {
        int asked = 0;

        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(3), total: 8));
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
            parameters.Add(body => body.OnPageChanged, (CloudDataGridPaginationType _) => { asked++; });
        });

        cut.WaitForAssertion(() => Assert.Equal(["Row 1", "Row 2", "Row 3"], FirstCells(cut)));
        Thread.Sleep(200);
        Assert.Equal(1, asked);
    }

    [Fact]
    public async Task The_virtualizer_can_be_refreshed_without_error_in_every_mode()
    {
        IRenderedComponent<CloudDataGridBody> pages = Render<CloudDataGridBody>(parameters =>
            parameters.Add(body => body.Columns, Columns("Name")));
        IRenderedComponent<CloudDataGridBody> infinite = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(3)));
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
        });

        await pages.InvokeAsync(() => pages.Instance.RefreshVirtualizeAsync());
        await infinite.InvokeAsync(() => infinite.Instance.RefreshVirtualizeAsync());
    }
}
