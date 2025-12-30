using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using JSException = Microsoft.JSInterop.JSException;

namespace EzioHost.Components.SeekableInputFile.Models;

public class SeekableBrowserFileStream(
    IJSObjectReference jsModule,
    Guid fileId,
    long length,
    ILoggerFactory loggerFactory) : Stream
{
    private long _position;
    private static bool IsWasm => OperatingSystem.IsBrowser();

    private ILogger<SeekableBrowserFileStream> Logger => loggerFactory.CreateLogger<SeekableBrowserFileStream>();
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > length)
                throw new ArgumentOutOfRangeException(nameof(value));
            _position = value;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (newPos < 0 || newPos > length)
            throw new IOException("Seek position is out of bounds.");

        _position = newPos;
        return _position;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_position >= length) return 0;

        var remaining = length - _position;
        var countToRead = (int)Math.Min(buffer.Length, remaining);

        if (countToRead <= 0) return 0;


        if (!IsWasm)
            Logger.LogWarning(
                "SignalR connection may close if the read buffer exceeds the default message size limit. " +
                "Please configure 'MaximumReceiveMessageSize' accordingly. " +
                "More info: https://learn.microsoft.com/aspnet/core/signalr/security#buffer-management");


        var start = _position;
        var end = _position + countToRead;

        if (end > Length) end = Length;

        try
        {
            var bytes = await jsModule.InvokeAsync<byte[]>(
                "readSlice",
                cancellationToken,
                fileId,
                start,
                end
            );

            if (bytes.Length == 0) return 0;

            bytes.CopyTo(buffer);

            _position += bytes.Length;

            return bytes.Length;
        }
        catch (JSException ex)
        {
            throw new IOException($"Error reading file slice from JS: {ex.Message}", ex);
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Synchronous Read is not supported. Use ReadAsync.");
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}