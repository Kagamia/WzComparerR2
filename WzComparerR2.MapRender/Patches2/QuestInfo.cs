using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender.Patches2
{
    public struct QuestInfo
    {
        public QuestInfo(int id, int state)
        {
            this.ID = id;
            this.State = state;
        }

        public int ID { get; set; }
        public int State { get; set; }
    }

    public struct QuestExInfo
    {
        public QuestExInfo(int id, string key, int state)
        {
            this.ID = id;
            this.Key = key;
            this.State = state;
        }

        public int ID { get; set; }
        public string Key { get; set; }
        public int State { get; set; }
    }
}
