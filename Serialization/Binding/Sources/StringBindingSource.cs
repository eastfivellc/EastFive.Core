using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Sources
{
    /// <summary>
    /// <see cref="IBindingSource"/> over a raw <see cref="string"/>. Used by the
    /// query-string and form-field binding paths (one source per value). Object-shaped
    /// accessors (<c>GetScoped</c>, <c>GetMembers</c>) fail with
    /// <see cref="WrongSourceType"/>; array access delegates to the legacy
    /// <c>ParseStringToArray</c> parser.
    /// <para>
    /// Recognized null sentinels (matching legacy <c>StringExtensions.BindTo</c>):
    /// <c>null</c>, empty/whitespace string, the literal <c>"null"</c> and
    /// <c>"empty"</c> (case-insensitive).
    /// </para>
    /// </summary>
    public sealed class StringBindingSource : IBindingSource
    {
        private readonly string value;
        private readonly CultureInfo culture;

        public StringBindingSource(string value, CultureInfo culture = null)
        {
            this.value = value;
            this.culture = culture ?? CultureInfo.InvariantCulture;
        }

        private bool RepresentsNull =>
            string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "empty", StringComparison.OrdinalIgnoreCase);

        private ValueTask<TResult> Null<TResult>(Type expected, Func<BindFailure, TResult> onFailure, Func<TResult> onNull)
        {
            if (onNull is not null)
                return new ValueTask<TResult>(onNull());
            return new ValueTask<TResult>(onFailure(new BindFailure(new NullValue(), expected)));
        }

        public ValueTask<TResult> GetString<TResult>(Func<string, TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(string), onFailure, onNull);
            return new ValueTask<TResult>(onValue(value));
        }

        public ValueTask<TResult> GetGuid<TResult>(Func<Guid, TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(Guid), onFailure, onNull);
            if (Guid.TryParse(value, out var g))
                return new ValueTask<TResult>(onValue(g));
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new ParseError($"'{value}' is not a Guid"), typeof(Guid))));
        }

        public ValueTask<TResult> GetBool<TResult>(Func<bool, TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(bool), onFailure, onNull);
            if (bool.TryParse(value, out var b))
                return new ValueTask<TResult>(onValue(b));
            var lower = value.Trim().ToLowerInvariant();
            if (lower is "1" or "yes" or "on") return new ValueTask<TResult>(onValue(true));
            if (lower is "0" or "no" or "off") return new ValueTask<TResult>(onValue(false));
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new ParseError($"'{value}' is not a Boolean"), typeof(bool))));
        }

        public ValueTask<TResult> GetInt64<TResult>(Func<long, TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(long), onFailure, onNull);
            if (long.TryParse(value, NumberStyles.Integer, culture, out var v))
                return new ValueTask<TResult>(onValue(v));
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new ParseError($"'{value}' is not an integer"), typeof(long))));
        }

        public ValueTask<TResult> GetDouble<TResult>(Func<double, TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(double), onFailure, onNull);
            if (double.TryParse(value, NumberStyles.Float, culture, out var v))
                return new ValueTask<TResult>(onValue(v));
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new ParseError($"'{value}' is not a number"), typeof(double))));
        }

        public ValueTask<TResult> GetDateTime<TResult>(Func<DateTime, TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(DateTime), onFailure, onNull);
            if (DateTime.TryParse(value, culture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var v))
                return new ValueTask<TResult>(onValue(v));
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new ParseError($"'{value}' is not a DateTime"), typeof(DateTime))));
        }

        public ValueTask<TResult> GetBytes<TResult>(Func<byte[], TResult> onValue, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(byte[]), onFailure, onNull);
            try
            {
                var bytes = Convert.FromBase64String(value);
                return new ValueTask<TResult>(onValue(bytes));
            }
            catch (FormatException ex)
            {
                return new ValueTask<TResult>(onFailure(new BindFailure(
                    new ParseError($"'{value}' is not valid base64: {ex.Message}"), typeof(byte[]))));
            }
        }

        public ValueTask<TResult> GetScoped<TResult>(string key, Func<IBindingSource, TResult> onChild, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(object), onFailure, onNull);
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new WrongSourceType("object", "string"), typeof(object))));
        }

        public ValueTask<TResult> GetIndexed<TResult>(int index, Func<IBindingSource, TResult> onChild, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(object), onFailure, onNull);
            var items = StringExtensions.ParseStringToArray(value);
            if (index < 0 || index >= items.Length)
                return new ValueTask<TResult>(onFailure(new BindFailure(
                    new NotPresent(), typeof(object))));
            return new ValueTask<TResult>(onChild(new StringBindingSource(items[index], culture)));
        }

        public ValueTask<TResult> GetArray<TResult>(Func<IEnumerable<IBindingSource>, TResult> onItems, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(object), onFailure, onNull);
            var items = StringExtensions.ParseStringToArray(value);
            var sources = items.Select(s => (IBindingSource)new StringBindingSource(s, culture));
            return new ValueTask<TResult>(onItems(sources));
        }

        public ValueTask<TResult> GetMembers<TResult>(Func<IEnumerable<KeyValuePair<string, IBindingSource>>, TResult> onMembers, Func<BindFailure, TResult> onFailure, Func<TResult> onNull = null)
        {
            if (RepresentsNull) return Null(typeof(object), onFailure, onNull);
            return new ValueTask<TResult>(onFailure(new BindFailure(
                new WrongSourceType("object", "string"), typeof(object))));
        }
    }
}
