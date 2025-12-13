using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;
using System.Linq;

namespace WzComparerR2.CharaSim
{
    public class DamageSkin
    {
        public DamageSkin()
        {
            MiniDigit = new Dictionary<string, BitmapOrigin>();
            BigDigit = new Dictionary<string, BitmapOrigin>();
            MiniCriticalDigit = new Dictionary<string, BitmapOrigin>();
            BigCriticalDigit = new Dictionary<string, BitmapOrigin>();
            MiniUnit = new Dictionary<string, BitmapOrigin>();
            BigUnit = new Dictionary<string, BitmapOrigin>();
            MiniCriticalUnit = new Dictionary<string, BitmapOrigin>();
            BigCriticalUnit = new Dictionary<string, BitmapOrigin>();
            EtcBitmap = new Dictionary<string, BitmapOrigin>();
            MiniDigitSpacing = 0;
            BigDigitSpacing = 0;
            MiniCriticalDigitSpacing = 0;
            BigCriticalDigitSpacing = 0;
            MiniUnitSpacing = 0;
            BigUnitSpacing = 0;
            MiniCriticalUnitSpacing = 0;
            BigCriticalUnitSpacing = 0;
        }

        public int DamageSkinID { get; set; }
        public int ExtractItemID { get; set; }
        public Dictionary<string, BitmapOrigin> MiniDigit { get; set; }
        public int MiniDigitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> BigDigit { get; set; }
        public int BigDigitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> MiniCriticalDigit { get; set; }
        public int MiniCriticalDigitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> BigCriticalDigit { get; set; }
        public int BigCriticalDigitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> MiniUnit { get; set; }
        public int MiniUnitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> BigUnit { get; set; }
        public int BigUnitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> MiniCriticalUnit { get; set; }
        public int MiniCriticalUnitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> BigCriticalUnit { get; set; }
        public int BigCriticalUnitSpacing { get; set; }
        public Dictionary<string, BitmapOrigin> EtcBitmap { get; set; }
        public BitmapOrigin Sample { get; set; }
        public string Desc { get; set; }
        public string CustomType { get; set; }

