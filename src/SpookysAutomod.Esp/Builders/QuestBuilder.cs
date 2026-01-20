using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Quest records.
/// </summary>
public class QuestBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Quest _quest;
    private QuestAdapter? _adapter;

    public QuestBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _quest = mod.Quests.AddNew(editorId);
        _quest.EditorID = editorId;
    }

    /// <summary>
    /// Set the quest display name.
    /// </summary>
    public QuestBuilder WithName(string name)
    {
        _quest.Name = name;
        return this;
    }

    /// <summary>
    /// Set quest flags.
    /// </summary>
    public QuestBuilder WithFlags(Quest.Flag flags)
    {
        _quest.Flags = flags;
        return this;
    }

    /// <summary>
    /// Add a flag to existing flags.
    /// </summary>
    public QuestBuilder AddFlag(Quest.Flag flag)
    {
        _quest.Flags |= flag;
        return this;
    }

    /// <summary>
    /// Make the quest start when the game loads.
    /// </summary>
    public QuestBuilder StartEnabled()
    {
        _quest.Flags |= Quest.Flag.StartGameEnabled;
        return this;
    }

    /// <summary>
    /// Make the quest run only once.
    /// </summary>
    public QuestBuilder RunOnce()
    {
        _quest.Flags |= Quest.Flag.RunOnce;
        return this;
    }

    /// <summary>
    /// Set the quest priority (higher = processed first).
    /// </summary>
    public QuestBuilder WithPriority(byte priority)
    {
        _quest.Priority = priority;
        return this;
    }

    /// <summary>
    /// Attach a script to the quest.
    /// </summary>
    public QuestBuilder WithScript(string scriptName, Action<ScriptBuilder>? configure = null)
    {
        _adapter ??= new QuestAdapter();

        var scriptEntry = new ScriptEntry
        {
            Name = scriptName,
            Flags = ScriptEntry.Flag.Local
        };

        if (configure != null)
        {
            var scriptBuilder = new ScriptBuilder(scriptEntry);
            configure(scriptBuilder);
        }

        _adapter.Scripts.Add(scriptEntry);
        return this;
    }

    /// <summary>
    /// Build and return the quest record.
    /// </summary>
    public Quest Build()
    {
        if (_adapter != null)
        {
            _quest.VirtualMachineAdapter = _adapter;
        }
        return _quest;
    }
}

/// <summary>
/// Fluent builder for script entries and properties.
/// </summary>
public class ScriptBuilder
{
    private readonly ScriptEntry _script;

    public ScriptBuilder(ScriptEntry script)
    {
        _script = script;
    }

    /// <summary>
    /// Add a string property.
    /// </summary>
    public ScriptBuilder WithStringProperty(string name, string value)
    {
        var prop = new ScriptStringProperty
        {
            Name = name,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };
        _script.Properties.Add(prop);
        return this;
    }

    /// <summary>
    /// Add an integer property.
    /// </summary>
    public ScriptBuilder WithIntProperty(string name, int value)
    {
        var prop = new ScriptIntProperty
        {
            Name = name,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };
        _script.Properties.Add(prop);
        return this;
    }

    /// <summary>
    /// Add a float property.
    /// </summary>
    public ScriptBuilder WithFloatProperty(string name, float value)
    {
        var prop = new ScriptFloatProperty
        {
            Name = name,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };
        _script.Properties.Add(prop);
        return this;
    }

    /// <summary>
    /// Add a boolean property.
    /// </summary>
    public ScriptBuilder WithBoolProperty(string name, bool value)
    {
        var prop = new ScriptBoolProperty
        {
            Name = name,
            Flags = ScriptProperty.Flag.Edited,
            Data = value
        };
        _script.Properties.Add(prop);
        return this;
    }

    /// <summary>
    /// Add an object property (FormKey reference).
    /// </summary>
    public ScriptBuilder WithObjectProperty(string name, FormKey formKey)
    {
        var prop = new ScriptObjectProperty
        {
            Name = name,
            Flags = ScriptProperty.Flag.Edited,
            Object = formKey.ToNullableLink<ISkyrimMajorRecordGetter>()
        };
        _script.Properties.Add(prop);
        return this;
    }

    /// <summary>
    /// Add an array property (list of FormKey references).
    /// Creates a ScriptObjectListProperty with multiple elements.
    /// </summary>
    public ScriptBuilder WithArrayProperty(string name, List<FormKey> formKeys)
    {
        if (formKeys.Count == 0)
        {
            throw new ArgumentException("Array property requires at least one FormKey", nameof(formKeys));
        }

        var arrayProp = new ScriptObjectListProperty
        {
            Name = name,
            Flags = ScriptProperty.Flag.Edited,
            Objects = new ExtendedList<ScriptObjectProperty>()
        };

        foreach (var formKey in formKeys)
        {
            var objProp = new ScriptObjectProperty
            {
                Alias = -1,  // -1 indicates no alias reference
                Unused = 0,
                Flags = ScriptProperty.Flag.Edited,
                Object = formKey.ToNullableLink<ISkyrimMajorRecordGetter>()
            };
            arrayProp.Objects.Add(objProp);
        }

        _script.Properties.Add(arrayProp);
        return this;
    }

    /// <summary>
    /// Get the underlying script entry for direct manipulation.
    /// </summary>
    public ScriptEntry GetScript()
    {
        return _script;
    }
}
