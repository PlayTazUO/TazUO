// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Utility;
using ClassicUO.Configuration;
using ClassicUO.IO.Audio;
using ClassicUO.Assets;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.Game.Managers
{
    public sealed class AudioManager
    {
        const float SOUND_DELTA = 250;

        private bool _canReproduceAudio = true;
        private bool _audioDeviceDisconnected = false;
        private uint _lastAudioRecoveryAttempt = 0;
        private const uint AUDIO_RECOVERY_DELAY = 1000; // 1 second delay between recovery attempts
        private readonly LinkedList<UOSound> _currentSounds = new LinkedList<UOSound>();
        private readonly UOMusic[] _currentMusic = { null, null };
        private readonly int[] _currentMusicIndices = { 0, 0 };
        public int LoginMusicIndex { get; private set; }
        public int DeathMusicIndex { get; } = 42;

        public void Initialize()
        {
            try
            {
                if(!System.Diagnostics.Debugger.IsAttached)
                    new DynamicSoundEffectInstance(0, AudioChannels.Mono).Dispose();
                else //Fix for rider debugging not having audio apparently
                    _canReproduceAudio = false;
            }
            catch (NoAudioHardwareException ex)
            {
                Log.Warn(ex.ToString());
                _canReproduceAudio = false;
            }

            LoginMusicIndex = Client.Game.UO.Version switch
            {
                >= ClientVersion.CV_7000 => 78, // LoginLoop
                > ClientVersion.CV_308Z => 0,
                _ => 8 // stones2
            };

            Client.Game.Activated += OnWindowActivated;
            Client.Game.Deactivated += OnWindowDeactivated;
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            if (!_canReproduceAudio || _audioDeviceDisconnected || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            try
            {
                SoundEffect.MasterVolume = 0;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to set master volume on window deactivation: {ex.Message}");
                _audioDeviceDisconnected = true;
            }
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            if (_audioDeviceDisconnected)
            {
                TryImmediateFallback();
                return;
            }

            try
            {
                SoundEffect.MasterVolume = 1;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to set master volume on window activation: {ex.Message}");
                _audioDeviceDisconnected = true;
                TryImmediateFallback();
            }
        }

        public void PlaySound(int index)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (!_canReproduceAudio || _audioDeviceDisconnected || currentProfile == null)
            {
                return;
            }

            float volume = currentProfile.SoundVolume / SOUND_DELTA;

            if (Client.Game.IsActive)
            {
                if (!currentProfile.ReproduceSoundsInBackground)
                {
                    volume = currentProfile.SoundVolume / SOUND_DELTA;
                }
            }
            else if (!currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (!currentProfile.EnableSound || !Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            var sound = (UOSound) Client.Game.UO.Sounds.GetSound(index);

            if (sound != null)
            {
                try
                {
                    if (sound.Play(Time.Ticks, volume))
                    {
                        sound.X = -1;
                        sound.Y = -1;
                        sound.CalculateByDistance = false;

                        _currentSounds.AddLast(sound);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to play sound {index}: {ex.Message}");
                    _audioDeviceDisconnected = true;
                }
            }
        }

        public void PlaySoundWithDistance(World world, int index, int x, int y)
        {
            if (!_canReproduceAudio || _audioDeviceDisconnected || !world.InGame)
            {
                return;
            }

            int distX = Math.Abs(x - world.Player.X);
            int distY = Math.Abs(y - world.Player.Y);
            int distance = Math.Max(distX, distY);

            Profile currentProfile = ProfileManager.CurrentProfile;
            float volume = currentProfile.SoundVolume / SOUND_DELTA;
            float distanceFactor = 0.0f;

            if (distance >= 1)
            {
                float volumeByDist = volume / (world.ClientViewRange + 1);
                distanceFactor = volumeByDist * distance;
            }

            if (distance > world.ClientViewRange)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (currentProfile == null || !currentProfile.EnableSound || !Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            var sound = (UOSound)Client.Game.UO.Sounds.GetSound(index);

            if (sound != null)
            {
                try
                {
                    if (sound.Play(Time.Ticks, volume, distanceFactor))
                    {
                        sound.X = x;
                        sound.Y = y;
                        sound.CalculateByDistance = true;

                        _currentSounds.AddLast(sound);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to play sound {index} with distance: {ex.Message}");
                    _audioDeviceDisconnected = true;
                }
            }
        }

        public void PlayMusic(int music, bool iswarmode = false, bool is_login = false)
        {
            if (!_canReproduceAudio || _audioDeviceDisconnected)
            {
                return;
            }

            if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
            {
                return;
            }

            float volume;

            if (is_login)
            {
                volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
            }
            else
            {
                Profile currentProfile = ProfileManager.CurrentProfile;

                if (currentProfile == null || !currentProfile.EnableMusic)
                {
                    volume = 0;
                }
                else
                {
                    volume = currentProfile.MusicVolume / SOUND_DELTA;
                }

                if (currentProfile != null && !currentProfile.EnableCombatMusic && iswarmode)
                {
                    return;
                }
            }


            if (volume < -1 || volume > 1f)
            {
                return;
            }

            Sound m = Client.Game.UO.Sounds.GetMusic(music);

            if (m == null && _currentMusic[0] != null)
            {
                StopMusic();
            }
            else if (m != null && (m != _currentMusic[0] || iswarmode))
            {
                StopMusic();

                int idx = iswarmode ? 1 : 0;
                _currentMusicIndices[idx] = music;
                _currentMusic[idx] = (UOMusic) m;

                try
                {
                    _currentMusic[idx].Play(Time.Ticks, volume);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to play music {music}: {ex.Message}");
                    _audioDeviceDisconnected = true;
                    _currentMusic[idx] = null;
                    _currentMusicIndices[idx] = 0;
                }
            }
        }

        public void UpdateCurrentMusicVolume(bool isLogin = false)
        {
            if (!_canReproduceAudio || _audioDeviceDisconnected)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    float volume;

                    if (isLogin)
                    {
                        volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
                    }
                    else
                    {
                        Profile currentProfile = ProfileManager.CurrentProfile;

                        volume = currentProfile == null || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                    }


                    if (volume < -1 || volume > 1f)
                    {
                        return;
                    }

                    try
                    {
                        _currentMusic[i].Volume = i == 0 && _currentMusic[1] != null ? 0 : volume;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Failed to set music volume: {ex.Message}");
                        _audioDeviceDisconnected = true;
                    }
                }
            }
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_canReproduceAudio || _audioDeviceDisconnected)
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            float volume = currentProfile == null || !currentProfile.EnableSound ? 0 : currentProfile.SoundVolume / SOUND_DELTA;

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            for (LinkedListNode<UOSound> soundNode = _currentSounds.First; soundNode != null; soundNode = soundNode.Next)
            {
                try
                {
                    soundNode.Value.Volume = volume;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to set sound volume: {ex.Message}");
                    _audioDeviceDisconnected = true;
                    break;
                }
            }
        }

        public void StopMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    _currentMusic[i].Stop();
                    _currentMusic[i].Dispose();
                    _currentMusic[i] = null;
                }
            }
        }

        public void StopWarMusic() => PlayMusic(_currentMusicIndices[0]);

        public void StopSounds()
        {
            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                first.Value.Stop();

                _currentSounds.Remove(first);

                first = next;
            }
        }

        public void Update()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            if (_audioDeviceDisconnected)
            {
                TryRecoverAudio();
                if (_audioDeviceDisconnected)
                {
                    return;
                }
            }

            CheckAudioDeviceHealth();

            bool runninWarMusic = _currentMusic[1] != null;
            Profile currentProfile = ProfileManager.CurrentProfile;

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && currentProfile != null)
                {
                    if (Client.Game.IsActive)
                    {
                        if (!currentProfile.ReproduceSoundsInBackground)
                        {
                            _currentMusic[i].Volume = i == 0 && runninWarMusic || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                        }
                    }
                    else if (!currentProfile.ReproduceSoundsInBackground && _currentMusic[i].Volume != 0.0f)
                    {
                        _currentMusic[i].Volume = 0;
                    }
                }

                _currentMusic[i]?.Update();
            }


            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                if (!first.Value.IsPlaying(Time.Ticks))
                {
                    first.Value.Stop();
                    _currentSounds.Remove(first);
                }

                first = next;
            }
        }

        public UOMusic GetCurrentMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && _currentMusic[i].IsPlaying(Time.Ticks))
                {
                    return _currentMusic[i];
                }
            }
            return null;
        }

        public void OnAudioDeviceAdded()
        {
            if (_audioDeviceDisconnected && _canReproduceAudio)
            {
                Log.Info("Audio device added - attempting immediate recovery...");
                TryImmediateFallback();
            }
            else if (_canReproduceAudio)
            {
                Log.Info("Audio device added while audio is working - system has more audio options available");
            }
        }

        public void OnAudioDeviceRemoved()
        {
            if (_canReproduceAudio)
            {
                Log.Warn("Audio device removed - attempting immediate fallback to alternative device");
                _audioDeviceDisconnected = true;

                StopAllAudio();

                TryImmediateFallback();
            }
        }

        private void TryImmediateFallback()
        {
            Log.Info("Attempting immediate fallback to available audio device...");

            if (TryCreateAudioInstance())
            {
                _audioDeviceDisconnected = false;
                Log.Info("Immediate audio fallback successful!");
                RestoreCurrentMusic();
            }
            else
            {
                Log.Warn("Immediate audio fallback failed - no alternative device available");
                _lastAudioRecoveryAttempt = Time.Ticks;
            }
        }

        private void TryRecoverAudio()
        {
            if (Time.Ticks - _lastAudioRecoveryAttempt < AUDIO_RECOVERY_DELAY)
            {
                return;
            }

            _lastAudioRecoveryAttempt = Time.Ticks;

            Log.Info("Attempting audio recovery...");

            if (TryCreateAudioInstance())
            {
                _audioDeviceDisconnected = false;
                Log.Info("Audio recovery successful!");
                RestoreCurrentMusic();
            }
            else
            {
                Log.Warn("Audio recovery failed - no hardware available");
            }
        }

        private bool TryCreateAudioInstance()
        {
            try
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        var testInstance = new DynamicSoundEffectInstance(22050, AudioChannels.Mono);
                        testInstance.Dispose();

                        Log.Info($"Audio device test successful on attempt {attempt + 1}");
                        return true;
                    }
                    catch (NoAudioHardwareException) when (attempt < 2)
                    {
                        Log.Warn($"Audio test attempt {attempt + 1} failed - trying again...");
                    }
                }
                return false;
            }
            catch (NoAudioHardwareException ex)
            {
                Log.Warn($"No audio hardware available: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Warn($"Audio device test failed: {ex.Message}");
                return false;
            }
        }

        private void StopAllAudio()
        {
            try
            {
                StopSounds();
                StopMusic();
            }
            catch (Exception ex)
            {
                Log.Warn($"Error stopping audio during device disconnection: {ex.Message}");
            }
        }

        private void RestoreCurrentMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusicIndices[i] > 0)
                {
                    PlayMusic(_currentMusicIndices[i], i == 1);
                }
            }
        }

        private bool CheckAudioDeviceHealth()
        {
            if (!_canReproduceAudio || _audioDeviceDisconnected)
            {
                return false;
            }

            try
            {
                float volume = SoundEffect.MasterVolume;
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"Audio device health check failed: {ex.Message}");
                _audioDeviceDisconnected = true;
                return false;
            }
        }
    }
}
