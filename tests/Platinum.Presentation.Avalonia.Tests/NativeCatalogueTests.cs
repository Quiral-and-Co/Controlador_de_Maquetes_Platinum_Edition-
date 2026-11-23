using Platinum.Presentation.Avalonia.Catalogue;
using Xunit;

namespace Platinum.Presentation.Avalonia.Tests;

public sealed class NativeCatalogueTests
{
    [Fact]
    public void Catalogue_Is_Compiled_As_Typed_CSharp_Data()
    {
        var catalogue = MarklinCCatalogue.Load();
        Assert.Equal("Märklin", catalogue.Brand);
        Assert.Equal("C-Gleis", catalogue.System);
        Assert.Equal(68, catalogue.Items.Count);
        Assert.Equal(catalogue.Items.Count, catalogue.Items.Select(x => x.Ref).Distinct().Count());
        Assert.Contains(catalogue.Items, x => x.Ref == "24130" && x.LengthMm is null && x.RadiusMm == 360m && x.AngleDeg == 30m);
    }

    [Fact]
    public void Catalogue_Exposes_Native_Geometry_For_Common_Pieces()
    {
        var catalogue = MarklinCCatalogue.Load();
        var straight = Assert.Single(catalogue.Items, x => x.Ref == "24188");
        Assert.Equal(2, straight.Ports.Count);
        Assert.Single(straight.TrackPaths);
        var turnout = Assert.Single(catalogue.Items, x => x.Ref == "24611");
        Assert.Equal(3, turnout.Ports.Count);
        Assert.Equal(2, turnout.TrackPaths.Count);
    }
}
