using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public static class Wz_NodeExtension2
    {
        public static Wz_Node GetLinkedSourceNode(this Wz_Node node, GlobalFindNodeFunction findNode)
        {
            string path;

            if (!string.IsNullOrEmpty(path = node.Nodes["source"].GetValueEx<string>(null)))
            {
                return findNode?.Invoke(path);
            }
            else if (!string.IsNullOrEmpty(path = node.Nodes["_inlink"].GetValueEx<string>(null)))
            {
                var img = node.GetNodeWzImage();
                return img?.Node.FindNodeByPath(true, path.Split('/'));
            }
            else if (!string.IsNullOrEmpty(path = node.Nodes["_outlink"].GetValueEx<string>(null)))
            {
                return findNode?.Invoke(path);
            }
            else
            {
                return node;
            }
        }
    }
}
