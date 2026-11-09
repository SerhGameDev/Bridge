#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    /// <summary>Explicit typed converters; no reflection serializer or runtime code generation.</summary>
    public sealed class BridgeSerializer
    {
        private readonly Dictionary<Type, object> converters = new Dictionary<Type, object>();
        /// <summary>Independent registration table; delegate targets must be stateless or caller-synchronized.</summary>
        public BridgeSerializer Clone()
        {
            var copy = new BridgeSerializer(); copy.CopyFrom(this); return copy;
        }
        internal void CopyFrom(BridgeSerializer source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var snapshot = new Dictionary<Type, object>(source.converters);
            converters.Clear(); foreach (var pair in snapshot) converters.Add(pair.Key, pair.Value);
        }
        private static TResult Convert<TResult>(Func<TResult> action)
        {
            try { return action(); }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
            { throw InternalJson.Error("DTO_CONVERSION", ex.Message); }
        }
        private sealed class Converter<T>
        {
            internal readonly Func<BridgeValue, T> Read;
            internal readonly Func<T, BridgeValue> Write;
            internal Converter(Func<BridgeValue, T> read, Func<T, BridgeValue> write) { Read = read; Write = write; }
        }
        public void Register<T>(Func<BridgeValue, T> read, Func<T, BridgeValue> write)
        {
            if (read == null || write == null) throw new ArgumentNullException(read == null ? nameof(read) : nameof(write));
            converters[typeof(T)] = new Converter<T>(read, write);
        }
        public T Deserialize<T>(BridgeValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return converters.TryGetValue(typeof(T), out var item) ? Convert(() => ((Converter<T>)item).Read(value)) : PrimitiveRead<T>(value);
        }
        public BridgeValue Serialize<T>(T value)
        {
            if (converters.TryGetValue(typeof(T), out var item))
            {
                var result = Convert(() => ((Converter<T>)item).Write(value));
                if (result == null) throw InternalJson.Error("DTO_CONVERSION", "Converter must return BridgeValue.Null instead of a null BridgeValue.");
                return new BridgeValue(result.Token);
            }
            return PrimitiveWrite(value);
        }
        internal static BridgeValue PrimitiveWrite(object? value)
        {
            if (value == null) return BridgeValue.Null;
            if (value is BridgeValue json) return new BridgeValue(json.Token);
            if (value is string text) { InternalJson.EnsureUnicode(text); return new BridgeValue(new JValue(text)); }
            if (value is bool boolean) return new BridgeValue(new JValue(boolean));
            if (value is double d && (double.IsNaN(d) || double.IsInfinity(d)) || value is float f && (float.IsNaN(f) || float.IsInfinity(f)))
                throw InternalJson.Error("VALUE_TYPE", "NaN and Infinity are not JSON numbers.");
            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is decimal || value is BigInteger || value is double || value is float)
            {
                string number = value is double dd ? dd.ToString("R", CultureInfo.InvariantCulture)
                    : value is float ff ? ff.ToString("R", CultureInfo.InvariantCulture) : ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
                return new BridgeValue(new JRaw(number));
            }
            throw InternalJson.Error("DTO_CONVERTER_MISSING", $"Register an explicit converter for {value.GetType().FullName}.");
        }
        internal static T PrimitiveRead<T>(BridgeValue value)
        {
            Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (type == typeof(BridgeValue)) return (T)(object)new BridgeValue(value.Token);
            if (value.Kind == BridgeValueKind.Null)
            {
                if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null) return default!;
                throw InternalJson.Error("VALUE_TYPE", $"JSON null cannot be read as {typeof(T).Name}.");
            }
            if (type == typeof(string) && value.Kind == BridgeValueKind.String) return (T)(object)(string)value.Token!;
            if (type == typeof(bool) && value.Kind == BridgeValueKind.Boolean) return (T)(object)(bool)value.Token;
            if (type == typeof(string) || type == typeof(bool)) throw InternalJson.Error("VALUE_TYPE", $"JSON value cannot be read as {type.Name}.");
            if (!(type == typeof(BigInteger) || type == typeof(double) || type == typeof(float) || type == typeof(decimal)
                || type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong)
                || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte)))
                throw InternalJson.Error("DTO_CONVERTER_MISSING", $"Register an explicit converter for {typeof(T).FullName}.");
            try
            {
                if (value.Kind != BridgeValueKind.Number) throw new FormatException("JSON value has incompatible kind.");
                string n = value.ToJson(); object result;
                if (type == typeof(BigInteger)) result = BigInteger.Parse(n, CultureInfo.InvariantCulture);
                else if (type == typeof(double)) { double d = double.Parse(n, NumberStyles.Float, CultureInfo.InvariantCulture); if (double.IsInfinity(d)) throw new OverflowException(); result = d; }
                else if (type == typeof(float)) { float f = float.Parse(n, NumberStyles.Float, CultureInfo.InvariantCulture); if (float.IsInfinity(f)) throw new OverflowException(); result = f; }
                else if (type == typeof(decimal)) result = decimal.Parse(n, NumberStyles.Float, CultureInfo.InvariantCulture);
                else if (type == typeof(int)) result = int.Parse(n, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                else if (type == typeof(long)) result = long.Parse(n, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                else if (type == typeof(uint)) result = uint.Parse(n, NumberStyles.None, CultureInfo.InvariantCulture);
                else if (type == typeof(ulong)) result = ulong.Parse(n, NumberStyles.None, CultureInfo.InvariantCulture);
                else if (type == typeof(short)) result = short.Parse(n, CultureInfo.InvariantCulture);
                else if (type == typeof(ushort)) result = ushort.Parse(n, CultureInfo.InvariantCulture);
                else if (type == typeof(byte)) result = byte.Parse(n, CultureInfo.InvariantCulture);
                else if (type == typeof(sbyte)) result = sbyte.Parse(n, CultureInfo.InvariantCulture);
                else throw InternalJson.Error("DTO_CONVERTER_MISSING", $"Register an explicit converter for {typeof(T).FullName}.");
                return (T)result;
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            { throw InternalJson.Error("VALUE_TYPE", $"JSON value cannot be read as {typeof(T).Name}: {ex.Message}"); }
        }
    }
}
