using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using System.Runtime.InteropServices;
using ManagedBass;

namespace WzComparerR2.MapRender
{
    class Music : IDisposable
    {
        public Music(Wz_Sound sound)
        {
            this.soundData = sound.ExtractSound();
            this.pData = GCHandle.Alloc(this.soundData, GCHandleType.Pinned);
            this.hStream = Bass.CreateStream(pData.AddrOfPinnedObject(), 0, this.soundData.Length, BassFlags.Default);
            Music.GlobalVolumeChanged += this.OnGlobalVolumeChanged;
        }

        private byte[] soundData;
        private GCHandle pData;
        private int hStream;
        private float? vol;

        public bool IsLoop
        {
            get { return (Bass.ChannelFlags(hStream, 0, 0) & BassFlags.Loop) != 0; }
            set { Bass.ChannelFlags(hStream, value ? BassFlags.Loop : BassFlags.Default, BassFlags.Loop); }
        }

        public PlayState State
        {
            get
            {
                var active = Bass.ChannelIsActive(hStream);
                switch (active)
                {
                    case PlaybackState.Stopped: return PlayState.Stopped;
                    case PlaybackState.Playing: return PlayState.Playing;
                    case PlaybackState.Paused: return PlayState.Paused;
                    default: return PlayState.Unknown;
                }
            }
        }

        public float Volume
        {
            get
            {
                if (vol == null)
                {
                    vol = Bass.ChannelGetAttribute(hStream, ChannelAttribute.Volume, out float value) ? value : 0;
                }
                return vol.Value;
            }
            set
            {
                vol = value;
                Bass.ChannelSetAttribute(hStream, ChannelAttribute.Volume, vol.Value * globalVol);
            }
        }

        public void Play()
        {
            Bass.ChannelPlay(hStream, false);
        }

        public void Pause()
        {
            Bass.ChannelPause(hStream);
        }

        public void Stop()
        {
            Bass.ChannelStop(hStream);
        }

        public void Dispose()
        {
            Music.GlobalVolumeChanged -= this.OnGlobalVolumeChanged;
            Bass.StreamFree(hStream);
            this.pData.Free();

        }

        public enum PlayState
        {
            Stopped = 0,
            Playing = 1,
            Paused = 2,

            Unknown = -1,
        }

        private void OnGlobalVolumeChanged(object sender, EventArgs e)
        {
            this.Volume = Volume;
        }

        #region Global Volume
        private static float globalVol = 1f;
        private static event EventHandler GlobalVolumeChanged;
        public static float GlobalVolume
        {
            get { return globalVol; }
            set
            {
                globalVol = value;
                GlobalVolumeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        #endregion
    }
}
