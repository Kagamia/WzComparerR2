using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Comparer
{
    public class CompareDifference
    {
        public CompareDifference(Wz_Node nodeNew, Wz_Node nodeOld,DifferenceType type)
        {
            this.nodeNew = nodeNew;
            this.nodeOld = nodeOld;
            this.type = type;
        }

        Wz_Node nodeNew;
        Wz_Node nodeOld;
        DifferenceType type;

        public Wz_Node NodeNew
        {
            get { return nodeNew; }
        }
       
        public Wz_Node NodeOld
        {
            get { return nodeOld; }
        }

        public object ValueNew
        {
            get { return nodeNew == null ? null : nodeNew.Value; }
        }

        public object ValueOld
        {
            get { return nodeOld == null ? null : nodeOld.Value; }
        }

        public DifferenceType DifferenceType
        {
            get { return type; }
        }

        public override string ToString()
        {
            switch (this.type)
            {
                case DifferenceType.Append:
                    return string.Format("{0} {1}({2})", this.type, this.nodeNew.Text, this.ValueNew);
                case DifferenceType.Changed:
                    return string.Format("{0} {1}({2}<-{3})", this.type, this.nodeNew.Text, this.ValueNew, this.ValueOld);
                case DifferenceType.Remove:
                    return string.Format("{0} {1}({2})", this.type, this.nodeOld.Text, this.ValueOld);
            }
            return base.ToString();
        }
    }
}
