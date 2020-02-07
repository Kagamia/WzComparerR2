using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class SetItemIDPart
    {
        public SetItemIDPart()
        {
            itemIDs = new Dictionary<int, bool>();
        }

        /// <summary>
        /// 通过一件装备ID初始化SetItemIDPart的实例。
        /// </summary>
        /// <param Name="ItemID">要初始化的装备ID。</param>
        public SetItemIDPart(int itemID)
            : this()
        {
            itemIDs[itemID] = false;
        }

        /// <summary>
        /// 通过一个装备ID集合初始化SetItemIDPart的实例。
        /// </summary>
        /// <param Name="itemIDList">要初始化的装备ID集合。</param>
        public SetItemIDPart(IEnumerable<int> itemIDList)
            : this()
        {
            foreach (int itemID in itemIDList)
            {
                itemIDs[itemID] = false;
            }
        }

        private Dictionary<int, bool> itemIDs;
        private string representName;
        private string typeName;
        private bool byGender;

        public Dictionary<int, bool> ItemIDs
        {
            get { return itemIDs; }
        }

        /// <summary>
        /// 获取一个值，它表示是否当前的装备ID中，至少有一个是生效的。
        /// </summary>
        public bool Enabled
        {
            get
            {
                foreach (var kv in itemIDs)
                {
                    if (kv.Value)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 获取或设置套装部件的显示名称。
        /// </summary>
        public string RepresentName
        {
            get { return representName; }
            set { representName = value; }
        }

        /// <summary>
        /// 获取或设置套装部件的类型显示名称。
        /// </summary>
        public string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        public bool ByGender
        {
            get { return byGender; }
            set { byGender = value; }
        }
    }
}
