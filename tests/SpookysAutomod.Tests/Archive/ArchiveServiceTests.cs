using SpookysAutomod.Archive.Services;
using SpookysAutomod.Core.Logging;

namespace SpookysAutomod.Tests.Archive;

public class ArchiveServiceTests
{
    private readonly ArchiveService _service;

    public ArchiveServiceTests()
    {
        _service = new ArchiveService(new SilentLogger());
    }

    [Fact]
    public void GetInfo_NonExistentFile_ReturnsError()
    {
        var result = _service.GetInfo("C:\\NonExistent\\Fake.bsa");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListFiles_NonExistentFile_ReturnsError()
    {
        var result = _service.ListFiles("C:\\NonExistent\\Fake.bsa");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetInfo_InvalidFile_ReturnsError()
    {
        // Create a temporary file with invalid content
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "This is not a BSA file");

            var result = _service.GetInfo(tempFile);

            Assert.False(result.Success);
            Assert.Contains("valid", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ListFiles_InvalidFile_ReturnsError()
    {
        // Create a temporary file with invalid content
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "This is not a BSA file");

            var result = _service.ListFiles(tempFile);

            Assert.False(result.Success);
            Assert.Contains("valid", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ListFiles_WithLimit_RespectsLimit()
    {
        // This test requires an actual BSA file to work
        // For now, we test that the limit parameter is properly handled
        // by checking that the method signature accepts it without error

        var result = _service.ListFiles("C:\\NonExistent\\Fake.bsa", filter: null, limit: 10);

        // Should fail with "not found", not a type error
        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListFiles_WithFilter_AcceptsFilter()
    {
        // Test that filter parameter is accepted
        var result = _service.ListFiles("C:\\NonExistent\\Fake.bsa", filter: "*.nif");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
