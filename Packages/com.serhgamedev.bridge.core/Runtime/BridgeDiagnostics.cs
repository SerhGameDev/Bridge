#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Core
{
    public enum BridgeSeverity { Error, Warning }

    public sealed class BridgeDiagnostic
    {
        public string Code { get; }
        public BridgeSeverity Severity { get; }
        public string Message { get; }
        public string? DocumentId { get; }
        public string? Source { get; }
        public string? Path { get; }
        public int? Line { get; }
        public int? Column { get; }

        public BridgeDiagnostic(string code, string message, string? documentId = null,
            string? source = null, string? path = null, int? line = null, int? column = null,
            BridgeSeverity severity = BridgeSeverity.Error)
        {
            Code = code; Message = message; DocumentId = documentId; Source = source;
            Path = path; Line = line; Column = column; Severity = severity;
        }
        public override string ToString() => $"{Code}: {Message} ({Source ?? DocumentId ?? "input"}:{Line}:{Column}, {Path})";
    }

    public sealed class BridgeException : Exception
    {
        public IReadOnlyList<BridgeDiagnostic> Diagnostics { get; }
        public BridgeException(BridgeDiagnostic diagnostic) : this(new[] { diagnostic }) { }
        public BridgeException(IEnumerable<BridgeDiagnostic> diagnostics)
            : this(diagnostics.ToArray()) { }
        private BridgeException(BridgeDiagnostic[] diagnostics)
            : base(string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString())))
        { Diagnostics = Array.AsReadOnly(diagnostics); }
    }

    public sealed class BridgeResult<T>
    {
        private readonly T value;
        public bool Success { get; }
        public IReadOnlyList<BridgeDiagnostic> Diagnostics { get; }
        public T Value => Success ? value : throw new BridgeException(Diagnostics);
        private BridgeResult(T value, bool success, IReadOnlyList<BridgeDiagnostic> diagnostics)
        { this.value = value; Success = success; Diagnostics = diagnostics; }
        public static BridgeResult<T> Ok(T value) => new BridgeResult<T>(value, true, Array.Empty<BridgeDiagnostic>());
        public static BridgeResult<T> Fail(params BridgeDiagnostic[] diagnostics) =>
            new BridgeResult<T>(default!, false, Array.AsReadOnly((BridgeDiagnostic[])diagnostics.Clone()));
        internal static BridgeResult<T> Fail(BridgeException ex) => Fail(ex.Diagnostics.ToArray());
    }

    public sealed class BridgeReadOptions
    {
        public int MaxUtf8Bytes { get; set; } = 16 * 1024 * 1024;
        public int MaxDepth { get; set; } = 64;
        public int MaxBlocks { get; set; } = 100000;
        public int MaxNumberCharacters { get; set; } = 4096;
        internal void Validate()
        {
            if (MaxUtf8Bytes < 1 || MaxDepth < 1 || MaxBlocks < 0 || MaxNumberCharacters < 1)
                throw new ArgumentOutOfRangeException(nameof(BridgeReadOptions), "Read limits must be positive (MaxBlocks may be zero).");
        }
    }
}
