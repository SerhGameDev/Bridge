#nullable enable
using System;
using System.IO;
using System.Security;

namespace Bridge.Core
{
    /// <summary>User-selected file paths. This is independent of the bounded reference resolver.</summary>
    public static class BridgeFile
    {
        private static string PathArgument(string path)
        {
            BridgeArguments.Name(path, nameof(path));
            return Path.GetFullPath(path);
        }
        public static bool Exists(string path) => File.Exists(PathArgument(path));
        public static BridgeDocument Load(string path, BridgeReadOptions? options = null)
        {
            string fullPath = PathArgument(path);
            try
            {
                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return BridgeDocument.Load(stream, options, fullPath);
            }
            catch (Exception ex) when (IsFileError(ex)) { throw Failure(ex, fullPath); }
        }
        public static BridgeResult<BridgeDocument> TryLoad(string path, BridgeReadOptions? options = null)
        {
            try { return BridgeResult<BridgeDocument>.Ok(Load(path, options)); }
            catch (BridgeException ex) { return BridgeResult<BridgeDocument>.Fail(ex); }
        }
        /// <summary>Uses the same replacement strategy as SaveAtomic.</summary>
        public static void Save(BridgeDocument document, string path, bool indented = true) => SaveAtomic(document, path, indented);
        /// <summary>Same-directory temp + File.Replace (existing file) or File.Move (new file).
        /// No delete-before-move fallback. Filesystem atomicity and power-loss durability vary by platform.</summary>
        public static void SaveAtomic(BridgeDocument document, string path, bool indented = true)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            string fullPath = PathArgument(path);
            // Finish serialization before touching the destination directory.
            byte[] bytes = InternalJson.Utf8.GetBytes(document.ToJson(indented));
            string temp = Path.Combine(Path.GetDirectoryName(fullPath)!, ".bridge-" + Guid.NewGuid().ToString("N") + ".tmp");
            bool created = false;
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = true; stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                if (File.Exists(fullPath)) File.Replace(temp, fullPath, null);
                else File.Move(temp, fullPath);
                created = false;
            }
            catch (Exception ex) when (IsFileError(ex)) { throw Failure(ex, fullPath, document.DocumentId); }
            finally
            {
                if (created)
                {
                    try { File.Delete(temp); }
                    catch (Exception ex) when (IsFileError(ex))
                    { throw Failure(ex, temp, document.DocumentId, "FILE_TEMP_CLEANUP"); }
                }
            }
        }
        public static BridgeResult<bool> TrySaveAtomic(BridgeDocument document, string path, bool indented = true)
        {
            try { SaveAtomic(document, path, indented); return BridgeResult<bool>.Ok(true); }
            catch (BridgeException ex) { return BridgeResult<bool>.Fail(ex); }
        }
        private static bool IsFileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is NotSupportedException;
        private static BridgeException Failure(Exception ex, string path, string? documentId = null, string? code = null)
            => InternalJson.Error(code ?? (ex is FileNotFoundException || ex is DirectoryNotFoundException ? "FILE_NOT_FOUND"
                : ex is UnauthorizedAccessException || ex is SecurityException ? "FILE_ACCESS" : "FILE_IO"), ex.Message, path, documentId: documentId);
    }
}
