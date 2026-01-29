using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Service for managing script properties on quests and aliases.
/// </summary>
public class ScriptPropertyService
{
    private readonly IModLogger _logger;
    private readonly PluginService? _pluginService;

    public ScriptPropertyService(IModLogger logger)
    {
        _logger = logger;
    }

    public ScriptPropertyService(IModLogger logger, PluginService pluginService)
    {
        _logger = logger;
        _pluginService = pluginService;
    }

    /// <summary>
    /// Parse a form link string in format "PluginName.esp|0xFormID" or "PluginName.esp|FormID"
    /// </summary>
    public FormKey? ParseFormLink(string formLink)
    {
        try
        {
            var parts = formLink.Split('|');
            if (parts.Length != 2)
            {
                _logger.Error($"Invalid form link format: {formLink}. Use 'Plugin.esp|0xFormID'");
                return null;
            }

            var pluginName = parts[0].Trim();
            var formIdStr = parts[1].Trim();

            // Parse form ID (hex or decimal)
            uint formId;
            if (formIdStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                formId = Convert.ToUInt32(formIdStr, 16);
            }
            else
            {
                formId = uint.Parse(formIdStr);
            }

            var modKey = ModKey.FromFileName(pluginName);
            return new FormKey(modKey, formId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse form link '{formLink}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Set an object property on a script (reference to a form).
    /// </summary>
    public bool SetObjectProperty(ScriptEntry script, string propertyName, FormKey formKey, short aliasIndex = -1)
    {
        // Remove existing property if present
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
        {
            script.Properties.Remove(existing);
        }

        var prop = new ScriptObjectProperty
        {
            Name = propertyName,
            Flags = ScriptProperty.Flag.Edited,
            Object = formKey.ToLink<ISkyrimMajorRecordGetter>(),
            Alias = aliasIndex
        };

        script.Properties.Add(prop);
        _logger.Debug($"Set object property '{propertyName}' = {formKey}");
        return true;
    }

    /// <summary>
    /// Set an object property using a form link string.
    /// </summary>
    public bool SetObjectProperty(ScriptEntry script, string propertyName, string formLink)
    {
        var formKey = ParseFormLink(formLink);
        if (formKey == null)
            return false;

        return SetObjectProperty(script, propertyName, formKey.Value);
    }

    /// <summary>
    /// Set a property referencing an alias within the same quest.
    /// The alias is referenced by its index (ID).
    /// </summary>
    public bool SetAliasProperty(ScriptEntry script, string propertyName, Quest quest, string aliasName)
    {
        var alias = quest.Aliases.FirstOrDefault(a => a.Name == aliasName);
        if (alias == null)
        {
            _logger.Error($"Alias not found: {aliasName}");
            return false;
        }

        // Remove existing property if present
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
        {
            script.Properties.Remove(existing);
        }

        // For alias properties, we set the Object to the quest and Alias to the alias index
        var prop = new ScriptObjectProperty
        {
            Name = propertyName,
            Flags = ScriptProperty.Flag.Edited,
            Object = quest.ToLink<ISkyrimMajorRecordGetter>(),
            Alias = (short)alias.ID
        };

        script.Properties.Add(prop);
        _logger.Debug($"Set alias property '{propertyName}' = alias [{alias.ID}] {aliasName}");
        return true;
    }

    /// <summary>
    /// Set an integer property.
    /// </summary>
    public bool SetIntProperty(ScriptEntry script, string propertyName, int value)
    {
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
            script.Properties.Remove(existing);

        var prop = new ScriptIntProperty
        {
            Name = propertyName,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };

        script.Properties.Add(prop);
        _logger.Debug($"Set int property '{propertyName}' = {value}");
        return true;
    }

    /// <summary>
    /// Set a float property.
    /// </summary>
    public bool SetFloatProperty(ScriptEntry script, string propertyName, float value)
    {
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
            script.Properties.Remove(existing);

        var prop = new ScriptFloatProperty
        {
            Name = propertyName,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };

        script.Properties.Add(prop);
        _logger.Debug($"Set float property '{propertyName}' = {value}");
        return true;
    }

    /// <summary>
    /// Set a boolean property.
    /// </summary>
    public bool SetBoolProperty(ScriptEntry script, string propertyName, bool value)
    {
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
            script.Properties.Remove(existing);

        var prop = new ScriptBoolProperty
        {
            Name = propertyName,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };

        script.Properties.Add(prop);
        _logger.Debug($"Set bool property '{propertyName}' = {value}");
        return true;
    }

    /// <summary>
    /// Set a string property.
    /// </summary>
    public bool SetStringProperty(ScriptEntry script, string propertyName, string value)
    {
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
            script.Properties.Remove(existing);

        var prop = new ScriptStringProperty
        {
            Name = propertyName,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };

        script.Properties.Add(prop);
        _logger.Debug($"Set string property '{propertyName}' = '{value}'");
        return true;
    }

    /// <summary>
    /// Find a script on a quest by name.
    /// </summary>
    public ScriptEntry? FindQuestScript(Quest quest, string scriptName)
    {
        if (quest.VirtualMachineAdapter is not QuestAdapter adapter)
            return null;

        return adapter.Scripts.FirstOrDefault(s =>
            string.Equals(s.Name, scriptName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find a script on an alias by name.
    /// Alias scripts are stored in QuestFragmentAlias in the Quest's VirtualMachineAdapter.
    /// </summary>
    public ScriptEntry? FindAliasScript(QuestAlias alias, string scriptName)
    {
        // NOTE: This method won't work because we need the Quest to access QuestFragmentAlias.
        // Use FindAliasScript(Quest, string aliasName, string scriptName) instead.
        return null;
    }

    /// <summary>
    /// Find a script on an alias by name, using the quest's QuestFragmentAlias.
    /// This is the correct way to find alias scripts in Mutagen.
    /// </summary>
    public ScriptEntry? FindAliasScript(Quest quest, string aliasName, string scriptName)
    {
        if (quest.VirtualMachineAdapter is not QuestAdapter adapter)
            return null;

        // Find the alias to get its ID
        var alias = quest.Aliases.FirstOrDefault(a => a.Name == aliasName);
        if (alias == null)
            return null;

        var aliasIndex = (short)alias.ID;

        // Find the QuestFragmentAlias for this alias
        var fragAlias = adapter.Aliases.FirstOrDefault(fa =>
            fa.Property?.Alias == aliasIndex ||
            fa.Property?.Name == aliasName);

        if (fragAlias == null)
            return null;

        // Find the script in the fragment alias
        return fragAlias.Scripts.FirstOrDefault(s =>
            string.Equals(s.Name, scriptName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Generic method to set a property on a script, parsing the value based on the type.
    /// </summary>
    public Result SetPropertyOnScript(
        SkyrimMod mod,
        ScriptEntry script,
        string propertyName,
        string value,
        ScriptProperty.Type propertyType,
        string contextDescription)
    {
        // Remove existing property if present
        var existing = script.Properties.FirstOrDefault(p => p.Name == propertyName);
        if (existing != null)
        {
            script.Properties.Remove(existing);
        }

        IScriptProperty newProperty;

        switch (propertyType)
        {
            case ScriptProperty.Type.Object:
                var formKey = ParseFormLink(value);
                if (formKey == null)
                    return Result.Fail($"Invalid form link for property '{propertyName}': {value}");

                newProperty = new ScriptObjectProperty
                {
                    Name = propertyName,
                    Flags = ScriptProperty.Flag.Edited,
                    Object = formKey.Value.ToLink<ISkyrimMajorRecordGetter>()
                };
                break;

            case ScriptProperty.Type.Int:
                if (!int.TryParse(value, out var intValue))
                    return Result.Fail($"Invalid integer value for property '{propertyName}': {value}");
                newProperty = new ScriptIntProperty
                {
                    Name = propertyName,
                    Flags = ScriptProperty.Flag.Edited,
                    Data = intValue
                };
                break;

            case ScriptProperty.Type.Float:
                if (!float.TryParse(value, out var floatValue))
                    return Result.Fail($"Invalid float value for property '{propertyName}': {value}");
                newProperty = new ScriptFloatProperty
                {
                    Name = propertyName,
                    Flags = ScriptProperty.Flag.Edited,
                    Data = floatValue
                };
                break;

            case ScriptProperty.Type.Bool:
                if (!bool.TryParse(value, out var boolValue))
                    return Result.Fail($"Invalid boolean value for property '{propertyName}': {value}");
                newProperty = new ScriptBoolProperty
                {
                    Name = propertyName,
                    Flags = ScriptProperty.Flag.Edited,
                    Data = boolValue
                };
                break;

            case ScriptProperty.Type.String:
                newProperty = new ScriptStringProperty
                {
                    Name = propertyName,
                    Flags = ScriptProperty.Flag.Edited,
                    Data = value
                };
                break;

            default:
                return Result.Fail($"Unsupported property type: {propertyType}");
        }

        script.Properties.Add((ScriptProperty)newProperty);
        _logger.Info($"Set {propertyType} property '{propertyName}' = '{value}' on script '{script.Name}' ({contextDescription})");
        return Result.Ok();
    }
}
