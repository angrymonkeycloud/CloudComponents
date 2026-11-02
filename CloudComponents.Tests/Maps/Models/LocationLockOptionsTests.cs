using AngryMonkey.CloudComponents.Maps.Models;

namespace CloudComponents.Tests.Maps.Models;

public sealed class LocationLockOptionsTests
{
    [Fact]
    public void The_country_code_constructor_makes_one_whole_country_area_per_code_in_order()
    {
        LocationLockOptions options = new("LB", "FR", "US");

        Assert.Equal(["LB", "FR", "US"], options.Areas.Select(a => a.CountryCode));
        Assert.All(options.Areas, area => Assert.Null(area.SubdivisionCodes));
    }

    [Fact]
    public void The_country_code_constructor_with_a_single_code_makes_a_single_area()
    {
        LocationLockOptions options = new("LB");

        LocationLockArea area = Assert.Single(options.Areas);
        Assert.Equal("LB", area.CountryCode);
    }

    [Fact]
    public void The_country_code_constructor_accepts_an_array()
    {
        string[] codes = ["LB", "FR"];

        LocationLockOptions options = new(codes);

        Assert.Equal(2, options.Areas.Count);
    }

    [Fact]
    public void The_country_code_constructor_with_no_codes_makes_a_lock_with_no_areas()
    {
        LocationLockOptions options = new();

        Assert.Empty(options.Areas);
    }

    [Fact]
    public void The_areas_constructor_keeps_the_given_list_instance()
    {
        IReadOnlyList<LocationLockArea> areas = [new LocationLockArea("US", ["CA"])];

        LocationLockOptions options = new(areas);

        Assert.Same(areas, options.Areas);
    }

    [Fact]
    public void The_defaults_show_and_zoom_to_the_boundary_in_green_with_the_standard_message()
    {
        LocationLockOptions options = new("LB");

        Assert.True(options.ShowBoundary);
        Assert.True(options.ZoomToBoundary);
        Assert.Equal("rgba(16, 124, 16, 0.10)", options.BoundaryFillColor);
        Assert.Equal("#107c10", options.BoundaryStrokeColor);
        Assert.Equal("This location is outside the allowed area.", options.RestrictionMessage);
    }

    [Fact]
    public void Every_option_can_be_overridden_with_an_initialiser()
    {
        LocationLockOptions options = new("LB")
        {
            ShowBoundary = false,
            ZoomToBoundary = false,
            BoundaryFillColor = "#111",
            BoundaryStrokeColor = "#222",
            RestrictionMessage = "No."
        };

        Assert.False(options.ShowBoundary);
        Assert.False(options.ZoomToBoundary);
        Assert.Equal("#111", options.BoundaryFillColor);
        Assert.Equal("#222", options.BoundaryStrokeColor);
        Assert.Equal("No.", options.RestrictionMessage);
    }

    [Fact]
    public void A_copy_keeps_the_areas_and_the_options()
    {
        LocationLockOptions options = new("LB") { RestrictionMessage = "No." };

        LocationLockOptions copy = options with { ShowBoundary = false };

        Assert.Same(options.Areas, copy.Areas);
        Assert.Equal("No.", copy.RestrictionMessage);
        Assert.False(copy.ShowBoundary);
        Assert.True(options.ShowBoundary);
    }

    [Fact]
    public void Two_locks_built_from_the_same_codes_are_not_equal_because_the_area_lists_compare_by_reference()
    {
        Assert.NotEqual(new LocationLockOptions("LB"), new LocationLockOptions("LB"));
    }

    [Fact]
    public void A_lock_equals_its_own_copy_and_differs_when_an_option_changes()
    {
        LocationLockOptions options = new("LB");

        Assert.Equal(options, options with { });
        Assert.NotEqual(options, options with { RestrictionMessage = "x" });
        Assert.NotEqual(options, options with { ShowBoundary = false });
        Assert.NotEqual(options, options with { ZoomToBoundary = false });
        Assert.NotEqual(options, options with { BoundaryFillColor = "x" });
        Assert.NotEqual(options, options with { BoundaryStrokeColor = "x" });
    }

    [Fact]
    public void An_area_without_subdivisions_means_the_whole_country()
    {
        LocationLockArea area = new("LB");

        Assert.Null(area.SubdivisionCodes);
    }

    [Fact]
    public void Areas_with_the_same_country_and_the_same_subdivision_list_are_equal()
    {
        IReadOnlyList<string> codes = ["CA"];

        Assert.Equal(new LocationLockArea("US", codes), new LocationLockArea("US", codes));
        Assert.NotEqual(new LocationLockArea("US", codes), new LocationLockArea("CA", codes));
        Assert.NotEqual(new LocationLockArea("US", codes), new LocationLockArea("US"));
    }
}
