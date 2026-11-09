#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;

namespace Bridge.Runtime
{
    internal sealed class SourceRegistration
    {
        internal readonly IBridgeDataSource Source;
        internal readonly BridgeDataPolicy Policy;
        internal SourceRegistration(IBridgeDataSource source, BridgeDataPolicy policy) { Source = source; Policy = policy; }
    }
    internal sealed class DocumentSource : IBridgeDataSource
    {
        private readonly BridgeDocument document;
        internal DocumentSource(BridgeDocument document) { this.document = document; }
        public Task<BridgeDataSnapshot> ReadAsync(CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(new BridgeDataSnapshot(document)); }
    }
    public sealed partial class BridgeHostBuilder
    {
        private readonly string name;
        private readonly Dictionary<string, SourceRegistration> sources = new Dictionary<string, SourceRegistration>(StringComparer.Ordinal);
        private readonly List<object> commandInstances = new List<object>();
        private IBridgeDispatcher? dispatcher;
        private bool ownsDispatcher, built;
        private BridgeHostOptions options = new BridgeHostOptions();
        private BridgeHostBuilder(string name) { RuntimeGuard.Name(name, nameof(name)); this.name = name; }
        public static BridgeHostBuilder Create(string name) => new BridgeHostBuilder(name);
        private void Mutable() { if (built) throw new InvalidOperationException("Builder has already built its immutable host."); }
        public BridgeHostBuilder AddDocument(string name, BridgeDocument document, BridgeDataPolicy? access = null)
            => AddDataSource(name, new DocumentSource(document ?? throw new ArgumentNullException(nameof(document))), access);
        public BridgeHostBuilder AddDataSource(string name, IBridgeDataSource source, BridgeDataPolicy? access = null)
        {
            Mutable(); RuntimeGuard.Name(name, nameof(name));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sources.ContainsKey(name)) throw new ArgumentException("Duplicate source name.", nameof(name));
            sources.Add(name, new SourceRegistration(source, access ?? BridgeDataPolicy.DenyAll())); return this;
        }
        public BridgeHostBuilder AddCommands(object instance)
        { Mutable(); commandInstances.Add(instance ?? throw new ArgumentNullException(nameof(instance))); return this; }
        public BridgeHostBuilder UseDispatcher(IBridgeDispatcher dispatcher, bool ownsDispatcher = false)
        { Mutable(); this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher)); this.ownsDispatcher = ownsDispatcher; return this; }
        public BridgeHostBuilder UseOptions(BridgeHostOptions options)
        { Mutable(); this.options = (options ?? throw new ArgumentNullException(nameof(options))).Copy(); return this; }
        public BridgeHost Build()
        {
            Mutable();
            var registry = new BridgeCommandRegistry(commandInstances, codecs.Clone(), typedCommands);
            var host = new BridgeHost(name, sources, commandInstances, dispatcher ?? new SerializedBridgeDispatcher(options.MaxPendingRequests), dispatcher == null || ownsDispatcher, options.Copy(), registry);
            built = true; return host;
        }
    }
    public sealed partial class BridgeHost : IDisposable
    {
        public string Name { get; }
        public bool IsDisposed => Volatile.Read(ref disposed) != 0;
        internal readonly IReadOnlyDictionary<string, SourceRegistration> Sources;
        internal readonly object[] CommandInstances;
        internal readonly IBridgeDispatcher Dispatcher;
        internal readonly BridgeHostOptions Options;
        internal readonly CancellationTokenSource Stop = new CancellationTokenSource();
        private readonly bool ownsDispatcher;
        private int disposed;
        internal BridgeHost(string name, IDictionary<string, SourceRegistration> sources, IEnumerable<object> commands, IBridgeDispatcher dispatcher, bool ownsDispatcher, BridgeHostOptions options, BridgeCommandRegistry registry)
        {
            Name = name; Sources = new ReadOnlyDictionary<string, SourceRegistration>(new Dictionary<string, SourceRegistration>(sources, StringComparer.Ordinal));
            CommandInstances = commands.ToArray(); Dispatcher = dispatcher; this.ownsDispatcher = ownsDispatcher; Options = options; Registry = registry;
        }
        public void Dispose()
        { if (Interlocked.Exchange(ref disposed, 1) == 0) { try { Stop.Cancel(); } finally { if (ownsDispatcher) Dispatcher.Dispose(); } } }
    }
}
