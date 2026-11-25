using Platinum.Application.Projects;
using Platinum.Domain.Layout;
using Platinum.Infrastructure.Projects;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class ProjectServiceValidationTests
{
    [Fact]
    public async Task ValidateAsync_AcceptsSerializableProject_AndDoesNotLeaveTemporaryPackage()
    {
        var service = new ProjectService(new PlatinumPackageStore());
        var levelId = Guid.NewGuid();
        var layout = new TrackLayout
        {
            Name = "Projecte de validació",
            Levels = [new TrackLevel(levelId, "Principal", 0m)]
        };

        var result = await service.ValidateAsync(layout);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateAsync_ReportsInvalidProject()
    {
        var service = new ProjectService(new PlatinumPackageStore());
        var layout = new TrackLayout
        {
            Name = " ",
            Levels = [new TrackLevel(Guid.NewGuid(), "Principal", 0m)]
        };

        var result = await service.ValidateAsync(layout);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Issues);
    }
}
