using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Comparer
{
    public class CompareDifference
    {
        public CompareDifference(Wz_Node nodeNew, Wz_Node nodeOld, DifferenceType type)
        {
            this.NodeNew = nodeNew;
            this.NodeOld = nodeOld;
            this.DifferenceType = type;
        }

        public Wz_Node NodeNew { get; protected set; }

        public Wz_Node NodeOld { get; protected set; }

        public virtual object ValueNew
        {
            get { return NodeNew?.Value; }
        }

        public virtual object ValueOld
        {
            get { return NodeOld?.Value; }
        }

        public DifferenceType DifferenceType { get; protected set; }

        public override string ToString()
        {
            switch (this.DifferenceType)
            {
                case DifferenceType.Append:
                    return string.Format("{0} {1}({2})", this.DifferenceType, this.NodeNew.Text, this.ValueNew);
                case DifferenceType.Changed:
                    return string.Format("{0} {1}({2}<-{3})", this.DifferenceType, this.NodeNew.Text, this.ValueNew, this.ValueOld);
                case DifferenceType.Remove:
                    return string.Format("{0} {1}({2})", this.DifferenceType, this.NodeOld.Text, this.ValueOld);
            }
            return base.ToString();
        }
    }

}
