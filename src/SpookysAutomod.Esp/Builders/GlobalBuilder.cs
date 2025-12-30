using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Global Variable records.
/// </summary>
public class GlobalBuilder
{
    private readonly SkyrimMod _mod;
    private readonly string _editorId;
    private GlobalType _type = GlobalType.Float;
    private float _value = 0f;

    public GlobalBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _editorId = editorId;
    }

    /// <summary>
    /// Set as short (integer) global.
    /// </summary>
    public GlobalBuilder AsShort(short value = 0)
    {
        _type = GlobalType.Short;
        _value = value;
        return this;
    }

    /// <summary>
    /// Set as long (integer) global.
    /// </summary>
    public GlobalBuilder AsLong(int value = 0)
    {
        _type = GlobalType.Long;
        _value = value;
        return this;
    }

    /// <summary>
    /// Set as float global.
    /// </summary>
    public GlobalBuilder AsFloat(float value = 0f)
    {
        _type = GlobalType.Float;
        _value = value;
        return this;
    }

    /// <summary>
    /// Set the initial value.
    /// </summary>
    public GlobalBuilder WithValue(float value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Build and return the global variable record.
    /// </summary>
    public Global Build()
    {
        Global global;

        switch (_type)
        {
            case GlobalType.Short:
                var shortGlobal = _mod.Globals.AddNewShort(_editorId);
                shortGlobal.Data = (short)_value;
                global = shortGlobal;
                break;
            case GlobalType.Long:
                var longGlobal = _mod.Globals.AddNewInt(_editorId);
                longGlobal.Data = (int)_value;
                global = longGlobal;
                break;
            default:
                var floatGlobal = _mod.Globals.AddNewFloat(_editorId);
                floatGlobal.Data = _value;
                global = floatGlobal;
                break;
        }

        global.EditorID = _editorId;
        return global;
    }
}

internal enum GlobalType
{
    Short,
    Long,
    Float
}
