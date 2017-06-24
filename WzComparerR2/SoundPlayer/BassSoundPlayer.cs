using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Un4seen.Bass;
using System.IO;
using System.Runtime.InteropServices;

namespace WzComparerR2
{
    public class BassSoundPlayer : ISoundPlayer
    {
        static BassSoundPlayer()
        {
            BassNet.Registration("amethyst50504724@msn.com", "2X3223324222822");
            bool success = Bass.LoadMe();
        }

        public BassSoundPlayer()
        {
            volume = 100;
            autoPlay = true;
        }

        private bool inited;
        private bool autoPlay;
        private int volume;
        private bool loop;
        
        private int hStream;
        private bool isDisposed;

        private string playingSoundName;
        private byte[] data;

        private Dictionary<int, string> loadedPlugin;

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
                    if (inited = Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
                    {
                        loadedPlugin = Bass.BASS_PluginLoadDirectory(Program.LibPath);
                    }
                }
                catch
                {
                }
            }
            return inited;
        }

        public BASSError GetLastError()
        {
            return Bass.BASS_ErrorGetCode();
        }

        public IEnumerable<string> GetPluginSupportedExt()
        {
            if (this.loadedPlugin == null || this.loadedPlugin.Count == 0)
                yield break;
            foreach (var kv in this.loadedPlugin)
            {
                BASS_PLUGININFO info = Bass.BASS_PluginGetInfo(kv.Key);
                foreach (BASS_PLUGINFORM form in info.formats)
                {
                    yield return form.name + "(" + form.exts + ")" + "|" + form.exts;
                }
            }
        }

        public void PreLoad(ISoundFile sound)
        {
            if (sound == null) return;
            this.UnLoad();

            try
            {
                hStream = Bass.BASS_StreamCreateFile(sound.FileName, sound.StartPosition, sound.Length, BASSFlag.BASS_DEFAULT);
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
                hStream = Bass.BASS_StreamCreateFile(pData, 0, data.Length, BASSFlag.BASS_DEFAULT);
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
                var lastErr = Bass.BASS_ErrorGetCode();
            }
        }

        public void UnLoad()
        {
            if (hStream != 0)
            {
                Bass.BASS_ChannelStop(hStream);
                Bass.BASS_StreamFree(hStream);
                this.data = null;
            }
        }

        public void Play()
        {
            bool success = Bass.BASS_ChannelPlay(hStream, false);
        }

        public void Pause()
        {
            Bass.BASS_ChannelPause(hStream);
        }

        public void Resume()
        {
            Bass.BASS_ChannelPlay(hStream, false);
        }

        public void Stop()
        {
            Bass.BASS_ChannelStop(hStream);
            Bass.BASS_ChannelSetPosition(hStream, 0d);
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
                Bass.BASS_ChannelSetAttribute(hStream, BASSAttribute.BASS_ATTRIB_VOL, this.volume * 0.01f);
            }
        }

        public double SoundPosition
        {
            get
            {
                if (this.hStream != 0)
                    return Bass.BASS_ChannelBytes2Seconds(hStream, Bass.BASS_ChannelGetPosition(hStream));
                else
                    return 0d;
            }
            set
            {
                if (this.hStream != 0)
                {
                    double totalLen = this.SoundLength;
                    value = Math.Min(Math.Max(value, 0), totalLen);
                    Bass.BASS_ChannelSetPosition(hStream, value);
                }
            }
        }

        public double SoundLength
        {
            get
            {
                if (this.hStream != 0)
                    return Bass.BASS_ChannelBytes2Seconds(hStream, Bass.BASS_ChannelGetLength(hStream));
                else
                    return 0d;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                this.UnLoad();
                Bass.BASS_Free();
                if (this.loadedPlugin != null && this.loadedPlugin.Count > 0)
                {
                    foreach (var kv in this.loadedPlugin)
                    {
                        bool success = Bass.BASS_PluginFree(kv.Key);
                    }
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
                        Bass.BASS_ChannelFlags(hStream, BASSFlag.BASS_SAMPLE_LOOP, BASSFlag.BASS_SAMPLE_LOOP);
                    else
                        Bass.BASS_ChannelFlags(hStream, BASSFlag.BASS_DEFAULT, BASSFlag.BASS_SAMPLE_LOOP);
                }
            }
        }

        public PlayState State
        {
            get
            {
                if (this.hStream != 0)
                {
                    BASSActive active = Bass.BASS_ChannelIsActive(hStream);
                    switch (active)
                    {
                        case BASSActive.BASS_ACTIVE_STOPPED: return PlayState.Stopped;
                        case BASSActive.BASS_ACTIVE_PLAYING: return PlayState.Playing;
                        case BASSActive.BASS_ACTIVE_PAUSED: return PlayState.Paused;
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
