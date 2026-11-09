namespace Bridge.Transport.Mcp;
// Bound each SDK stdio JSON line before its parser accumulates an unbounded message.
sealed class BoundedInputStream : Stream
{
    readonly Stream inner;readonly int limit;int length;
    public BoundedInputStream(Stream inner,int limit){this.inner=inner;this.limit=limit;}
    void Check(ReadOnlySpan<byte> data){foreach(byte b in data){if(b==10)length=0;else if(++length>limit)throw new IOException("MCP input message limit exceeded.");}}
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default){int n=await inner.ReadAsync(buffer,cancellationToken);Check(buffer.Span[..n]);return n;}
    public override async Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken cancellationToken){int n=await inner.ReadAsync(buffer,offset,count,cancellationToken);Check(buffer.AsSpan(offset,n));return n;}
    public override int Read(byte[] buffer,int offset,int count){int n=inner.Read(buffer,offset,count);Check(buffer.AsSpan(offset,n));return n;}
    public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
    public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
    public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
}
