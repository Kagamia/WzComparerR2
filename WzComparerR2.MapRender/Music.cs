using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using System.Runtime.InteropServices;
using Un4seen.Bass;

namespace WzComparerR2.MapRender
{
    class Music : IDisposable
    {
        public Music(Wz_Sound sound)
        {
            this.soundData = sound.ExtractSound();
            this.pData = GCHandle.Alloc(this.soundData, GCHandleType.Pinned);
            this.hStream = Bass.BASS_StreamCreateFile(pData.AddrOfPinnedObject(), 0, this.soundData.Length, BASSFlag.BASS_DEFAULT);
        }

        private byte[] soundData;
        private GCHandle pData;
        private int hStream;

        public bool IsLoop
        {
            get { return (Bass.BASS_ChannelFlags(hStream, 0, 0) & BASSFlag.BASS_SAMPLE_LOOP) != 0; }
            set { Bass.BASS_ChannelFlags(hStream, value ? BASSFlag.BASS_SAMPLE_LOOP : BASSFlag.BASS_DEFAULT, BASSFlag.BASS_SAMPLE_LOOP); }
        }

        public PlayState State
        {
            get
            {
                var active = Bass.BASS_ChannelIsActive(hStream);
                switch (active)
                {
                    case BASSActive.BASS_ACTIVE_STOPPED: return PlayState.Stopped;
                    case BASSActive.BASS_ACTIVE_PLAYING: return PlayState.Playing;
                    case BASSActive.BASS_ACTIVE_PAUSED: return PlayState.Paused;
                    default: return PlayState.Unknown;
                }
            }
        }

        public float Volume
        {
            get
            {
                float value = 0;
                return Bass.BASS_ChannelGetAttribute(hStream, BASSAttribute.BASS_ATTRIB_VOL, ref value) ? value : 0;
            }
            set
            {
                Bass.BASS_ChannelSetAttribute(hStream, BASSAttribute.BASS_ATTRIB_VOL, value);
            }
        }

        public void Play()
        {
            Bass.BASS_ChannelPlay(hStream, false);
        }

        public void Pause()
        {
            Bass.BASS_ChannelPause(hStream);
        }

        public void Stop()
        {
            Bass.BASS_ChannelStop(hStream);
        }

        public void Dispose()
        {
            Bass.BASS_StreamFree(hStream);
            this.pData.Free();
        }

        public enum PlayState
        {
            Stopped = 0,
            Playing = 1,
            Paused = 2,

            Unknown = -1,
        }
    }
}
