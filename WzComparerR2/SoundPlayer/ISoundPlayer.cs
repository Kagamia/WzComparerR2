using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2
{
    public interface ISoundPlayer : IDisposable
    {
        bool Inited { get; }
        bool Init();
        void PreLoad(ISoundFile sound);
        void UnLoad();
        void Play();
        void Pause();
        void Resume();
        void Stop();
        bool AutoPlay { get; set; }
        int Volume { get; set; }
        double SoundPosition { get; set; }
        double SoundLength { get; }
        PlayState State { get; }
    }
}
