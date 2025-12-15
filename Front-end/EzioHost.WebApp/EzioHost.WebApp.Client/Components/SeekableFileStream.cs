using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components;

internal sealed class SeekableFileStream : Stream, IAsyncDisposable
{
    private readonly DotNetObjectReference<SeekableFileStream> _dotNetReference;
    private readonly int _fileIndex;
    private readonly string _inputFileId;
    private readonly Lazy<Task<IJSObjectReference>> _jsModuleTask;
    private readonly IJSRuntime _jsRuntime;
    private byte[] _buffer;
    private int _bufferOffset;
    private int _bufferSize;
    private IJSObjectReference? _jsModule;
    private long _position;
    private TaskCompletionSource<int>? _receiveFileSliceCompletionSource;

    public SeekableFileStream(IJSRuntime jsRuntime, string inputFileId, int fileIndex, long size)
    {
        _jsRuntime = jsRuntime;
        _jsModuleTask = new Lazy<Task<IJSObjectReference>>(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/SeekableFileStream.js").AsTask());
        _position = 0;
        _buffer = [];
        _bufferOffset = 0;
        _bufferSize = 0;
        _dotNetReference = DotNetObjectReference.Create(this);
        _inputFileId = inputFileId;
        _fileIndex = fileIndex;
        Length = size;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > Length) throw new ArgumentOutOfRangeException(nameof(value));
            _position = value;
            _buffer = [];
            _bufferOffset = 0;
            _bufferSize = 0;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _dotNetReference.Dispose();
        if (_jsModuleTask.IsValueCreated && _jsModule != null) await _jsModule.DisposeAsync();
        await base.DisposeAsync();
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentException($"Invalid {nameof(origin)}: {origin}", nameof(origin))
        };

        if (newPosition < 0 || newPosition > Length)
            throw new IOException("Attempted to seek outside the bounds of the stream.");

        Position = newPosition;
        return Position;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_position >= Length) return 0;

        if (_bufferOffset < _bufferSize)
        {
            var bytesToCopy = Math.Min(count, _bufferSize - _bufferOffset);
            Array.Copy(_buffer, _bufferOffset, buffer, offset, bytesToCopy);
            _bufferOffset += bytesToCopy;
            _position += bytesToCopy;
            return bytesToCopy;
        }

        var bytesToRead = Math.Min(count, (int)(Length - _position));
        if (bytesToRead <= 0) return 0;
        _receiveFileSliceCompletionSource = new TaskCompletionSource<int>();

        _jsModule ??= await _jsModuleTask.Value;
        await _jsModule.InvokeVoidAsync("readFileSlice", cancellationToken, _inputFileId, _fileIndex, _position,
            bytesToRead, _dotNetReference);
        // Wait for the JavaScript callback to populate the buffer
        await _receiveFileSliceCompletionSource.Task;
        _receiveFileSliceCompletionSource = null; // Reset for the next read

        if (_bufferOffset < _bufferSize)
        {
            var bytesCopied = Math.Min(count, _bufferSize - _bufferOffset);
            Array.Copy(_buffer, _bufferOffset, buffer, offset, bytesCopied);
            _bufferOffset += bytesCopied;
            _position += bytesCopied;
            return bytesCopied;
        }

        return 0;
    }

    [JSInvokable]
    public void ReceiveFileSlice(long offset, byte[] data)
    {
        if (offset != _position || _receiveFileSliceCompletionSource == null) return;

        _buffer = data;
        _bufferOffset = 0;
        _bufferSize = data.Length;
        _receiveFileSliceCompletionSource.SetResult(data.Length);
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _dotNetReference.Dispose();
        base.Dispose(disposing);
    }
}