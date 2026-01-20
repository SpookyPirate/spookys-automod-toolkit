using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating FormList records.
/// </summary>
public class FormListBuilder
{
    private readonly SkyrimMod _mod;
    private readonly FormList _formList;
    private readonly List<FormKey> _forms = new();

    public FormListBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _formList = mod.FormLists.AddNew();
        _formList.EditorID = editorId;
    }

    /// <summary>
    /// Adds a single form to the list.
    /// </summary>
    /// <param name="formKey">FormKey of the record to add</param>
    public FormListBuilder AddForm(FormKey formKey)
    {
        _forms.Add(formKey);
        return this;
    }

    /// <summary>
    /// Adds multiple forms to the list.
    /// </summary>
    /// <param name="formKeys">Array of FormKeys to add</param>
    public FormListBuilder AddForms(params FormKey[] formKeys)
    {
        _forms.AddRange(formKeys);
        return this;
    }

    /// <summary>
    /// Adds multiple forms to the list.
    /// </summary>
    /// <param name="formKeys">Collection of FormKeys to add</param>
    public FormListBuilder AddForms(IEnumerable<FormKey> formKeys)
    {
        _forms.AddRange(formKeys);
        return this;
    }

    /// <summary>
    /// Builds and returns the FormList record.
    /// </summary>
    public FormList Build()
    {
        // Set Items property through reflection or create new FormList with items
        if (_forms.Count > 0)
        {
            var items = new ExtendedList<IFormLinkGetter<ISkyrimMajorRecordGetter>>();
            foreach (var formKey in _forms)
            {
                items.Add(formKey.ToLink<ISkyrimMajorRecordGetter>());
            }
            _formList.Items.SetTo(items);
        }
        return _formList;
    }
}
