namespace Message.Web.API.Extensions;

/// <summary>
/// 将 <see cref="IAsyncEnumerable{T}"/>（byte[] 分片序列）适配为可读 <see cref="Stream"/>，
/// 用于 Results.Stream 流式转发 FileDev gRPC 下载流（避免整文件缓冲进内存）。
/// </summary>
public sealed class AsyncEnumerableStream(IAsyncEnumerable<byte[]> chunks) : Stream
{
    private readonly IAsyncEnumerator<byte[]> _enumerator = chunks.GetAsyncEnumerator();
    private byte[]? _current;
    private int _position;
    private bool _completed;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
        => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_completed)
            return 0;

        if (_current is null || _position >= _current.Length)
        {
            if (!await _enumerator.MoveNextAsync())
            {
                _completed = true;
                await _enumerator.DisposeAsync();
                return 0;
            }

            _current = _enumerator.Current;
            _position = 0;
        }

        var n = Math.Min(buffer.Length, _current.Length - _position);
        _current.AsSpan(_position, n).CopyTo(buffer.Span);
        _position += n;
        return n;
    }

    public override async Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_completed)
        {
            _enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
