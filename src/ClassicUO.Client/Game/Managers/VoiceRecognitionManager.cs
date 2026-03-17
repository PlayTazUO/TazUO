using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;
using SDL3;
using Vosk;

namespace ClassicUO.Game.Managers
{
    public sealed class VoiceRecognitionManager
    {
        public static VoiceRecognitionManager Instance { get; } = new();

        // SDL_AUDIO_DEVICE_DEFAULT_RECORDING = 0xFFFFFFFE
        private const uint SDL_AUDIO_DEVICE_DEFAULT_RECORDING = 0xFFFFFFFE;
        private const int SAMPLE_RATE = 16000;
        private const int CHANNELS = 1;
        private const int POLL_BUFFER_SIZE = 8192; // bytes (~256ms of audio)

        private Model _model;
        private VoskRecognizer _recognizer;
        private IntPtr _audioStream = IntPtr.Zero;
        private Thread _processingThread;
        private volatile bool _processingRunning;
        private volatile bool _isListening;
        private volatile bool _initialized;
        private volatile bool _initializing;

        private readonly ConcurrentQueue<string> _recognizedTexts = new();
        private readonly ConcurrentQueue<string> _statusMessages = new();

        public event Action<string> TextRecognized;
        public event Action<string> StatusMessage;
        public event Action InitializationComplete;

        public bool IsListening => _isListening;
        public bool IsInitialized => _initialized;
        public bool IsInitializing => _initializing;

        private VoiceRecognitionManager() { }

        /// <summary>
        /// Loads the Vosk model on a background thread so the game doesn't freeze.
        /// Optionally starts listening once ready.
        /// </summary>
        public void InitializeAsync(string modelPath, bool startListeningAfter = false)
        {
            if (_initialized || _initializing)
                return;

            if (string.IsNullOrEmpty(modelPath))
            {
                Log.Warn("[VoiceRecognition] Model path is empty");
                return;
            }

            bool isZip = modelPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && File.Exists(modelPath);
            if (!isZip && !Directory.Exists(modelPath))
            {
                Log.Warn($"[VoiceRecognition] Model path not found: {modelPath}");
                return;
            }

            _initializing = true;
            new Thread(() =>
            {
                try
                {
                    string resolvedPath = isZip ? ExtractZipModel(modelPath) : modelPath;
                    if (resolvedPath == null)
                        return;

                    Vosk.Vosk.SetLogLevel(-1);
                    _model = new Model(resolvedPath);
                    _recognizer = new VoskRecognizer(_model, (float)SAMPLE_RATE);
                    _initialized = true;
                    Log.Info("[VoiceRecognition] Initialized successfully");

                    if (startListeningAfter)
                        StartListening();

                    InitializationComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error($"[VoiceRecognition] Failed to initialize: {ex.Message}");
                    _recognizer?.Dispose();
                    _recognizer = null;
                    _model?.Dispose();
                    _model = null;
                    _initialized = false;
                }
                finally
                {
                    _initializing = false;
                }
            })
            {
                IsBackground = true,
                Name = "VoiceRecognitionInit"
            }.Start();
        }

        public void Reinitialize()
        {
            if (_initializing)
                return;

            if (_isListening)
                StopListening();

            _recognizer?.Dispose();
            _model?.Dispose();
            _recognizer = null;
            _model = null;
            _initialized = false;

            Profile profile = ProfileManager.CurrentProfile;
            if (profile != null)
                InitializeAsync(profile.VoiceModelPath, profile.VoiceRecognitionEnabled);
        }

