#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;

namespace Bridge.Runtime
{
    public sealed partial class BridgeHost
    {
        private int pending;
        internal static int Bytes(BridgeValue value) => Encoding.UTF8.GetByteCount(value.ToJson());
        internal async Task<BridgeCommandResult> Respond(BridgeRequestContext context, Func<CancellationToken, Task<BridgeValue>> action, BridgeValue? payload = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            string operationId = Guid.NewGuid().ToString("N");
            BridgeCommandResult Error(BridgeCommandStatus status, string code) => new BridgeCommandResult(status, context.RequestId, operationId, code: code);
            if (IsDisposed) return Error(BridgeCommandStatus.Rejected, "HOST_STOPPED");
            if (Interlocked.Increment(ref pending) > Options.MaxPendingRequests) { Interlocked.Decrement(ref pending); return Error(BridgeCommandStatus.Rejected, "BUSY"); }
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, Stop.Token);
            linked.CancelAfter(Options.RequestTimeout);
            try
            {
                if (payload != null && Bytes(payload) > Options.MaxRequestBytes) return Error(BridgeCommandStatus.Rejected, "REQUEST_LIMIT");
                var data = await Dispatcher.InvokeAsync(action, linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                var result = new BridgeCommandResult(BridgeCommandStatus.Completed, context.RequestId, operationId, data);
                return Bytes(result.ToValue()) > Options.MaxResponseBytes ? Error(BridgeCommandStatus.Failed, "RESPONSE_LIMIT") : result;
            }
            catch (OperationCanceledException) { return Error(BridgeCommandStatus.Cancelled, IsDisposed ? "HOST_STOPPED" : context.CancellationToken.IsCancellationRequested ? "REQUEST_CANCELLED" : "REQUEST_TIMEOUT"); }
            catch (BridgeBindingException ex) { return Error(ex.Code == "INVALID_RESULT" ? BridgeCommandStatus.Failed : BridgeCommandStatus.Rejected, ex.Code); }
            catch (BridgeDispatchException) { return Error(BridgeCommandStatus.Rejected, "BUSY"); }
            catch (ObjectDisposedException) { return Error(BridgeCommandStatus.Rejected, "HOST_STOPPED"); }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is AccessViolationException)) { return Error(BridgeCommandStatus.Failed, "OPERATION_FAILED"); }
            finally { Interlocked.Decrement(ref pending); }
        }
        public Task<BridgeCommandResult> DiscoverAsync(BridgeRequestContext context)
            => Respond(context, _ =>
            {
                var commands = Registry.For(context).Select(c => Json.Obj(("name", BridgeValue.From(c.Name)), ("description", BridgeValue.From(c.Description)), ("inputSchema", c.InputSchema), ("outputSchema", c.OutputSchema)));
                var sources = Sources.Where(s => context.Allows(BridgePermission.Read) && s.Value.Policy.HasReadSurface).OrderBy(s => s.Key, StringComparer.Ordinal)
                    .Select(s => Json.Obj(("name", BridgeValue.From(s.Key)), ("types", BridgeValue.Array(s.Value.Policy.Fields.Where(f => s.Value.Policy.Types.Contains(f.Key) && f.Value.Length > 0).OrderBy(f => f.Key, StringComparer.Ordinal)
                        .Select(f => Json.Obj(("type", BridgeValue.From(f.Key)), ("fields", Json.Strings(f.Value))))))));
                return Task.FromResult(Json.Obj(("commands", BridgeValue.Array(commands)), ("sources", BridgeValue.Array(sources))));
            });
        public Task<BridgeCommandResult> QueryAsync(string source, BridgeQuerySpec spec, BridgeRequestContext context)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            return Respond(context, async token =>
            {
                if (!context.Allows(BridgePermission.Read) || !Sources.TryGetValue(source, out var registration) || !registration.Policy.HasReadSurface) throw new BridgeBindingException("NOT_FOUND");
                var policy = registration.Policy;
                if (Encoding.UTF8.GetByteCount(spec.ToJson()) > policy.MaxQueryBytes) throw new BridgeBindingException("REQUEST_LIMIT");
                var types = spec.Types ?? policy.Types.Where(t => policy.Fields.TryGetValue(t, out var f) && f.Length > 0).ToArray();
                if (types.Any(t => !policy.Types.Contains(t) || !policy.Fields.TryGetValue(t, out var fields) || fields.Length == 0)
                    || spec.Id != null && policy.Blocks != null && !policy.Blocks.Contains(spec.Id)) throw new BridgeBindingException("NOT_FOUND");
                if (spec.Fields != null && types.Any(t => spec.Fields.Any(f => !policy.Fields[t].Contains(f, StringComparer.Ordinal)))) throw new BridgeBindingException("NOT_FOUND");
                var snapshot = await registration.Source.ReadAsync(token);
                if (snapshot == null) throw new InvalidOperationException("Source returned no snapshot.");
                var document = snapshot.Document;
                bool Allowed(BridgeBlock b) => policy.Types.Contains(b.Type) && policy.Fields.TryGetValue(b.Type, out var fields) && fields.Length > 0 && (policy.Blocks == null || policy.Blocks.Contains(b.Id));
                if (spec.Id != null && (!document.TryGetBlock(spec.Id, out var target) || !Allowed(target!))) throw new BridgeBindingException("NOT_FOUND");
                var result = BridgeDocument.Create(source); // Public name only: no original documentId/source/revision/extensions/refs.
                int limit = Math.Min(spec.Limit ?? policy.MaxBlocks, policy.MaxBlocks);
                var selected = document.Blocks.Where(b => Allowed(b) && types.Contains(b.Type, StringComparer.Ordinal) && (spec.Id == null || b.Id == spec.Id)).Take(limit);
                foreach (var block in selected)
                {
                    token.ThrowIfCancellationRequested(); var data = block.Data;
                    if (data.Kind != BridgeValueKind.Object) throw new InvalidOperationException("Policy requires object data.");
                    var projected = new Dictionary<string, BridgeValue>(StringComparer.Ordinal);
                    foreach (string field in spec.Fields ?? policy.Fields[block.Type])
                    {
                        if (data.TryGetProperty(field, out var value)) projected.Add(field, value!);
                        else if (spec.Fields != null) throw new BridgeBindingException("FIELD_NOT_FOUND");
                    }
                    result.AddBlock(block.Id, block.Type, BridgeValue.Object(projected));
                    if (Encoding.UTF8.GetByteCount(result.ToJson()) > policy.MaxResponseBytes) throw new BridgeBindingException("RESPONSE_LIMIT");
                }
                var projection = BridgeValue.Parse(result.ToJson());
                if (Bytes(projection) > policy.MaxResponseBytes) throw new BridgeBindingException("RESPONSE_LIMIT");
                return projection;
            }, BridgeValue.Parse(spec.ToJson()));
        }
    }
}
