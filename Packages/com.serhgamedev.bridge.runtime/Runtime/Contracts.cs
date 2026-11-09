#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;

namespace Bridge.Runtime
{
    public enum BridgePermission { None = 0, Read = 1, Control = 2, Edit = 3, Admin = 4 }
    public sealed class BridgeRequestContext
    {
        public string Principal { get; }
        public BridgePermission Permission { get; }
        public string RequestId { get; }
        public CancellationToken CancellationToken { get; }
        public BridgeRequestContext(string principal, BridgePermission permission = BridgePermission.None, string? requestId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(principal) || principal.Length > 128) throw new ArgumentException("Principal must be 1..128 characters.", nameof(principal));
            if (!Enum.IsDefined(typeof(BridgePermission), permission)) throw new ArgumentOutOfRangeException(nameof(permission));
            Principal = principal; Permission = permission; RequestId = requestId ?? Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(RequestId) || RequestId.Length > 128) throw new ArgumentException("Request ID must be 1..128 characters.", nameof(requestId));
            CancellationToken = cancellationToken;
        }
        public bool Allows(BridgePermission minimum) => Permission != BridgePermission.None && Permission >= minimum;
        public static BridgeRequestContext Local(CancellationToken cancellationToken = default) => new BridgeRequestContext("local", BridgePermission.Admin, cancellationToken: cancellationToken);
    }
    public sealed class BridgeHostOptions
    {
        public int MaxRequestBytes { get; set; } = 65536;
        public int MaxResponseBytes { get; set; } = 1048576;
        public int MaxPendingRequests { get; set; } = 64;
        public int AuditCapacity { get; set; } = 0;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        internal BridgeHostOptions Copy()
        {
            if (MaxRequestBytes < 1 || MaxResponseBytes < 1024 || MaxPendingRequests < 1 || AuditCapacity < 0 || AuditCapacity > 10000 || RequestTimeout <= TimeSpan.Zero || RequestTimeout.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(BridgeHostOptions));
            return (BridgeHostOptions)MemberwiseClone();
        }
    }
    public sealed class BridgeDataSnapshot
    {
        public BridgeDocument Document { get; }
        public string? Revision { get; }
        public DateTimeOffset CapturedAt { get; }
        public BridgeDataSnapshot(BridgeDocument document, string? revision = null)
        { Document = document ?? throw new ArgumentNullException(nameof(document)); Revision = revision; CapturedAt = DateTimeOffset.UtcNow; }
    }
    public interface IBridgeDataSource { Task<BridgeDataSnapshot> ReadAsync(CancellationToken cancellationToken); }
    public interface IBridgeDispatcher : IDisposable
    { Task<T> InvokeAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken); }
    public sealed class BridgeDispatchException : Exception
    {
        public string Code { get; }
        public BridgeDispatchException(string code) : base(code) { Code = code; }
    }
    internal static class RuntimeGuard
    {
        internal static void Name(string name, string parameter)
        {
            if (name == null) throw new ArgumentNullException(parameter);
            if (!Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_.-]{0,127}$", RegexOptions.CultureInvariant)) throw new ArgumentException("Expected ASCII identifier, 1..128 chars, starting with a letter.", parameter);
        }
    }
    internal static class Json
    {
        internal static BridgeValue Obj(params (string key, BridgeValue value)[] fields) => BridgeValue.Object(fields.Select(p => new KeyValuePair<string, BridgeValue>(p.key, p.value)));
        internal static BridgeValue Strings(IEnumerable<string> values) => BridgeValue.Array(values.Select(BridgeValue.From));
    }
}
