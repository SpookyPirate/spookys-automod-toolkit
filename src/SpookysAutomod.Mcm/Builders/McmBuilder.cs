using SpookysAutomod.Mcm.Models;

namespace SpookysAutomod.Mcm.Builders;

/// <summary>
/// Fluent builder for MCM Helper configuration files.
/// </summary>
public class McmBuilder
{
    private readonly McmConfig _config;
    private McmPage? _currentPage;

    public McmBuilder(string modName, string displayName)
    {
        _config = new McmConfig
        {
            ModName = modName,
            DisplayName = displayName
        };
    }

    /// <summary>
    /// Set minimum MCM version required.
    /// </summary>
    public McmBuilder WithMinVersion(int version)
    {
        _config.MinMcmVersion = version;
        return this;
    }

    /// <summary>
    /// Add required plugin dependencies.
    /// </summary>
    public McmBuilder RequiresPlugin(string pluginName)
    {
        _config.PluginRequirements ??= new List<string>();
        _config.PluginRequirements.Add(pluginName);
        return this;
    }

    /// <summary>
    /// Add a new page to the MCM.
    /// </summary>
    public McmBuilder AddPage(string displayName)
    {
        _currentPage = new McmPage { PageDisplayName = displayName };
        _config.Content.Add(_currentPage);
        return this;
    }

    /// <summary>
    /// Add a header control.
    /// </summary>
    public McmBuilder AddHeader(string text)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Header,
            Text = text
        });
        return this;
    }

    /// <summary>
    /// Add a text label.
    /// </summary>
    public McmBuilder AddText(string text, string? help = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Text,
            Text = text,
            Help = help
        });
        return this;
    }

    /// <summary>
    /// Add a toggle control.
    /// </summary>
    public McmBuilder AddToggle(string id, string text, string? help = null, int? groupControl = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Toggle,
            Id = id,
            Text = text,
            Help = help,
            GroupControl = groupControl
        });
        return this;
    }

    /// <summary>
    /// Add a slider control.
    /// </summary>
    public McmBuilder AddSlider(string id, string text, float min, float max, float step = 1, string? help = null, string? format = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Slider,
            Id = id,
            Text = text,
            Help = help,
            Min = min,
            Max = max,
            Step = step,
            FormatString = format ?? "{0}"
        });
        return this;
    }

    /// <summary>
    /// Add a menu/dropdown control.
    /// </summary>
    public McmBuilder AddMenu(string id, string text, List<string> options, string? help = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Menu,
            Id = id,
            Text = text,
            Help = help,
            Options = options
        });
        return this;
    }

    /// <summary>
    /// Add an enum control.
    /// </summary>
    public McmBuilder AddEnum(string id, string text, List<string> options, List<string>? shortNames = null, string? help = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Enum,
            Id = id,
            Text = text,
            Help = help,
            Options = options,
            ShortNames = shortNames
        });
        return this;
    }

    /// <summary>
    /// Add a color picker control.
    /// </summary>
    public McmBuilder AddColor(string id, string text, string? defaultColor = null, string? help = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Color,
            Id = id,
            Text = text,
            Help = help,
            DefaultColor = defaultColor
        });
        return this;
    }

    /// <summary>
    /// Add a keymap control.
    /// </summary>
    public McmBuilder AddKeymap(string id, string text, bool ignoreConflicts = false, string? help = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Keymap,
            Id = id,
            Text = text,
            Help = help,
            IgnoreConflicts = ignoreConflicts
        });
        return this;
    }

    /// <summary>
    /// Add a text input control.
    /// </summary>
    public McmBuilder AddInput(string id, string text, string? help = null)
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Input,
            Id = id,
            Text = text,
            Help = help
        });
        return this;
    }

    /// <summary>
    /// Add an empty spacer.
    /// </summary>
    public McmBuilder AddEmpty()
    {
        EnsurePage();
        _currentPage!.Content.Add(new McmControl
        {
            Type = McmControlType.Empty
        });
        return this;
    }

    /// <summary>
    /// Bind the last control to a global variable.
    /// </summary>
    public McmBuilder BindToGlobal(string formId)
    {
        var lastControl = GetLastControl();
        if (lastControl != null)
        {
            lastControl.SourceType = "GlobalValue";
            lastControl.SourceForm = formId;
        }
        return this;
    }

    /// <summary>
    /// Bind the last control to a script property.
    /// </summary>
    public McmBuilder BindToProperty(string scriptName, string propertyName, string? formId = null)
    {
        var lastControl = GetLastControl();
        if (lastControl != null)
        {
            lastControl.SourceType = "PropertyValue";
            lastControl.ScriptName = scriptName;
            lastControl.PropertyName = propertyName;
            if (formId != null)
                lastControl.SourceForm = formId;
        }
        return this;
    }

    /// <summary>
    /// Build the MCM configuration.
    /// </summary>
    public McmConfig Build()
    {
        return _config;
    }

    private void EnsurePage()
    {
        if (_currentPage == null)
        {
            AddPage("General");
        }
    }

    private McmControl? GetLastControl()
    {
        if (_currentPage == null || _currentPage.Content.Count == 0)
            return null;
        return _currentPage.Content[^1];
    }
}
