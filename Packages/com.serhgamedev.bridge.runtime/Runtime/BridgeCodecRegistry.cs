#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bridge.Core;

namespace Bridge.Runtime
{
    internal interface IBridgeCodec
    {
        BridgeValue Schema { get; }
        object? Read(BridgeValue value);
        BridgeValue Write(object? value);
        Task<object?> Await(Task task);
    }
    public sealed class BridgeCodecRegistry
    {
        private sealed class Codec<T> : IBridgeCodec
        {
            public BridgeValue Schema { get; }
            private readonly Func<BridgeValue, T> read;
            private readonly Func<T, BridgeValue> write;
            internal Codec(BridgeValue schema, Func<BridgeValue, T> read, Func<T, BridgeValue> write) { Schema = schema; this.read = read; this.write = write; }
            public object? Read(BridgeValue value) => read(value);
            public BridgeValue Write(object? value) => value == null ? BridgeValue.Null : write((T)value);
            public async Task<object?> Await(Task task) => await ((Task<T>)task);
        }
        private readonly Dictionary<Type, IBridgeCodec> codecs = new Dictionary<Type, IBridgeCodec>();
        public BridgeCodecRegistry()
        {
            Register(Json.Obj(("type", BridgeValue.From("string"))), v => v.As<string>(), BridgeValue.From<string>);
            Register(Json.Obj(("type", BridgeValue.From("boolean"))), v => v.As<bool>(), BridgeValue.From<bool>);
            Register<bool?>(BridgeSchema.Nullable(Json.Obj(("type", BridgeValue.From("boolean")))), v => v.Kind == BridgeValueKind.Null ? (bool?)null : v.As<bool>(), BridgeValue.From<bool?>);
            Integer<sbyte>(sbyte.MinValue, sbyte.MaxValue, v => checked((sbyte)IntegerValue(v)));
            Integer<byte>(byte.MinValue, byte.MaxValue, v => checked((byte)IntegerValue(v)));
            Integer<short>(short.MinValue, short.MaxValue, v => checked((short)IntegerValue(v)));
            Integer<ushort>(ushort.MinValue, ushort.MaxValue, v => checked((ushort)IntegerValue(v)));
            Integer<int>(int.MinValue, int.MaxValue, v => checked((int)IntegerValue(v)));
            Integer<uint>(uint.MinValue, uint.MaxValue, v => checked((uint)IntegerValue(v)));
            Integer<long>(long.MinValue, long.MaxValue, v => checked((long)IntegerValue(v)));
            Integer<ulong>(ulong.MinValue, ulong.MaxValue, v => checked((ulong)IntegerValue(v)));
            Number<float>(-float.MaxValue, float.MaxValue, v => v.As<float>());
            Number<double>(-double.MaxValue, double.MaxValue, v => v.As<double>());
            Number<decimal>((double)decimal.MinValue, (double)decimal.MaxValue, v => v.As<decimal>());
        }
        private static decimal IntegerValue(BridgeValue value) => value.As<decimal>(); // Schema first ensures mathematical integer, including 1.0/1e0.
        private void Integer<T>(double minimum, double maximum, Func<BridgeValue, T> read) where T : struct => Numeric(minimum, maximum, read, "integer");
        private void Number<T>(double minimum, double maximum, Func<BridgeValue, T> read) where T : struct => Numeric(minimum, maximum, read, "number");
        private void Numeric<T>(double minimum, double maximum, Func<BridgeValue, T> read, string type) where T : struct
        {
            var schema = Json.Obj(("type", BridgeValue.From(type)), ("minimum", BridgeValue.From(minimum)), ("maximum", BridgeValue.From(maximum)));
            Register(schema, read, BridgeValue.From<T>);
            Register<T?>(BridgeSchema.Nullable(schema), v => v.Kind == BridgeValueKind.Null ? (T?)null : read(v), BridgeValue.From<T?>);
        }
        public BridgeCodecRegistry Register<T>(BridgeValue schema, Func<BridgeValue, T> read, Func<T, BridgeValue> write)
        {
            if (schema == null || read == null || write == null) throw new ArgumentNullException(schema == null ? nameof(schema) : read == null ? nameof(read) : nameof(write));
            if (typeof(T) == typeof(object) || typeof(T) == typeof(Type) || typeof(Delegate).IsAssignableFrom(typeof(T)) || typeof(T).ContainsGenericParameters) throw new ArgumentException("Unsupported codec type.");
            try { BridgeSchema.Definition(schema); }
            catch (BridgeException ex) { throw new ArgumentException("Invalid registered schema.", nameof(schema), ex); }
            if (codecs.ContainsKey(typeof(T))) throw new ArgumentException("Duplicate codec registration.");
            codecs.Add(typeof(T), new Codec<T>(schema, read, write)); return this;
        }
        public BridgeCodecRegistry RegisterEnum<T>() where T : struct, Enum
        {
            var names = Enum.GetNames(typeof(T));
            var schema = Json.Obj(("type", BridgeValue.From("string")), ("enum", Json.Strings(names)));
            T Read(BridgeValue v) { string name = v.As<string>(); if (!names.Contains(name, StringComparer.Ordinal)) throw new FormatException("Unknown enum value."); return (T)Enum.Parse(typeof(T), name, false); }
            Register(schema, Read, v => BridgeValue.From(v.ToString()));
            Register<T?>(BridgeSchema.Nullable(schema), v => v.Kind == BridgeValueKind.Null ? (T?)null : Read(v), v => v.HasValue ? BridgeValue.From(v.Value.ToString()) : BridgeValue.Null);
            return this;
        }
        internal IBridgeCodec Get(Type type) => codecs.TryGetValue(type, out var codec) ? codec : throw new ArgumentException("An explicit codec is required for " + type.FullName);
        public BridgeValue SchemaFor<T>() => Get(typeof(T)).Schema;
        public BridgeCodecRegistry Clone()
        {
            var copy = new BridgeCodecRegistry(); copy.codecs.Clear();
            foreach (var pair in codecs) copy.codecs.Add(pair.Key, pair.Value); return copy;
        }
    }
}
