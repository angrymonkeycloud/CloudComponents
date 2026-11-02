using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>
/// A scripted data provider: records every request and answers each one with <see cref="Respond"/>, optionally after a gate
/// the test opens, so loading states can be observed.
/// </summary>
public sealed class GridRig
{
    public List<CloudDataGridDataRequest> Requests { get; } = [];

    /// <summary>Answers a request; defaults to a single page of three rows.</summary>
    public Func<CloudDataGridDataRequest, CloudDataGridDataResult?> Respond { get; set; } = _ => new CloudDataGridDataResult
    {
        Rows = [.. Enumerable.Range(1, 3).Select(number => new CloudDataGridRow { Id = new Guid($"{number:D8}-0000-0000-0000-000000000000"), Cells = [$"Row {number}", number] })],
        Page = 1,
        PageSize = 30,
        Total = 3
    };

    /// <summary>When set, answers wait for it to complete (and its result is ignored).</summary>
    public Task? Gate { get; set; }

    public CloudDataGridDataRequest LastRequest => Requests[^1];

    public Func<CloudDataGridDataRequest, Task<CloudDataGridDataResult?>> Provider => async request =>
    {
        Requests.Add(request);

        if (Gate is not null)
            await Gate;

        return Respond(request);
    };
}
