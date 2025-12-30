using SpookysAutomod.Core.Logging;
using SpookysAutomod.Esp.Services;

namespace SpookysAutomod.Tests.Esp;

public class PluginServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PluginService _service;

    public PluginServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SpookysAutomodTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new PluginService(new SilentLogger());
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public void CreatePlugin_WithValidName_CreatesFile()
    {
        var result = _service.CreatePlugin("TestMod.esp", _tempDir);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.True(File.Exists(result.Value));
    }

    [Fact]
    public void CreatePlugin_AsLight_CreatesLightPlugin()
    {
        var result = _service.CreatePlugin("LightMod.esp", _tempDir, isLight: true);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);

        // Verify it's actually a light plugin by reading it back
        var info = _service.GetPluginInfo(result.Value);
        Assert.True(info.Success);
        Assert.True(info.Value!.IsLight);
    }

    [Fact]
    public void CreatePlugin_WithAuthor_SetsAuthor()
    {
        var result = _service.CreatePlugin("AuthorMod.esp", _tempDir, author: "TestAuthor");

        Assert.True(result.Success);

        var info = _service.GetPluginInfo(result.Value!);
        Assert.True(info.Success);
        Assert.Equal("TestAuthor", info.Value!.Author);
    }

    [Fact]
    public void GetPluginInfo_NonExistentFile_ReturnsError()
    {
        var result = _service.GetPluginInfo(Path.Combine(_tempDir, "NonExistent.esp"));

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPluginInfo_ValidPlugin_ReturnsInfo()
    {
        // Create a plugin first
        var createResult = _service.CreatePlugin("InfoTest.esp", _tempDir);
        Assert.True(createResult.Success);

        // Get info
        var result = _service.GetPluginInfo(createResult.Value!);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("InfoTest.esp", result.Value.FileName);
    }

    [Fact]
    public void LoadPluginForEdit_ValidPlugin_ReturnsModInstance()
    {
        // Create a plugin first
        var createResult = _service.CreatePlugin("EditTest.esp", _tempDir);
        Assert.True(createResult.Success);

        // Load for edit
        var result = _service.LoadPluginForEdit(createResult.Value!);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void SavePlugin_AfterEdit_PersistsChanges()
    {
        // Create a plugin
        var createResult = _service.CreatePlugin("SaveTest.esp", _tempDir);
        Assert.True(createResult.Success);
        var pluginPath = createResult.Value!;

        // Load, modify, and save
        var loadResult = _service.LoadPluginForEdit(pluginPath);
        Assert.True(loadResult.Success);

        var saveResult = _service.SavePlugin(loadResult.Value!, pluginPath);
        Assert.True(saveResult.Success);

        // Verify file still exists and is valid
        var infoResult = _service.GetPluginInfo(pluginPath);
        Assert.True(infoResult.Success);
    }

    [Fact]
    public void GenerateSeqFile_NoStartEnabledQuests_ReturnsNoQuestsMessage()
    {
        // Create a plugin without any start-enabled quests
        var createResult = _service.CreatePlugin("NoSeq.esp", _tempDir);
        Assert.True(createResult.Success);

        // Try to generate SEQ
        var seqResult = _service.GenerateSeqFile(createResult.Value!, _tempDir);

        // Should indicate no start-enabled quests found
        Assert.False(seqResult.Success);
        Assert.Contains("start-enabled", seqResult.Error, StringComparison.OrdinalIgnoreCase);
    }
}
