using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Patcher
{
    public class PatchingEventArgs : EventArgs
    {
        public PatchingEventArgs(PatchPartContext part, PatchingState state)
            : this(part, state, 0)
        {
        }

        public PatchingEventArgs(PatchPartContext part, PatchingState state, long currentFileLength)
        {
            this.part = part;
            this.state = state;
            this.currentFileLen = currentFileLength;
        }

        private PatchPartContext part;
        private PatchingState state;
        private long currentFileLen;

        public PatchPartContext Part
        {
            get { return part; }
        }

        public PatchingState State
        {
            get { return state; }
        }

        public long CurrentFileLength
        {
            get { return currentFileLen; }
        }
    }
}
