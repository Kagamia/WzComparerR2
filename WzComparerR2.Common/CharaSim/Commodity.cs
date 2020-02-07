using System;
using System.Collections.Generic;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Commodity
    {
        public Commodity()
        {
        }
        public int SN;
        public int ItemId;
        public int Count;
        public int Price;
        public int Bonus;
        public int Period;
        public int Priority;
        public int ReqPOP;
        public int ReqLEV;
        public int Gender;
        public int OnSale;
        public int Class;
        public int Limit;
        public string gameWorld;
        public int LimitMax;
        public int LimitQuestID;
        public int originalPrice;
        public int discount;
        public int PbCash;
        public int PbPoint;
        public int PbGift;
        public int Refundable;
        public int WebShop;
        public int termStart;
        public int termEnd;

        public static Commodity CreateFromNode(Wz_Node commodityNode)
        {
            if (commodityNode == null)
                return null;

            Commodity commodity = new Commodity();

            foreach (Wz_Node subNode in commodityNode.Nodes)
            {
                switch (subNode.Text)
                {
                    case "SN":
                        commodity.SN = Convert.ToInt32(subNode.Value);
                        break;
                    case "ItemId":
                        commodity.ItemId = Convert.ToInt32(subNode.Value);
                        break;
                    case "Count":
                        commodity.Count = Convert.ToInt32(subNode.Value);
                        break;
                    case "Price":
                        commodity.Price = Convert.ToInt32(subNode.Value);
                        break;
                    case "Bonus":
                        commodity.Bonus = Convert.ToInt32(subNode.Value);
                        break;
                    case "Period":
                        commodity.Period = Convert.ToInt32(subNode.Value);
                        break;
                    case "Priority":
                        commodity.Priority = Convert.ToInt32(subNode.Value);
                        break;
                    case "ReqPOP":
                        commodity.ReqPOP = Convert.ToInt32(subNode.Value);
                        break;
                    case "ReqLEV":
                        commodity.ReqLEV = Convert.ToInt32(subNode.Value);
                        break;
                    case "Gender":
                        commodity.Gender = Convert.ToInt32(subNode.Value);
                        break;
                    case "OnSale":
                        commodity.OnSale = Convert.ToInt32(subNode.Value);
                        break;
                    case "Class":
                        commodity.Class = Convert.ToInt32(subNode.Value);
                        break;
                    case "Limit":
                        commodity.Limit = Convert.ToInt32(subNode.Value);
                        break;
                    case "gameWorld":
                        commodity.gameWorld = Convert.ToString(subNode.Value);
                        break;
                    case "originalPrice":
                        commodity.originalPrice = Convert.ToInt32(subNode.Value);
                        break;
                    case "discount":
                        commodity.discount = Convert.ToInt32(subNode.Value);
                        break;
                    case "PbCash":
                        commodity.PbCash = Convert.ToInt32(subNode.Value);
                        break;
                    case "PbPoint":
                        commodity.PbPoint = Convert.ToInt32(subNode.Value);
                        break;
                    case "PbGift":
                        commodity.PbGift = Convert.ToInt32(subNode.Value);
                        break;
                    case "Refundable":
                        commodity.Refundable = Convert.ToInt32(subNode.Value);
                        break;
                    case "WebShop":
                        commodity.WebShop = Convert.ToInt32(subNode.Value);
                        break;
                    case "termStart":
                        commodity.termStart = Convert.ToInt32(subNode.Value);
                        break;
                    case "termEnd":
                        commodity.termEnd = Convert.ToInt32(subNode.Value);
                        break;
                }
            }

            return commodity;
        }
    }
}