        public void StartListening()
        {
            if (!_initialized || _isListening)
                return;

            try
            {
                var spec = new SDL.SDL_AudioSpec
                {
                    format = SDL.SDL_AudioFormat.SDL_AUDIO_S16LE,
                    channels = CHANNELS,
                    freq = SAMPLE_RATE
                };

                _audioStream = SDL.SDL_OpenAudioDeviceStream(
                    SDL_AUDIO_DEVICE_DEFAULT_RECORDING,
                    ref spec,
                    null,
                    IntPtr.Zero
                );

                if (_audioStream == IntPtr.Zero)
                {
                    Log.Error("[VoiceRecognition] Failed to open audio device stream");
                    return;
                }

                _recognizer.Reset();
                _processingRunning = true;
                _processingThread = new Thread(ProcessingLoop)
                {
                    IsBackground = true,
                    Name = "VoiceRecognition"
                };
                _processingThread.Start();

                SDL.SDL_ResumeAudioStreamDevice(_audioStream);
                _isListening = true;
                _statusMessages.Enqueue("[Voice] Listening...");
                Log.Info("[VoiceRecognition] Started listening");
            }
            catch (Exception ex)
            {
                Log.Error($"[VoiceRecognition] Error starting: {ex.Message}");
                _processingRunning = false;
                if (_processingThread != null && _processingThread.IsAlive)
                {
                    _processingThread.Join(2000);
                }
                _processingThread = null;
                CleanupStream();
            }
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            _isListening = false;
            _processingRunning = false;

            _processingThread?.Join(2000);
            _processingThread = null;

            CleanupStream();

            // Get any remaining text
            if (_initialized && _recognizer != null)
            {
                try
                {
                    string finalResult = _recognizer.FinalResult();
                    string text = ExtractText(finalResult, "text");
                    if (!string.IsNullOrWhiteSpace(text))
                        _recognizedTexts.Enqueue(text);
                }
                catch { }
            }

            Log.Info("[VoiceRecognition] Stopped listening");
        }

        public void ToggleListening()
        {
            if (_isListening)
                StopListening();
            else
                StartListening();
        }

        private static string ExtractZipModel(string zipPath)
        {
            try
            {
                string voskDir = Path.Combine(CUOEnviroment.ExecutablePath, "vosk");
                Directory.CreateDirectory(voskDir);

                // Vosk zips contain a single top-level folder with the same name as the zip
                string modelDirName = Path.GetFileNameWithoutExtension(zipPath);
                string modelDir = Path.Combine(voskDir, modelDirName);

                if (!Directory.Exists(modelDir))
                {
                    Log.Info($"[VoiceRecognition] Extracting {zipPath} ...");
                    ZipFile.ExtractToDirectory(zipPath, voskDir);
                }

                if (!Directory.Exists(modelDir))
                {
                    Log.Error($"[VoiceRecognition] Expected model directory not found after extraction: {modelDir}");
                    return null;
                }

                return modelDir;
            }
            catch (Exception ex)
            {
                Log.Error($"[VoiceRecognition] Failed to extract zip: {ex.Message}");
                return null;
            }
        }

        private void CleanupStream()
        {
            if (_audioStream != IntPtr.Zero)
            {
                SDL.SDL_PauseAudioStreamDevice(_audioStream);
                SDL.SDL_DestroyAudioStream(_audioStream);
                _audioStream = IntPtr.Zero;
            }
        }

        private void ProcessingLoop()
        {
            IntPtr nativeBuffer = Marshal.AllocHGlobal(POLL_BUFFER_SIZE);
            try
            {
                while (_processingRunning)
                {
                    IntPtr stream = _audioStream;
                    if (stream == IntPtr.Zero)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    int available = SDL.SDL_GetAudioStreamAvailable(stream);
                    if (available >= 2)
                    {
                        int toRead = Math.Min(available, POLL_BUFFER_SIZE) & ~1;
                        int read = SDL.SDL_GetAudioStreamData(stream, nativeBuffer, toRead);
                        if (read > 0)
                        {
                            byte[] managed = new byte[read];
                            Marshal.Copy(nativeBuffer, managed, 0, read);
                            ProcessAudioChunk(managed, read);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[VoiceRecognition] Processing error: {ex.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(nativeBuffer);
            }
        }

        private void ProcessAudioChunk(byte[] buffer, int length)
        {
            try
            {
                if (_recognizer.AcceptWaveform(buffer, length))
                {
                    string result = _recognizer.Result();
                    string text = ExtractText(result, "text");
                    if (!string.IsNullOrWhiteSpace(text))
                        _recognizedTexts.Enqueue(text);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[VoiceRecognition] Waveform processing error: {ex.Message}");
            }
        }

        private static string ExtractText(string json, string field)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(field, out JsonElement element))
                    return element.GetString();
            }
            catch { }
            return null;
        }

        // Call from main thread each frame
        public void Update()
        {
            while (_statusMessages.TryDequeue(out string msg))
                StatusMessage?.Invoke(msg);

            while (_recognizedTexts.TryDequeue(out string text))
                TextRecognized?.Invoke(text);
        }

        public void Dispose()
        {
            StopListening();
            _recognizer?.Dispose();
            _model?.Dispose();
            _recognizer = null;
            _model = null;
            _initialized = false;
        }
    }
}
