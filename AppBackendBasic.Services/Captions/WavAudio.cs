using System.Text;

namespace AppBackendBasic.Services.Captions;

/// <summary>Minimal PCM WAV reader/writer (replacement for Python's `wave` module use in the old pipeline).</summary>
public sealed class WavAudio
{
    public short Channels { get; init; }
    public short BitsPerSample { get; init; }
    public int SampleRate { get; init; }
    public byte[] Frames { get; init; } = Array.Empty<byte>();

    public int BytesPerFrame => (BitsPerSample / 8) * Channels;
    public int FrameCount => BytesPerFrame == 0 ? 0 : Frames.Length / BytesPerFrame;
    public double DurationSeconds => SampleRate == 0 ? 0 : (double)FrameCount / SampleRate;

    public static WavAudio Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 12 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE")
            throw new InvalidDataException("Not a RIFF/WAVE file");

        short channels = 0, bits = 0; int rate = 0; byte[]? data = null;
        int pos = 12;
        while (pos + 8 <= bytes.Length)
        {
            var id = Encoding.ASCII.GetString(bytes, pos, 4);
            long size = BitConverter.ToUInt32(bytes, pos + 4);
            int body = pos + 8;
            if (id == "fmt ")
            {
                channels = BitConverter.ToInt16(bytes, body + 2);
                rate = BitConverter.ToInt32(bytes, body + 4);
                bits = BitConverter.ToInt16(bytes, body + 14);
            }
            else if (id == "data")
            {
                // ffmpeg streaming output can leave the size as 0xFFFFFFFF; clamp to what is present.
                long available = bytes.Length - body;
                int length = (int)Math.Min(size, available);
                data = new byte[length];
                Buffer.BlockCopy(bytes, body, data, 0, length);
                break;
            }
            pos = (int)Math.Min(bytes.Length, body + size + (size & 1));
        }
        if (data == null || channels == 0 || bits == 0) throw new InvalidDataException("WAV is missing fmt/data chunks");
        return new WavAudio { Channels = channels, BitsPerSample = bits, SampleRate = rate, Frames = data };
    }

    public byte[] FrameSlice(double startSeconds, double endSeconds)
    {
        int startFrame = Math.Max(0, (int)Math.Round(startSeconds * SampleRate, MidpointRounding.ToEven));
        int endFrame = Math.Max(startFrame, (int)Math.Round(endSeconds * SampleRate, MidpointRounding.ToEven));
        int from = Math.Min(Frames.Length, startFrame * BytesPerFrame);
        int to = Math.Min(Frames.Length, endFrame * BytesPerFrame);
        var slice = new byte[Math.Max(0, to - from)];
        Buffer.BlockCopy(Frames, from, slice, 0, slice.Length);
        return slice;
    }

    public void WriteChunk(string path, params byte[][] parts)
    {
        int dataLength = parts.Sum(p => p.Length);
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + dataLength);
        w.Write(Encoding.ASCII.GetBytes("WAVE"));
        w.Write(Encoding.ASCII.GetBytes("fmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write(Channels);
        w.Write(SampleRate);
        w.Write(SampleRate * BytesPerFrame);
        w.Write((short)BytesPerFrame);
        w.Write(BitsPerSample);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(dataLength);
        foreach (var part in parts) w.Write(part);
    }
}
