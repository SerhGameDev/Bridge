#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;

[assembly: InternalsVisibleTo("Bridge.Runtime.Tests")]

namespace Bridge.Runtime
{
    public sealed class BridgeCommandDescriptor
    {
        public string Name { get; }
        public string Description { get; }
        public BridgePermission Permission { get; }
        public BridgeValue InputSchema { get; }
        public BridgeValue OutputSchema { get; }
        internal BridgeCommandDescriptor(string name, string description, BridgePermission permission, BridgeValue input, BridgeValue output)
        {
            RuntimeGuard.Name(name, nameof(name));
            if (!Enum.IsDefined(typeof(BridgePermission), permission) || permission == BridgePermission.None) throw new ArgumentException("Command permission must be explicit, default Admin.");
            Name = name; Description = description ?? throw new ArgumentNullException(nameof(description)); Permission = permission; InputSchema = input; OutputSchema = output;
        }
    }
    internal sealed class BridgeBindingException : Exception
    {
        internal string Code { get; }
        internal BridgeBindingException(string code) : base(code) { Code = code; }
    }
    internal sealed class CommandEntry
    {
        internal readonly BridgeCommandDescriptor Descriptor;
        internal readonly Func<BridgeValue, CancellationToken, Task<BridgeValue>> Invoke;
        internal CommandEntry(BridgeCommandDescriptor descriptor, Func<BridgeValue, CancellationToken, Task<BridgeValue>> invoke) { Descriptor = descriptor; Invoke = invoke; }
        internal static BridgeValue Result(object? value, IBridgeCodec? codec, BridgeValue schema)
        {
            BridgeValue result;
            try { result = codec == null || value == null ? BridgeValue.Null : codec.Write(value); }
            catch (Exception ex) when (ex is BridgeException || ex is FormatException || ex is OverflowException || ex is InvalidCastException) { throw new BridgeBindingException("INVALID_RESULT"); }
            if (result == null || !BridgeSchema.Matches(result, schema)) throw new BridgeBindingException("INVALID_RESULT");
            return result;
        }
    }
    internal sealed class BridgeCommandRegistry
    {
        private readonly Dictionary<string, CommandEntry> entries = new Dictionary<string, CommandEntry>(StringComparer.Ordinal);
        internal BridgeCommandRegistry(IEnumerable<object> instances, BridgeCodecRegistry codecs, IEnumerable<Func<BridgeCodecRegistry, CommandEntry>> typed)
        {
            foreach (var instance in instances)
                foreach (var method in instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var attribute = method.GetCustomAttribute<BridgeCommandAttribute>(false);
                    if (attribute != null) Add(Reflect(instance, method, attribute, codecs));
                }
            foreach (var factory in typed) Add(factory(codecs));
        }
        private void Add(CommandEntry entry)
        {
            if (entries.ContainsKey(entry.Descriptor.Name)) throw new ArgumentException("Duplicate command name.");
            entries.Add(entry.Descriptor.Name, entry);
        }
        internal bool TryGet(string name, out CommandEntry entry) => entries.TryGetValue(name, out entry!);
        internal IReadOnlyList<BridgeCommandDescriptor> For(BridgeRequestContext context)
            => Array.AsReadOnly(entries.Values.Where(e => context.Allows(e.Descriptor.Permission)).Select(e => e.Descriptor).OrderBy(e => e.Name, StringComparer.Ordinal).ToArray());
        private static CommandEntry Reflect(object instance, MethodInfo method, BridgeCommandAttribute attribute, BridgeCodecRegistry codecs)
        {
            if (method.IsStatic || method.ContainsGenericParameters || method.ReturnType.IsByRef || method.ReturnType.IsPointer
                || method.ReturnType == typeof(void) && method.GetCustomAttribute<AsyncStateMachineAttribute>() != null) throw new ArgumentException("Unsupported command method signature.");
            var parameters = method.GetParameters();
            if (parameters.Any(p => p.ParameterType.IsByRef || p.ParameterType.IsPointer || p.IsOut || p.GetCustomAttribute<ParamArrayAttribute>() != null)
                || parameters.Count(p => p.ParameterType == typeof(CancellationToken)) > 1) throw new ArgumentException("Unsupported command parameters.");
            var properties = new Dictionary<string, BridgeValue>(StringComparer.Ordinal);
            var required = new List<string>(); var bindings = new List<(ParameterInfo parameter, IBridgeCodec? codec, BridgeValue? schema, bool nullable)>();
            foreach (var p in parameters)
            {
                if (p.ParameterType == typeof(CancellationToken))
                {
                    if (p.GetCustomAttribute<BridgeRangeAttribute>() != null || p.GetCustomAttribute<BridgeAllowNullAttribute>() != null) throw new ArgumentException("Service parameter cannot have value constraints.");
                    bindings.Add((p, null, null, false)); continue;
                }
                var codec = codecs.Get(p.ParameterType); var schema = codec.Schema;
                bool explicitNull = p.GetCustomAttribute<BridgeAllowNullAttribute>() != null;
                if (explicitNull && p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) == null) throw new ArgumentException("AllowNull requires a reference or Nullable parameter.");
                bool nullable = BridgeSchema.Matches(BridgeValue.Null, codec.Schema) || Nullable.GetUnderlyingType(p.ParameterType) != null || explicitNull || p.IsOptional && p.DefaultValue == null;
                if (nullable) schema = BridgeSchema.Nullable(schema);
                var description = p.GetCustomAttribute<BridgeDescriptionAttribute>();
                if (description != null) schema = schema.WithField("description", BridgeValue.From(description.Description));
                var range = p.GetCustomAttribute<BridgeRangeAttribute>();
                if (range != null) schema = Range(schema, range);
                if (p.Name == null) throw new ArgumentException("Parameter names are required.");
                properties.Add(p.Name, schema);
                if (!p.IsOptional) required.Add(p.Name);
                else if (!p.HasDefaultValue || !BridgeSchema.Matches(codec.Write(p.DefaultValue), schema)) throw new ArgumentException("Optional default does not match command schema.");
                bindings.Add((p, codec, schema, nullable));
            }
            var input = Json.Obj(("type", BridgeValue.From("object")), ("properties", BridgeValue.Object(properties)), ("required", Json.Strings(required)), ("additionalProperties", BridgeValue.From(false)));
            BridgeSchema.Definition(input);
            Type returned = method.ReturnType;
            bool taskResult = returned.IsGenericType && returned.GetGenericTypeDefinition() == typeof(Task<>);
            bool taskVoid = returned == typeof(Task);
            if (taskResult) returned = returned.GetGenericArguments()[0];
            IBridgeCodec? outputCodec = returned == typeof(void) || taskVoid ? null : codecs.Get(returned);
            var output = outputCodec?.Schema ?? Json.Obj(("type", BridgeValue.From("null")));
            if (outputCodec != null && !returned.IsValueType) output = BridgeSchema.Nullable(output);
            var descriptor = new BridgeCommandDescriptor(attribute.Name, attribute.Description, attribute.Permission, input, output);
            return new CommandEntry(descriptor, async (arguments, cancellation) =>
            {
                if (!BridgeSchema.Matches(arguments, input)) throw new BridgeBindingException("INVALID_ARGUMENT");
                var values = new object?[bindings.Count];
                for (int i = 0; i < bindings.Count; i++)
                {
                    var b = bindings[i];
                    if (b.codec == null) { values[i] = cancellation; continue; }
                    if (!arguments.TryGetProperty(b.parameter.Name!, out var value)) { values[i] = b.parameter.DefaultValue; continue; }
                    try { values[i] = value!.Kind == BridgeValueKind.Null && b.nullable ? null : b.codec.Read(value!); }
                    catch (Exception ex) when (ex is BridgeException || ex is FormatException || ex is OverflowException || ex is InvalidCastException) { throw new BridgeBindingException("INVALID_ARGUMENT"); }
                    if (values[i] == null && !b.nullable) throw new BridgeBindingException("INVALID_ARGUMENT");
                }
                cancellation.ThrowIfCancellationRequested();
                object? result;
                try { result = method.Invoke(instance, values); }
                catch (TargetInvocationException ex) when (ex.InnerException != null) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
                if (taskResult) result = await outputCodec!.Await((Task)(result ?? throw new InvalidOperationException("Handler returned a null Task.")));
                else if (taskVoid) { await (Task)(result ?? throw new InvalidOperationException("Handler returned a null Task.")); result = null; }
                return CommandEntry.Result(result, outputCodec, output);
            });
        }
        private static BridgeValue Range(BridgeValue schema, BridgeRangeAttribute range)
        {
            if (double.IsNaN(range.Minimum) || double.IsNaN(range.Maximum) || double.IsInfinity(range.Minimum) || double.IsInfinity(range.Maximum) || range.Minimum > range.Maximum) throw new ArgumentException("Invalid command range.");
            if (schema.TryGetProperty("anyOf", out var choices)) return schema.WithField("anyOf", BridgeValue.Array(choices!.Items.Select(s => s.Get<string>("type") == "null" ? s : Range(s, range))));
            if (schema.Get<string>("type") != "number" && schema.Get<string>("type") != "integer") throw new ArgumentException("Range requires numeric parameter.");
            double minimum = schema.TryGetProperty("minimum", out var min) ? Math.Max(min!.As<double>(), range.Minimum) : range.Minimum;
            double maximum = schema.TryGetProperty("maximum", out var max) ? Math.Min(max!.As<double>(), range.Maximum) : range.Maximum;
            if (minimum > maximum) throw new ArgumentException("Empty numeric range.");
            return schema.WithField("minimum", BridgeValue.From(minimum)).WithField("maximum", BridgeValue.From(maximum));
        }
    }
    public sealed partial class BridgeHostBuilder
    {
        private BridgeCodecRegistry codecs = new BridgeCodecRegistry();
        private readonly List<Func<BridgeCodecRegistry, CommandEntry>> typedCommands = new List<Func<BridgeCodecRegistry, CommandEntry>>();
        public BridgeHostBuilder UseCodecs(BridgeCodecRegistry codecs) { Mutable(); this.codecs = (codecs ?? throw new ArgumentNullException(nameof(codecs))).Clone(); return this; }
        /// <summary>Explicit AOT path: DTO schema/reader/writer and handler are statically registered.</summary>
        public BridgeHostBuilder AddCommand<TArguments, TResult>(string name, Func<TArguments, CancellationToken, Task<TResult>> handler, BridgePermission permission = BridgePermission.Admin, string description = "")
        {
            Mutable(); RuntimeGuard.Name(name, nameof(name)); if (handler == null) throw new ArgumentNullException(nameof(handler));
            typedCommands.Add(registry =>
            {
                var inputCodec = registry.Get(typeof(TArguments)); var outputCodec = registry.Get(typeof(TResult));
                var input = inputCodec.Schema;
                if (!input.TryGetProperty("type", out var type) || type!.As<string>() != "object") throw new ArgumentException("Typed command arguments require an object schema.");
                var output = typeof(TResult).IsValueType ? outputCodec.Schema : BridgeSchema.Nullable(outputCodec.Schema);
                return new CommandEntry(new BridgeCommandDescriptor(name, description, permission, input, output), async (arguments, token) =>
                {
                    if (!BridgeSchema.Matches(arguments, input)) throw new BridgeBindingException("INVALID_ARGUMENT");
                    TArguments value;
                    try { value = (TArguments)inputCodec.Read(arguments)!; }
                    catch (Exception ex) when (ex is BridgeException || ex is FormatException || ex is OverflowException || ex is InvalidCastException) { throw new BridgeBindingException("INVALID_ARGUMENT"); }
                    if (value is null) throw new BridgeBindingException("INVALID_ARGUMENT");
                    token.ThrowIfCancellationRequested(); return CommandEntry.Result(await handler(value, token), outputCodec, output);
                });
            });
            return this;
        }
    }
    public sealed partial class BridgeHost
    {
        internal BridgeCommandRegistry Registry { get; }
        public IReadOnlyList<BridgeCommandDescriptor> CommandsFor(BridgeRequestContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (IsDisposed) throw new ObjectDisposedException(nameof(BridgeHost));
            return Registry.For(context);
        }
    }
}
