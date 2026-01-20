using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using Noggog;

namespace SpookysAutomod.Esp.Services;

public class AliasService
{
    private readonly IModLogger _logger;

    public AliasService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add a reference alias to a quest
    /// </summary>
    public Result<QuestAlias> AddReferenceAlias(
        SkyrimMod mod,
        string questEditorId,
        string aliasName,
        QuestAlias.Flag flags = 0)
    {
        var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questEditorId);
        if (quest == null)
        {
            return Result<QuestAlias>.Fail($"Quest not found: {questEditorId}");
        }

        // Create new alias with a unique ID (starting from 0)
        var nextId = quest.Aliases.Count > 0 
            ? quest.Aliases.Max(a => a.ID) + 1 
            : 0;
        
        var alias = new QuestAlias
        {
            ID = (uint)nextId,
            Name = aliasName,
            Flags = flags,
            Type = QuestAlias.TypeEnum.Reference
        };
        
        quest.Aliases.Add(alias);

        _logger.Info($"Added alias '{aliasName}' (ID: {alias.ID}) to quest '{questEditorId}'");
        return Result<QuestAlias>.Ok(alias);
    }

    /// <summary>
    /// Attach a script to a quest alias via QuestFragmentAlias
    /// This is the correct way to attach scripts to aliases in Mutagen!
    /// </summary>
    public Result AttachScriptToAlias(
        SkyrimMod mod,
        string questEditorId,
        string aliasName,
        string scriptName)
    {
        var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questEditorId);
        if (quest == null)
        {
            return Result.Fail($"Quest not found: {questEditorId}");
        }

        // Find the alias and get its index
        var alias = quest.Aliases.FirstOrDefault(a => a.Name == aliasName);
        if (alias == null)
        {
            return Result.Fail($"Alias '{aliasName}' not found on quest '{questEditorId}'.");
        }

        // Get the alias index (it's the ID field)
        var aliasIndex = (short)alias.ID;

        // Ensure quest has a VirtualMachineAdapter
        if (quest.VirtualMachineAdapter == null)
        {
            quest.VirtualMachineAdapter = new QuestAdapter();
        }

        var adapter = quest.VirtualMachineAdapter as QuestAdapter;
        if (adapter == null)
        {
            return Result.Fail($"Quest '{questEditorId}' does not have a valid QuestAdapter.");
        }

        // Check if there's already a QuestFragmentAlias for this alias
        var existingFragAlias = adapter.Aliases.FirstOrDefault(fa => 
            fa.Property?.Alias == aliasIndex || 
            fa.Property?.Name == aliasName);

        if (existingFragAlias != null)
        {
            // Check if script is already attached
            if (existingFragAlias.Scripts.Any(s => s.Name == scriptName))
            {
                _logger.Info($"Script '{scriptName}' already attached to alias '{aliasName}'");
                return Result.Ok();
            }

            // Add script to existing fragment alias
            existingFragAlias.Scripts.Add(new ScriptEntry { Name = scriptName });
            _logger.Info($"Added script '{scriptName}' to existing fragment alias for '{aliasName}'");
            return Result.Ok();
        }

        // Create new QuestFragmentAlias
        var fragAlias = new QuestFragmentAlias
        {
            Version = 5, // Standard version
            ObjectFormat = 2, // Standard format
            Property = new ScriptObjectProperty
            {
                Name = aliasName,
                Alias = aliasIndex,
                Flags = ScriptProperty.Flag.Edited,
                Object = new FormLinkNullable<ISkyrimMajorRecordGetter>(quest.FormKey) // CRITICAL: Must link back to the quest!
            }
        };

        // Add the script
        fragAlias.Scripts.Add(new ScriptEntry { Name = scriptName });

        // Add to adapter
        adapter.Aliases.Add(fragAlias);

        _logger.Info($"Attached script '{scriptName}' to alias '{aliasName}' (index {aliasIndex}) on quest '{questEditorId}'");
        return Result.Ok();
    }

    /// <summary>
    /// Get the QuestFragmentAlias for a specific alias, creating one if needed
    /// </summary>
    public Result<QuestFragmentAlias> GetOrCreateFragmentAlias(
        SkyrimMod mod,
        string questEditorId,
        string aliasName)
    {
        var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questEditorId);
        if (quest == null)
        {
            return Result<QuestFragmentAlias>.Fail($"Quest not found: {questEditorId}");
        }

        var alias = quest.Aliases.FirstOrDefault(a => a.Name == aliasName);
        if (alias == null)
        {
            return Result<QuestFragmentAlias>.Fail($"Alias '{aliasName}' not found on quest '{questEditorId}'.");
        }

        var aliasIndex = (short)alias.ID;

        if (quest.VirtualMachineAdapter == null)
        {
            quest.VirtualMachineAdapter = new QuestAdapter();
        }

        var adapter = quest.VirtualMachineAdapter as QuestAdapter;
        if (adapter == null)
        {
            return Result<QuestFragmentAlias>.Fail($"Quest '{questEditorId}' does not have a valid QuestAdapter.");
        }

        var existingFragAlias = adapter.Aliases.FirstOrDefault(fa => 
            fa.Property?.Alias == aliasIndex || 
            fa.Property?.Name == aliasName);

        if (existingFragAlias != null)
        {
            return Result<QuestFragmentAlias>.Ok(existingFragAlias);
        }

        // Create new
        var fragAlias = new QuestFragmentAlias
        {
            Version = 5,
            ObjectFormat = 2,
            Property = new ScriptObjectProperty
            {
                Name = aliasName,
                Alias = aliasIndex,
                Flags = ScriptProperty.Flag.Edited,
                Object = new FormLinkNullable<ISkyrimMajorRecordGetter>(quest.FormKey) // CRITICAL: Must link back to the quest!
            }
        };

        adapter.Aliases.Add(fragAlias);
        return Result<QuestFragmentAlias>.Ok(fragAlias);
    }
}
