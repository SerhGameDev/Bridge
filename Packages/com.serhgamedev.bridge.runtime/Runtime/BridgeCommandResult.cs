#nullable enable
using System;
using Bridge.Core;

namespace Bridge.Runtime
{
    public enum BridgeCommandStatus { Completed, Queued, Rejected, Failed, Cancelled }
    /// <summary>Common request envelope for command/data/discovery. This MVP awaits completion and never emits Queued.</summary>
    public sealed class BridgeCommandResult
    {
        public BridgeCommandStatus Status { get; }
        public string RequestId { get; }
        public string OperationId { get; }
        public BridgeValue Data { get; }
        public BridgeDiagnostic? Error { get; }
        public bool Success => Status == BridgeCommandStatus.Completed;
        internal BridgeCommandResult(BridgeCommandStatus status, string requestId, string operationId, BridgeValue? data = null, string? code = null)
        {
            Status = status; RequestId = requestId; OperationId = operationId; Data = data ?? BridgeValue.Null;
            Error = code == null ? null : new BridgeDiagnostic(code, Message(code));
        }
        private static string Message(string code) => code == "NOT_FOUND" ? "Resource unavailable."
            : code == "INVALID_ARGUMENT" ? "Arguments do not match the registered contract."
            : code == "FIELD_NOT_FOUND" ? "Requested field is not present."
            : code == "REQUEST_LIMIT" ? "Request exceeds the configured limit."
            : code == "RESPONSE_LIMIT" ? "Response exceeds the configured limit."
            : code == "BUSY" ? "Request queue is full."
            : code == "HOST_STOPPED" ? "Host is stopped."
            : code == "REQUEST_TIMEOUT" ? "Request timeout elapsed; cancellation is cooperative."
            : code == "REQUEST_CANCELLED" ? "Request cancelled; side effects are not rolled back."
            : "Operation failed.";
        public BridgeValue ToValue() => Json.Obj(("status", BridgeValue.From(Status.ToString())), ("requestId", BridgeValue.From(RequestId)),
            ("operationId", BridgeValue.From(OperationId)), ("data", Data),
            ("error", Error == null ? BridgeValue.Null : Json.Obj(("code", BridgeValue.From(Error.Code)), ("message", BridgeValue.From(Error.Message)))));
        public string ToJson() => ToValue().ToJson();
    }
}
