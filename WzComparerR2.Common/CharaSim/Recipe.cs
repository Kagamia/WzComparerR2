using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Recipe
    {
        public Recipe()
        {
            this.TargetItems = new List<RecipeItemInfo>();
            this.RecipeItems = new List<RecipeItemInfo>();
            this.Props = new Dictionary<RecipePropType, int>();
        }

        public int RecipeID { get; set; }
        public List<RecipeItemInfo> TargetItems { get; private set; }
        public List<RecipeItemInfo> RecipeItems { get; private set; }
        public Dictionary<RecipePropType, int> Props { get; private set; }

        public int MainTargetItemID
        {
            get
            {
                if (this.TargetItems.Count > 0)
                {
                    return this.TargetItems[0].ItemID;
                }
                return 0;
            }
        }

        public static Recipe CreateFromNode(Wz_Node node)
        {
            Recipe recipe = new Recipe();
            int recipeID;
            if (!Int32.TryParse(node.Text, out recipeID))
                return null;
            recipe.RecipeID = recipeID;

            foreach (Wz_Node subNode in node.Nodes)
            {
                switch (subNode.Text)
                {
                    case "target":
                        for (int i = 0; ; i++)
                        {
                            Wz_Node itemNode = subNode.FindNodeByPath(i.ToString());
                            if (itemNode == null)
                            {
                                break;
                            }

                            RecipeItemInfo itemInfo = new RecipeItemInfo();
                            foreach (var itemPropNode in itemNode.Nodes)
                            {
                                switch (itemPropNode.Text)
                                {
                                    case "item":
                                        itemInfo.ItemID = itemPropNode.GetValue<int>();
                                        break;
                                    case "count":
                                        itemInfo.Count = itemPropNode.GetValue<int>();
                                        break;
                                    case "probWeight":
                                        itemInfo.ProbWeight = itemPropNode.GetValue<int>();
                                        break;
                                }
                            }
                            recipe.TargetItems.Add(itemInfo);
                        }
                        break;

                    case "recipe":
                        for (int i = 0; ; i++)
                        {
                            Wz_Node itemNode = subNode.FindNodeByPath(i.ToString());
                            if (itemNode == null)
                            {
                                break;
                            }
                            RecipeItemInfo itemInfo = new RecipeItemInfo();
                            foreach (var itemPropNode in itemNode.Nodes)
                            {
                                switch (itemPropNode.Text)
                                {
                                    case "item":
                                        itemInfo.ItemID = itemPropNode.GetValue<int>();
                                        break;
                                    case "count":
                                        itemInfo.Count = itemPropNode.GetValue<int>();
                                        break;
                                }
                            }
                            recipe.RecipeItems.Add(itemInfo);
                        }
                        break;

                    default:
                        RecipePropType type;
                        if (Enum.TryParse(subNode.Text, out type))
                        {
                            try
                            {
                                recipe.Props.Add(type, Convert.ToInt32(subNode.Value));
                            }
                            finally
                            {
                            }
                        }
                        break;
                }
            }

            return recipe;
        }
    }
}
