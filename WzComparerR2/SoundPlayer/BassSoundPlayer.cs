using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using ManagedBass;
using System.IO;
using System.Runtime.InteropServices;

namespace WzComparerR2
{
    public class BassSoundPlayer : ISoundPlayer
    {
        public BassSoundPlayer()
        {
            volume = 100;
            autoPlay = true;
            loadedPlugin = new HashSet<int>();
        }

        private bool inited;
        private bool autoPlay;
        private int volume;
        private bool loop;
        
        private int hStream;
        private bool isDisposed;

        private string playingSoundName;
        private byte[] data;

        private HashSet<int> loadedPlugin;

        public string PlayingSoundName
        {
            get { return playingSoundName; }
            set { playingSoundName = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public bool Inited
        {
            get { return inited; }
        }

        public bool Init()
        {
            if (!inited)
            {
                try
                {
                    Bass.Configure(Configuration.IncludeDefaultDevice, true);
                    if (inited = Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero))
                    {
                        if (Directory.Exists(Program.LibPath))
                        {
                            foreach (string file in Directory.GetFiles(Program.LibPath, "bass*.dll", SearchOption.AllDirectories))
                            {
                                int p = Bass.PluginLoad(file);
                                if (p != 0)
                                {
                                    loadedPlugin.Add(p);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return inited;
        }

        public Errors GetLastError()
        {
            return Bass.LastError;
        }

        public IEnumerable<string> GetPluginSupportedExt()
        {
            if (this.loadedPlugin == null || this.loadedPlugin.Count == 0)
                yield break;
            foreach (var p in this.loadedPlugin)
            {
                PluginInfo info = Bass.PluginGetInfo(p);
                foreach (PluginFormat form in info.Formats)
                {
                    yield return form.Name + "(" + form.FileExtensions + ")" + "|" + form.FileExtensions;
                }
            }
        }

        public void PreLoad(ISoundFile sound)
        {
            if (sound == null) return;
            this.UnLoad();

            try
            {
                hStream = Bass.CreateStream(sound.FileName, sound.StartPosition, sound.Length, BassFlags.Default);
            }
            catch
            {
                hStream = 0;
            }

            if (hStream != 0)
            {
                this.Volume = this.Volume;//调节音量到设定值
                this.Loop = this.Loop;
                if (this.autoPlay)
                    this.Play();
            }
        }

        public void PreLoad(byte[] data)
        {
            if (data == null) return;
            this.UnLoad();

            try
            {
                IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(data,0);
                hStream = Bass.CreateStream(pData, 0, data.Length, BassFlags.Default);
            }
            catch
            {
                hStream = 0;
            }

            if (hStream != 0)
            {
                this.Volume = this.Volume;//调节音量到设定值
                this.Loop = this.Loop;
                this.data = data;
                if (this.autoPlay)
                    this.Play();
            }
            else
            {
                var lastErr = Bass.LastError;
            }
        }

        public void UnLoad()
        {
            if (hStream != 0)
            {
                Bass.ChannelStop(hStream);
                Bass.StreamFree(hStream);
                this.data = null;
            }
        }

        public void Play()
        {
            bool success = Bass.ChannelPlay(hStream, false);
        }

        public void Pause()
        {
            Bass.ChannelPause(hStream);
        }

        public void Resume()
        {
            Bass.ChannelPlay(hStream, false);
        }

        public void Stop()
        {
            Bass.ChannelStop(hStream);
            Bass.ChannelSetPosition(hStream, 0);
        }

        public int Volume
        {
            get
            {
                return this.volume;
            }
            set
            {
                this.volume = Math.Min(Math.Max(value, 0), 100);
                Bass.ChannelSetAttribute(hStream, ChannelAttribute.Volume, this.volume * 0.01f);
            }
        }

        public double SoundPosition
        {
            get
            {
                if (this.hStream != 0)
                    return Bass.ChannelBytes2Seconds(hStream, Bass.ChannelGetPosition(hStream));
                else
                    return 0d;
            }
            set
            {
                if (this.hStream != 0)
                {
                    double totalLen = this.SoundLength;
                    value = Math.Min(Math.Max(value, 0), totalLen);
                    Bass.ChannelSetPosition(hStream, Bass.ChannelSeconds2Bytes(hStream, value));
                }
            }
        }

        public double SoundLength
        {
            get
            {
                if (this.hStream != 0)
                    return Bass.ChannelBytes2Seconds(hStream, Bass.ChannelGetLength(hStream));
                else
                    return 0d;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                this.UnLoad();
                Bass.Free();
                if (this.loadedPlugin != null && this.loadedPlugin.Count > 0)
                {
                    bool success = Bass.PluginFree(0);
                    this.loadedPlugin.Clear();
                }
                isDisposed = true;
            }
        }

        public bool AutoPlay
        {
            get
            {
                return this.autoPlay;
            }
            set
            {
                this.autoPlay = value;
            }
        }

        public bool Loop
        {
            get
            {
                return this.loop;
            }
            set
            {
                this.loop = value;
                if (this.hStream != 0)
                {
                    if (this.loop)
                        Bass.ChannelFlags(hStream, BassFlags.Loop, BassFlags.Loop);
                    else
                        Bass.ChannelFlags(hStream, BassFlags.Default, BassFlags.Loop);
                }
            }
        }

        public PlayState State
        {
            get
            {
                if (this.hStream != 0)
                {
                    PlaybackState active = Bass.ChannelIsActive(hStream);
                    switch (active)
                    {
                        case PlaybackState.Stopped: return PlayState.Stopped;
                        case PlaybackState.Playing: return PlayState.Playing;
                        case PlaybackState.Paused: return PlayState.Paused;
                        default: return PlayState.Stopped;
                    }
                }
                else
                {
                    return PlayState.Stopped;
                }
            }
        }
    }
}
