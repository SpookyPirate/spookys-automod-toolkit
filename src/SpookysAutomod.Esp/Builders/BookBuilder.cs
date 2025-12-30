using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Book records.
/// </summary>
public class BookBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Book _book;

    public BookBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _book = mod.Books.AddNew();
        _book.EditorID = editorId;
        _book.Value = 10;
        _book.Weight = 1;
    }

    public BookBuilder WithName(string name)
    {
        _book.Name = name;
        return this;
    }

    public BookBuilder WithDescription(string description)
    {
        _book.Description = description;
        return this;
    }

    public BookBuilder WithText(string text)
    {
        _book.BookText = text;
        return this;
    }

    public BookBuilder WithValue(uint value)
    {
        _book.Value = value;
        return this;
    }

    public BookBuilder WithWeight(float weight)
    {
        _book.Weight = weight;
        return this;
    }

    public BookBuilder AsNote()
    {
        _book.Flags |= Book.Flag.CantBeTaken;
        return this;
    }

    public BookBuilder AsSpellTome(FormKey spellFormKey)
    {
        _book.Teaches = new BookSpell { Spell = spellFormKey.ToLink<ISpellGetter>() };
        return this;
    }

    public BookBuilder AsSkillBook(Skill skill)
    {
        _book.Teaches = new BookSkill { Skill = skill };
        return this;
    }

    public Book Build() => _book;
}
