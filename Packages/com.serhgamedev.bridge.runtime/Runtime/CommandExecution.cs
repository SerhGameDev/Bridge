#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;

namespace Bridge.Runtime
{
    internal sealed class BridgeHandlerException : Exception
    {
        internal string HandlerType { get; }
        internal BridgeHandlerException(string handlerType) : base("Handler failed.") { HandlerType = handlerType; }
    }
    internal static class BridgeCommandExecutor
    {
        internal static async Task<BridgeValue> Execute(CommandEntry command, BridgeValue arguments, CancellationToken token)
        {
            try { return await command.Invoke(arguments, token); }
            catch (BridgeBindingException) { throw; }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is AccessViolationException)) { throw new BridgeHandlerException(ex.GetType().FullName ?? "Exception"); }
        }
    }
    public sealed class BridgeAuditEntry
    {
        public string Principal { get; }
        public string RequestId { get; }
        public string OperationId { get; }
        public string Operation { get; }
        public BridgeCommandStatus Status { get; }
        public string? ErrorCode { get; }
        public string? ExceptionType { get; }
        public DateTimeOffset Timestamp { get; }
        internal BridgeAuditEntry(BridgeRequestContext context, BridgeCommandResult result, string operation, string? exceptionType)
        {
            Principal = context.Principal; RequestId = result.RequestId; OperationId = result.OperationId; Operation = operation;
            Status = result.Status; ErrorCode = result.Error?.Code; ExceptionType = exceptionType; Timestamp = DateTimeOffset.UtcNow;
        }
    }
    public sealed partial class BridgeHost
    {
        private readonly object auditLock = new object();
        private readonly Queue<BridgeAuditEntry> audit = new Queue<BridgeAuditEntry>();
        public IReadOnlyList<BridgeAuditEntry> GetAuditSnapshot() { lock (auditLock) return Array.AsReadOnly(audit.ToArray()); }
        public Task<BridgeCommandResult> ExecuteAsync(string command, BridgeValue arguments, BridgeRequestContext context)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            if (context == null) throw new ArgumentNullException(nameof(context));
            bool visible = command.Length <= 128 && Registry.TryGet(command, out var found) && context.Allows(found.Descriptor.Permission);
            string auditName = visible ? command : "execute";
            return Respond(context, token =>
            {
                if (!visible || !Registry.TryGet(command, out var entry)) throw new BridgeBindingException("NOT_FOUND");
                return BridgeCommandExecutor.Execute(entry, arguments, token);
            }, arguments, auditName);
        }
        public Task<BridgeCommandResult> ExecuteLocalAsync(string command, BridgeValue arguments, CancellationToken cancellationToken = default)
            => ExecuteAsync(command, arguments, BridgeRequestContext.Local(cancellationToken));
    }
}