        public static DamageSkin CreateFromNode(Wz_Node damageSkinNode, GlobalFindNodeFunction findNode)
        {
            if (damageSkinNode == null)
                return null;

            DamageSkin damageSkin = new DamageSkin();

            damageSkin.DamageSkinID = Convert.ToInt32(damageSkinNode.Text);

            foreach (Wz_Node subNode in damageSkinNode.Nodes)
            {
                switch (subNode.Text)
                {
                    case "desc":
                        damageSkin.Desc = subNode.GetValue<string>();
                        break;
                    case "extractID":
                        damageSkin.ExtractItemID = subNode.GetValue<int>();
                        break;
                    case "sample":
                        damageSkin.Sample = BitmapOrigin.CreateFromNode(subNode, findNode);
                        break;
                    case "effect":
                        foreach (Wz_Node effectNode in subNode.Nodes)
                        {
                            switch (effectNode.Text)
                            {
                                case "NoRed0":
                                    foreach (Wz_Node digitNode in effectNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.MiniDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.MiniDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.MiniDigitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoRed1":
                                    foreach (Wz_Node digitNode in effectNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.BigDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.BigDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.BigDigitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoCri0":
                                    foreach (Wz_Node digitNode in effectNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.MiniCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.MiniCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.MiniCriticalDigitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoCri1":
                                    foreach (Wz_Node digitNode in effectNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.BigCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.BigCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.BigCriticalDigitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoCustom":
                                    foreach (Wz_Node customSubNode in effectNode.Nodes)
                                    {
                                        switch (customSubNode.Text)
                                        {
                                            case "NoRed0":
                                                foreach (Wz_Node digitNode in customSubNode.Nodes)
                                                {
                                                    if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                                    {
                                                        damageSkin.MiniUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                                    }
                                                    else if (digitNode.Nodes.Count > 1)
                                                    {
                                                        foreach (Wz_Node node in digitNode.Nodes)
                                                        {
                                                            if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                            {
                                                                damageSkin.MiniUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else if (digitNode.Text == "numberSpace")
                                                    {
                                                        damageSkin.MiniUnitSpacing = digitNode.GetValue<int>();
                                                    }
                                                }
                                                break;
                                            case "NoRed1":
                                                foreach (Wz_Node digitNode in customSubNode.Nodes)
                                                {
                                                    if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                                    {
                                                        damageSkin.BigUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                                    }
                                                    else if (digitNode.Nodes.Count > 1)
                                                    {
                                                        foreach (Wz_Node node in digitNode.Nodes)
                                                        {
                                                            if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                            {
                                                                damageSkin.BigUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else if (digitNode.Text == "numberSpace")
                                                    {
                                                        damageSkin.BigUnitSpacing = digitNode.GetValue<int>();
                                                    }
                                                }
                                                break;
                                            case "NoCri0":
                                                foreach (Wz_Node digitNode in customSubNode.Nodes)
                                                {
                                                    if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                                    {
                                                        damageSkin.MiniCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                                    }
                                                    else if (digitNode.Nodes.Count > 1)
                                                    {
                                                        foreach (Wz_Node node in digitNode.Nodes)
                                                        {
                                                            if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                            {
                                                                damageSkin.MiniCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else if (digitNode.Text == "numberSpace")
                                                    {
                                                        damageSkin.MiniCriticalUnitSpacing = digitNode.GetValue<int>();
                                                    }
                                                }
                                                break;
                                            case "NoCri1":
                                                foreach (Wz_Node digitNode in customSubNode.Nodes)
                                                {
                                                    if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                                    {
                                                        damageSkin.BigCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                                    }
                                                    else if (digitNode.Nodes.Count > 1)
                                                    {
                                                        foreach (Wz_Node node in digitNode.Nodes)
                                                        {
                                                            if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                            {
                                                                damageSkin.BigCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else if (digitNode.Text == "numberSpace")
                                                    {
                                                        damageSkin.BigCriticalUnitSpacing = digitNode.GetValue<int>();
                                                    }
                                                }
                                                break;
                                            case "customType":
                                                damageSkin.CustomType = customSubNode.GetValue<string>();
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    // Legacy below
                    case "NoRed0":
                        foreach (Wz_Node digitNode in subNode.Nodes)
                        {
                            if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                            {
                                damageSkin.MiniDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                            }
                            else if (digitNode.Nodes.Count > 1)
                            {
                                foreach (Wz_Node node in digitNode.Nodes)
                                {
                                    if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                    {
                                        damageSkin.MiniDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                        break;
                                    }
                                }
                            }
                            else if (digitNode.Text == "numberSpace")
                            {
                                damageSkin.MiniDigitSpacing = digitNode.GetValue<int>();
                            }
                        }
                        break;
                    case "NoRed1":
                        foreach (Wz_Node digitNode in subNode.Nodes)
                        {
                            if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                            {
                                damageSkin.BigDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                            }
                            else if (digitNode.Nodes.Count > 1)
                            {
                                foreach (Wz_Node node in digitNode.Nodes)
                                {
                                    if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                    {
                                        damageSkin.BigDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                        break;
                                    }
                                }
                            }
                            else if (digitNode.Text == "numberSpace")
                            {
                                damageSkin.BigDigitSpacing = digitNode.GetValue<int>();
                            }
                        }
                        break;
                    case "NoCri0":
                        foreach (Wz_Node digitNode in subNode.Nodes)
                        {
                            if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                            {
                                damageSkin.MiniCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                            }
                            else if (digitNode.Nodes.Count > 1)
                            {
                                foreach (Wz_Node node in digitNode.Nodes)
                                {
                                    if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                    {
                                        damageSkin.MiniCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                        break;
                                    }
                                }
                            }
                            else if (digitNode.Text == "numberSpace")
                            {
                                damageSkin.MiniCriticalDigitSpacing = digitNode.GetValue<int>();
                            }
                        }
                        break;
                    case "NoCri1":
                        foreach (Wz_Node digitNode in subNode.Nodes)
                        {
                            if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                            {
                                damageSkin.BigCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                            }
                            else if (digitNode.Nodes.Count > 1)
                            {
                                foreach (Wz_Node node in digitNode.Nodes)
                                {
                                    if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                    {
                                        damageSkin.BigCriticalDigit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                        break;
                                    }
                                }
                            }
                            else if (digitNode.Text == "numberSpace")
                            {
                                damageSkin.BigCriticalDigitSpacing = digitNode.GetValue<int>();
                            }
                        }
                        break;
                    case "NoCustom":
                        foreach (Wz_Node customSubNode in subNode.Nodes)
                        {
                            switch (customSubNode.Text)
                            {
                                case "NoRed0":
                                    foreach (Wz_Node digitNode in customSubNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.MiniUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.MiniUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.MiniUnitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoRed1":
                                    foreach (Wz_Node digitNode in customSubNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.BigUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.BigUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.BigUnitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoCri0":
                                    foreach (Wz_Node digitNode in customSubNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.MiniCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.MiniCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.MiniCriticalUnitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "NoCri1":
                                    foreach (Wz_Node digitNode in customSubNode.Nodes)
                                    {
                                        if (digitNode.Value is Wz_Uol || digitNode.Value is Wz_Png)
                                        {
                                            damageSkin.BigCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(digitNode, findNode));
                                        }
                                        else if (digitNode.Nodes.Count > 1)
                                        {
                                            foreach (Wz_Node node in digitNode.Nodes)
                                            {
                                                if (node.Value is Wz_Uol || node.Value is Wz_Png)
                                                {
                                                    damageSkin.BigCriticalUnit.Add(digitNode.Text, BitmapOrigin.CreateFromNode(node, findNode));
                                                    break;
                                                }
                                            }
                                        }
                                        else if (digitNode.Text == "numberSpace")
                                        {
                                            damageSkin.BigCriticalUnitSpacing = digitNode.GetValue<int>();
                                        }
                                    }
                                    break;
                                case "customType":
                                    damageSkin.CustomType = customSubNode.GetValue<string>();
                                    break;
                            }
                        }
                        break;
                }
            }

            return damageSkin;
        }
    }
}
