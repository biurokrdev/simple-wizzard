using D2ServicesCreator.Api.Services;
using FluentAssertions;

namespace D2ServicesCreator.Tests.Unit.Services;

public class UnavailableFileStorageServiceTests
{
    private readonly UnavailableFileStorageService _sut = new();

    [Fact]
    public async Task EveryOperation_ThrowsNotSupported_PointingToD2ApiCreator()
    {
        // The Services API must never touch template binaries — a loud failure, not a silent no-op.
        var upload = () => _sut.UploadAsync("templates/x/y", new byte[] { 1 }, null);
        var download = () => _sut.DownloadAsync("templates/x/y");
        var delete = () => _sut.DeleteAsync("templates/x/y");
        var exists = () => _sut.ExistsAsync("templates/x/y");

        (await upload.Should().ThrowAsync<NotSupportedException>()).WithMessage("*D2ApiCreator*");
        await download.Should().ThrowAsync<NotSupportedException>();
        await delete.Should().ThrowAsync<NotSupportedException>();
        await exists.Should().ThrowAsync<NotSupportedException>();
    }
}
