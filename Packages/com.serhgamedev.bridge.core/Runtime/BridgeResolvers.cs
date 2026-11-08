#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace Bridge.Core
{
    public sealed class InMemoryBridgeDocumentResolver : IBridgeDocumentResolver
    {
        private readonly Dictionary<string, BridgeDocument> documents;
        public StringComparer PathComparer { get; }
        public InMemoryBridgeDocumentResolver(StringComparer? pathComparer = null)
        { PathComparer = pathComparer ?? StringComparer.Ordinal; documents = new Dictionary<string, BridgeDocument>(PathComparer); }
        public void Set(string path, BridgeDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            documents[BridgePaths.Normalize(path)] = document;
        }
        public BridgeResult<BridgeDocument> Load(string documentPath)
        {
            try
            {
                string path = BridgePaths.Normalize(documentPath);
                return documents.TryGetValue(path, out var document) ? BridgeResult<BridgeDocument>.Ok(document)
                    : BridgeResult<BridgeDocument>.Fail(new BridgeDiagnostic("REFERENCE_DOCUMENT_NOT_FOUND", "Document not found.", source: path));
            }
            catch (BridgeException ex) { return BridgeResult<BridgeDocument>.Fail(ex); }
        }
    }
    /// <summary>Local files under a stable trusted root; rejects all reparse points and symbolic links.</summary>
    public sealed class FileBridgeDocumentResolver : IBridgeDocumentResolver
    {
        private readonly string root;
        private readonly BridgeReadOptions readOptions;
        public StringComparer PathComparer { get; } = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        public FileBridgeDocumentResolver(string allowedRoot, BridgeReadOptions? readOptions = null)
        {
            if (allowedRoot == null) throw new ArgumentNullException(nameof(allowedRoot));
            root = Path.GetFullPath(allowedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            this.readOptions = readOptions ?? new BridgeReadOptions(); this.readOptions.Validate();
        }
        public BridgeResult<BridgeDocument> Load(string documentPath)
        {
            try
            {
                string normalized = BridgePaths.Normalize(documentPath);
                string file = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
                if (!file.StartsWith(root, Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    throw InternalJson.Error("REFERENCE_PATH", "Path leaves the allowed root.", documentPath);
                RejectLinks(file);
                using var input = File.OpenRead(file);
                RejectLinks(file);
                return BridgeDocument.TryLoad(input, readOptions, normalized);
            }
            catch (BridgeException ex) { return BridgeResult<BridgeDocument>.Fail(ex); }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            { return BridgeResult<BridgeDocument>.Fail(new BridgeDiagnostic("REFERENCE_DOCUMENT_NOT_FOUND", ex.Message, source: documentPath)); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            { return BridgeResult<BridgeDocument>.Fail(new BridgeDiagnostic("REFERENCE_IO", ex.Message, source: documentPath)); }
        }
        private static void RejectLinks(string file)
        {
            for (string? current = file; current != null; current = Path.GetDirectoryName(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw InternalJson.Error("REFERENCE_PATH", "Symbolic links and reparse points are not allowed (including root ancestors).", current);
        }
    }
}
