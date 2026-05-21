using System.Globalization;

namespace EastFive.Serialization.Binding
{
    /// <summary>Default immutable <see cref="IBindingContext"/>.</summary>
    public sealed class BindingContext : IBindingContext
    {
        public BindingContext(
            ITypeBindings typeBindings,
            IBindingSlot slot = null,
            string keyPath = "",
            CultureInfo culture = null)
        {
            TypeBindings = typeBindings;
            Slot = slot;
            KeyPath = keyPath ?? string.Empty;
            Culture = culture ?? CultureInfo.InvariantCulture;
        }

        public ITypeBindings TypeBindings { get; }

        public IBindingSlot Slot { get; }

        public string KeyPath { get; }

        public CultureInfo Culture { get; }

        public IBindingContext WithSlot(IBindingSlot slot) =>
            new BindingContext(TypeBindings, slot, KeyPath, Culture);

        public IBindingContext WithKeyPath(string keyPath) =>
            new BindingContext(TypeBindings, Slot, keyPath, Culture);

        public IBindingContext WithTypeBindings(ITypeBindings typeBindings) =>
            new BindingContext(typeBindings, Slot, KeyPath, Culture);
    }
}
