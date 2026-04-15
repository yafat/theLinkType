using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace theLinkType.Services;

public sealed class AudioRecorderService : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _memoryStream;
    private WaveFileWriter? _waveWriter;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public Task StartAsync()
    {
        if (_isRecording) return Task.CompletedTask;

        _memoryStream = new MemoryStream();

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 100
        };

        _waveWriter = new WaveFileWriter(_memoryStream, _waveIn.WaveFormat);

        _waveIn.DataAvailable += (_, e) =>
        {
            _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
            _waveWriter?.Flush();
        };

        _waveIn.RecordingStopped += (_, _) =>
        {
            _waveWriter?.Flush();
        };

        _waveIn.StartRecording();
        _isRecording = true;

        return Task.CompletedTask;
    }

    public Task<byte[]> StopAndGetWavBytesAsync()
    {
        if (!_isRecording || _waveIn is null || _memoryStream is null || _waveWriter is null)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;

        _waveWriter.Flush();
        _waveWriter.Dispose();
        _waveWriter = null;

        _isRecording = false;

        byte[] bytes = _memoryStream.ToArray();
        _memoryStream.Dispose();
        _memoryStream = null;

        return Task.FromResult(bytes);
    }

    public void Dispose()
    {
        _waveIn?.Dispose();
        _waveWriter?.Dispose();
        _memoryStream?.Dispose();
    }
}