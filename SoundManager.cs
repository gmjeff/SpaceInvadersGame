using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SpaceInvaders;

/// <summary>
/// Manages Atari 2600-style sound effects for the game.
/// Uses NAudio for reliable, low-latency audio playback.
/// </summary>
public class SoundManager : IDisposable
{
    private const int SampleRate = 44100;

    private readonly WaveOutEvent? _waveOut;
    private readonly MixingSampleProvider? _mixer;

    private readonly byte[]? _shootWav;
    private readonly byte[]? _explosionWav;
    private readonly byte[]? _playerDeathWav;
    private readonly byte[]? _ufoExplosionWav;
    private readonly byte[][]? _heartbeatWavs;

    // UFO siren - looping sound
    private LoopingSampleProvider? _ufoSiren;
    private readonly ISampleProvider? _ufoSirenSource;

    private int _currentHeartbeatNote = 0;
    private bool _disposed;
    private bool _audioAvailable;

    public SoundManager()
    {
        try
        {
            // Create mixer for combining multiple sounds
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1))
            {
                ReadFully = true
            };

            // Create output device
            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100
            };
            _waveOut.Init(_mixer);
            _waveOut.Play();

            // Generate all sounds
            _shootWav = GenerateShootSound();
            _explosionWav = GenerateExplosionSound();
            _playerDeathWav = GeneratePlayerDeathSound();
            _ufoExplosionWav = GenerateUfoExplosionSound();

            // UFO siren (loopable)
            var ufoSirenWav = GenerateUfoSirenSound();
            var ufoStream = new MemoryStream(ufoSirenWav);
            var ufoReader = new WaveFileReader(ufoStream);
            _ufoSirenSource = ufoReader.ToSampleProvider();

            // Four descending bass notes for alien march
            double[] heartbeatFrequencies = { 73.4, 69.3, 65.4, 61.7 };
            _heartbeatWavs = new byte[4][];
            for (int i = 0; i < 4; i++)
            {
                _heartbeatWavs[i] = GenerateHeartbeatSound(heartbeatFrequencies[i]);
            }

            _audioAvailable = true;
        }
        catch
        {
            // Audio initialization failed - game will run without sound
            _audioAvailable = false;
        }
    }

    private void PlaySound(byte[]? wavData)
    {
        if (!_audioAvailable || wavData == null || _mixer == null) return;
        try
        {
            var stream = new MemoryStream(wavData);
            var reader = new WaveFileReader(stream);
            var sampleProvider = reader.ToSampleProvider();
            _mixer.AddMixerInput(sampleProvider);
        }
        catch { /* Ignore audio errors */ }
    }

    /// <summary>
    /// Play the player's laser "pew" sound.
    /// </summary>
    public void PlayShoot()
    {
        PlaySound(_shootWav);
    }

    /// <summary>
    /// Play the alien explosion sound.
    /// </summary>
    public void PlayExplosion()
    {
        PlaySound(_explosionWav);
    }

    /// <summary>
    /// Play the player death explosion sound.
    /// </summary>
    public void PlayPlayerDeath()
    {
        PlaySound(_playerDeathWav);
    }

    /// <summary>
    /// Play the UFO explosion sound.
    /// </summary>
    public void PlayUfoExplosion()
    {
        PlaySound(_ufoExplosionWav);
    }

    /// <summary>
    /// Start playing the UFO siren (loops until stopped).
    /// </summary>
    public void StartUfoSiren()
    {
        if (!_audioAvailable || _mixer == null) return;
        if (_ufoSiren != null) return; // Already playing

        try
        {
            var stream = new MemoryStream(GenerateUfoSirenSound());
            var reader = new WaveFileReader(stream);
            _ufoSiren = new LoopingSampleProvider(reader.ToSampleProvider());
            _mixer.AddMixerInput(_ufoSiren);
        }
        catch { /* Ignore audio errors */ }
    }

    /// <summary>
    /// Stop playing the UFO siren.
    /// </summary>
    public void StopUfoSiren()
    {
        if (_ufoSiren != null)
        {
            _ufoSiren.Stop();
            _ufoSiren = null;
        }
    }

    /// <summary>
    /// Play the next heartbeat thump in the sequence.
    /// </summary>
    public void PlayHeartbeat()
    {
        if (!_audioAvailable || _heartbeatWavs == null) return;
        PlaySound(_heartbeatWavs[_currentHeartbeatNote]);
        _currentHeartbeatNote = (_currentHeartbeatNote + 1) % 4;
    }

    /// <summary>
    /// Reset heartbeat to start from the first note.
    /// </summary>
    public void ResetHeartbeat()
    {
        _currentHeartbeatNote = 0;
    }

    #region Sound Generation

    private byte[] GenerateShootSound()
    {
        double duration = 0.08;
        int numSamples = (int)(SampleRate * duration);
        var samples = new short[numSamples];

        double startFreq = 1200;
        double endFreq = 200;
        double amplitude = 0.3;

        double phase = 0;
        for (int i = 0; i < numSamples; i++)
        {
            double progress = (double)i / numSamples;
            double freq = startFreq * Math.Pow(endFreq / startFreq, progress);
            phase += freq / SampleRate;
            double value = phase % 1.0 < 0.5 ? 1.0 : -1.0;
            double envelope = 1.0 - progress;
            samples[i] = (short)(value * amplitude * envelope * short.MaxValue);
        }

        return CreateWavData(samples);
    }

    private byte[] GenerateExplosionSound()
    {
        double duration = 0.15;
        int numSamples = (int)(SampleRate * duration);
        var samples = new short[numSamples];

        double amplitude = 0.35;
        int lfsr = 0x1FF;

        for (int i = 0; i < numSamples; i++)
        {
            double progress = (double)i / numSamples;
            if (i % 10 == 0)
            {
                int bit = ((lfsr >> 0) ^ (lfsr >> 4)) & 1;
                lfsr = (lfsr >> 1) | (bit << 8);
            }
            double value = (lfsr & 1) == 1 ? 1.0 : -1.0;
            double envelope = Math.Pow(1.0 - progress, 2);
            samples[i] = (short)(value * amplitude * envelope * short.MaxValue);
        }

        return CreateWavData(samples);
    }

    private byte[] GeneratePlayerDeathSound()
    {
        // Longer, lower-pitched explosion with descending tone
        double duration = 0.5;
        int numSamples = (int)(SampleRate * duration);
        var samples = new short[numSamples];

        double amplitude = 0.4;
        int lfsr = 0x1FF;
        double phase = 0;

        for (int i = 0; i < numSamples; i++)
        {
            double progress = (double)i / numSamples;

            // Mix noise with descending tone
            if (i % 8 == 0)
            {
                int bit = ((lfsr >> 0) ^ (lfsr >> 4)) & 1;
                lfsr = (lfsr >> 1) | (bit << 8);
            }
            double noise = (lfsr & 1) == 1 ? 1.0 : -1.0;

            // Descending square wave
            double freq = 200 * (1.0 - progress * 0.7);
            phase += freq / SampleRate;
            double tone = phase % 1.0 < 0.5 ? 1.0 : -1.0;

            // Mix noise and tone
            double value = noise * 0.6 + tone * 0.4;

            // Decay envelope with initial burst
            double envelope = Math.Pow(1.0 - progress, 1.5);

            samples[i] = (short)(value * amplitude * envelope * short.MaxValue);
        }

        return CreateWavData(samples);
    }

    private byte[] GenerateUfoExplosionSound()
    {
        // Distinctive UFO explosion - descending "whoop" with noise
        double duration = 0.35;
        int numSamples = (int)(SampleRate * duration);
        var samples = new short[numSamples];

        double amplitude = 0.4;
        double phase = 0;
        int lfsr = 0x1FF;

        for (int i = 0; i < numSamples; i++)
        {
            double progress = (double)i / numSamples;

            // Descending tone from high to low (the "whoop")
            double freq = 800 * Math.Pow(0.15, progress); // 800Hz down to ~120Hz
            phase += freq / SampleRate;
            double tone = phase % 1.0 < 0.5 ? 1.0 : -1.0;

            // Add some noise
            if (i % 6 == 0)
            {
                int bit = ((lfsr >> 0) ^ (lfsr >> 4)) & 1;
                lfsr = (lfsr >> 1) | (bit << 8);
            }
            double noise = (lfsr & 1) == 1 ? 1.0 : -1.0;

            // Mix tone (dominant) with noise
            double value = tone * 0.7 + noise * 0.3;

            // Envelope with sustain then decay
            double envelope = progress < 0.1
                ? progress / 0.1
                : Math.Pow(1.0 - (progress - 0.1) / 0.9, 1.2);

            samples[i] = (short)(value * amplitude * envelope * short.MaxValue);
        }

        return CreateWavData(samples);
    }

    private byte[] GenerateUfoSirenSound()
    {
        // Classic warbling UFO sound - oscillating between two frequencies
        double duration = 0.3; // Short loop that repeats
        int numSamples = (int)(SampleRate * duration);
        var samples = new short[numSamples];

        double amplitude = 0.25;
        double phase = 0;
        double lfoPhase = 0;
        double lfoFreq = 8; // Warble rate

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / SampleRate;

            // LFO modulates between low and high frequency
            lfoPhase += lfoFreq / SampleRate;
            double lfo = Math.Sin(lfoPhase * 2 * Math.PI);

            // Frequency wobbles between ~400Hz and ~600Hz
            double freq = 500 + lfo * 100;

            phase += freq / SampleRate;
            double value = phase % 1.0 < 0.5 ? 1.0 : -1.0;

            samples[i] = (short)(value * amplitude * short.MaxValue);
        }

        return CreateWavData(samples);
    }

    private byte[] GenerateHeartbeatSound(double frequency)
    {
        double duration = 0.06;
        int numSamples = (int)(SampleRate * duration);
        var samples = new short[numSamples];

        double amplitude = 0.5;
        double phase = 0;

        for (int i = 0; i < numSamples; i++)
        {
            double progress = (double)i / numSamples;
            phase += frequency / SampleRate;
            double value = phase % 1.0 < 0.5 ? 1.0 : -1.0;
            double envelope = progress < 0.1
                ? progress / 0.1
                : 1.0 - ((progress - 0.1) / 0.9);
            samples[i] = (short)(value * amplitude * envelope * short.MaxValue);
        }

        return CreateWavData(samples);
    }

    private byte[] CreateWavData(short[] samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        int numChannels = 1;
        int bitsPerSample = 16;
        int byteRate = SampleRate * numChannels * bitsPerSample / 8;
        int blockAlign = numChannels * bitsPerSample / 8;
        int dataSize = samples.Length * 2;

        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)numChannels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write("data".ToCharArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            StopUfoSiren();
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Sample provider that loops audio until stopped.
/// </summary>
internal class LoopingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly byte[] _sourceData;
    private int _position;
    private bool _stopped;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public LoopingSampleProvider(ISampleProvider source)
    {
        _source = source;

        // Read all source data into memory for looping
        var samples = new List<float>();
        var buffer = new float[1024];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                samples.Add(buffer[i]);
        }

        _sourceData = new byte[samples.Count * 4];
        Buffer.BlockCopy(samples.ToArray(), 0, _sourceData, 0, _sourceData.Length);
        _position = 0;
    }

    public void Stop()
    {
        _stopped = true;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_stopped)
        {
            // Fill with silence and signal we're done
            Array.Clear(buffer, offset, count);
            return 0;
        }

        var sourceFloats = new float[_sourceData.Length / 4];
        Buffer.BlockCopy(_sourceData, 0, sourceFloats, 0, _sourceData.Length);

        int samplesWritten = 0;
        while (samplesWritten < count && !_stopped)
        {
            buffer[offset + samplesWritten] = sourceFloats[_position];
            samplesWritten++;
            _position++;
            if (_position >= sourceFloats.Length)
            {
                _position = 0; // Loop back
            }
        }

        return samplesWritten;
    }
}
