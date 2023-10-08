#if NET6_0_OR_GREATER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DevComponents.AdvTree;
using DevComponents.AdvTree.Display;
using DevComponents.DotNetBar;
using DevComponents.DotNetBar.Controls;
using HarmonyLib;

namespace WzComparerR2
{
    internal static class Dotnet6Patch
    {
        static Dotnet6Patch()
        {
            harmony = new Harmony("WzComparerR2-Dotnet6Patch");
        }

        private static readonly Harmony harmony;

        public static void Patch()
        {
            harmony.PatchAll();
        }

        public static void Unpatch()
        {
            harmony.UnpatchAll();
        }
    }

    [HarmonyPatch(typeof(ComboBoxEx))]
    internal class ComboBoxExPatch
    {
        [HarmonyPatch("ᑧ"), HarmonyPrefix]
        public static bool ᑧ(ComboBoxEx __instance, IntPtr ळ)
        {
            bool flag = false;
            if (Environment.Version.Major > 5)
            {
                flag = true;
            }
            else if (Environment.Version.Major == 5 && Environment.Version.Minor >= 1)
            {
                flag = true;
            }
            if (flag)
            {
                SetWindowTheme(ळ, " ", " ");
            }

            return false;
        }

        [DllImport("UxTheme.dll", CharSet = CharSet.Auto)]
        internal static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
    }

    [HarmonyPatch(typeof(AdvTree))]
    internal class AdvTreePatch
    {
        // this function is decompiled by ilspy
        public static void ٹ(AdvTree __instance, Node _0652, MouseEventArgs ؾ, Point _٧)
        {
            #region private members
            var GetLayoutPosition = AccessTools.MethodDelegate<Func<MouseEventArgs, Point>>(
                AccessTools.Method(typeof(AdvTree), "GetLayoutPosition", new[] { typeof(MouseEventArgs) }), __instance);
            var InvokeNodeMouseDown = AccessTools.MethodDelegate<Action<TreeNodeMouseEventArgs>>("DevComponents.AdvTree.AdvTree:InvokeNodeMouseDown", __instance);
            var ա = AccessTools.FieldRefAccess<AdvTree, int>(__instance, "ա");
            var node_CommandButton = AccessTools.Property(typeof(Node), "CommandButton");
            var ڳ = AccessTools.MethodDelegate<Action<Node, CommandButtonEventArgs>>("DevComponents.AdvTree.AdvTree:ڳ", __instance);
            var _ײ = AccessTools.FieldRefAccess<AdvTree, bool>(__instance, "_ײ");
            var _ת = AccessTools.FieldRefAccess<AdvTree, bool>(__instance, "_ת");
            var _055E = AccessTools.FieldRefAccess<AdvTree, SelectedNodesCollection>(__instance, "՞");
            var __0603 = AccessTools.FieldRefAccess<AdvTree, bool>(__instance, "_\u0603");
            var __05EB = AccessTools.FieldRefAccess<AdvTree, eMultiSelectRule>(__instance, "_\u05EB");
            var selectedNodesCollection_ۺ = AccessTools.Property(typeof(SelectedNodesCollection), "ۺ ");
            var _2599__25AA = AccessTools.MethodDelegate<Func<Node, Node>>("DevComponents.AdvTree.\u2599:\u25AA");
            var _2599__25A4 = AccessTools.MethodDelegate<Func<Node, Node>>("DevComponents.AdvTree.\u2599:\u25A4");
            var ٮ = AccessTools.MethodDelegate<Action<EventArgs>>("DevComponents.AdvTree.AdvTree:ٮ", __instance);
            var ڊ = AccessTools.MethodDelegate<Func<Node, int, int, Point, Cell>>("DevComponents.AdvTree.AdvTree:ڊ", __instance);
            var cell_GetEnabled = AccessTools.Method("DevComponents.AdvTree.Cell:GetEnabled");
            var cell_CheckBoxBoundsRelative = AccessTools.Property(typeof(Cell), "CheckBoxBoundsRelative");
            var cell_SetMouseDown = AccessTools.Method("DevComponents.AdvTree.Cell:SetMouseDown");
            var _0602 = AccessTools.StaticFieldRefAccess<AdvTree, string>("\u0602");
            var _059B = AccessTools.FieldRefAccess<AdvTree, object>(__instance, "\u059B");
            #endregion

            #region method body
#if true
            Point layoutPosition = GetLayoutPosition(ؾ);
            InvokeNodeMouseDown(new TreeNodeMouseEventArgs(_0652, ؾ.Button, ؾ.Clicks, ؾ.Delta, layoutPosition.X, layoutPosition.Y));
            if (ؾ.Button == MouseButtons.Left)
            {
                if (NodeDisplay.GetNodeRectangle(eNodeRectanglePart.ExpandHitTestBounds, _0652, _٧).Contains(layoutPosition) && ؾ.Clicks == 1 && _0652.ExpandVisibility != eNodeExpandVisibility.Hidden)
                {
                    ա = 0;
                    _0652.Toggle(eTreeAction.Mouse);
                    return;
                }
                if (/*_0652.CommandButton*/(bool)node_CommandButton.GetValue(_0652))
                {
                    ա = 0;
                    if (NodeDisplay.GetNodeRectangle(eNodeRectanglePart.CommandBounds, _0652, _٧).Contains(layoutPosition))
                    {
                        ڳ(_0652, new CommandButtonEventArgs(eTreeAction.Mouse, _0652));
                        return;
                    }
                }
                Rectangle nodeRectangle = NodeDisplay.GetNodeRectangle(eNodeRectanglePart.NodeContentBounds, _0652, _٧);
                if ((nodeRectangle.Contains(layoutPosition) || (_ײ && layoutPosition.Y >= nodeRectangle.Y && layoutPosition.Y <= nodeRectangle.Bottom)) && _0652.TreeControl != null)
                {
                    if (_0652.TreeControl.SelectedNode != _0652)
                    {
                        ա = 0;
                    }
                    if (_0652.Selectable)
                    {
                        if (_ת && _055E.Count > 0 && Control.ModifierKeys == Keys.None && ؾ.Button == MouseButtons.Left)
                        {
                            __0603 = true;
                        }
                        else
                        {
                            __0603 = false;
                        }
                        if (_ת && _055E.Count > 0 && (Control.ModifierKeys == Keys.Shift || Control.ModifierKeys == Keys.Control))
                        {
                            ա = 0;
                            if (__05EB == eMultiSelectRule.SameParent && _055E[0].Parent != _0652.Parent)
                            {
                                return;
                            }
                            if (Control.ModifierKeys == Keys.Shift && _055E.Count > 0)
                            {
                                Node node = _055E[0];
                                Node node2 = _0652;
                                bool flag = false;
                                /*_055E.ۺ = true;*/
                                selectedNodesCollection_ۺ.SetValue(_055E, true);
                                try
                                {
                                    while (_055E.Count > 1)
                                    {
                                        _055E.Remove(_055E[_055E.Count - 1], eTreeAction.Mouse);
                                        flag = true;
                                    }
                                }
                                finally
                                {
                                    /*_055E.ۺ = false;*/
                                    selectedNodesCollection_ۺ.SetValue(_055E, false);
                                }
                                if (node2 != node)
                                {
                                    if (node2.Bounds.Y > node.Bounds.Y)
                                    {
                                        /*_055E.ۺ = true;*/
                                        selectedNodesCollection_ۺ.SetValue(_055E, true);
                                        try
                                        {
                                            do
                                            {
                                                if (!node2.IsSelected && node2.Selectable && (__05EB == eMultiSelectRule.AnyNode || (__05EB == eMultiSelectRule.SameParent && _055E.Count > 0 && _055E[0].Parent == node2.Parent)))
                                                {
                                                    _055E.Add(node2, eTreeAction.Mouse);
                                                }
                                                node2 = _2599__25AA(node2);
                                            }
                                            while (node != node2 && node2 != null);
                                            return;
                                        }
                                        finally
                                        {
                                            /*_055E.ۺ = false;*/
                                            selectedNodesCollection_ۺ.SetValue(node, false);
                                            ٮ(EventArgs.Empty);
                                        }
                                    }
                                    /*_055E.ۺ = true;*/
                                    selectedNodesCollection_ۺ.SetValue(node, true);
                                    try
                                    {
                                        do
                                        {
                                            if (!node2.IsSelected && node2.Selectable && (__05EB == eMultiSelectRule.AnyNode || (__05EB == eMultiSelectRule.SameParent && _055E.Count > 0 && _055E[0].Parent == node2.Parent)))
                                            {
                                                _055E.Add(node2, eTreeAction.Mouse);
                                            }
                                            node2 = _2599__25A4(node2);
                                        }
                                        while (node != node2 && node2 != null);
                                        return;
                                    }
                                    finally
                                    {
                                        /*_055E.ۺ = false;*/
                                        selectedNodesCollection_ۺ.SetValue(node, false);
                                        ٮ(EventArgs.Empty);
                                    }
                                }
                                if (flag)
                                {
                                    ٮ(EventArgs.Empty);
                                }
                            }
                            else if (_0652.IsSelected)
                            {
                                _055E.Remove(_0652, eTreeAction.Mouse);
                            }
                            else
                            {
                                _055E.Add(_0652, eTreeAction.Mouse);
                            }
                            return;
                        }
                        if (!_0652.IsSelected)
                        {
                            __instance.SelectNode(_0652, eTreeAction.Mouse);
                            if (_0652.TreeControl == null || _0652.TreeControl.SelectedNode != _0652)
                            {
                                return;
                            }
                        }
                    }
                    Cell cell = ڊ(_0652, layoutPosition.X, layoutPosition.Y, _٧);
                    if (cell == null)
                    {
                        return;
                    }
                    bool flag2 = false;
                    if (cell.CheckBoxVisible && /*cell.GetEnabled()*/AccessTools.MethodDelegate<Func<bool>>(cell_GetEnabled, cell)())
                    {
                        Rectangle checkBoxBoundsRelative = /*cell.CheckBoxBoundsRelative*/(Rectangle)cell_CheckBoxBoundsRelative.GetValue(cell);
                        checkBoxBoundsRelative.Offset(NodeDisplay.GetNodeRectangle(eNodeRectanglePart.NodeBounds, _0652, _٧).Location);
                        if (checkBoxBoundsRelative.Contains(layoutPosition))
                        {
                            if (cell.CheckBoxThreeState)
                            {
                                if (cell.CheckState == CheckState.Checked)
                                {
                                    cell.SetChecked(CheckState.Indeterminate, eTreeAction.Mouse);
                                }
                                else if (cell.CheckState == CheckState.Unchecked)
                                {
                                    cell.SetChecked(CheckState.Checked, eTreeAction.Mouse);
                                }
                                else if (cell.CheckState == CheckState.Indeterminate)
                                {
                                    cell.SetChecked(CheckState.Unchecked, eTreeAction.Mouse);
                                }
                            }
                            else
                            {
                                cell.SetChecked(!cell.Checked, eTreeAction.Mouse);
                            }
                            flag2 = true;
                            ա = 0;
                        }
                    }
                    if (_0652.SelectedCell != cell)
                    {
                        ա = 1;
                    }
                    else if (!flag2)
                    {
                        ա++;
                    }
                    _0652.SetSelectedCell(cell, eTreeAction.Mouse);
                    /*cell.SetMouseDown(over: true);*/
                    AccessTools.MethodDelegate<Action<bool>>(cell_SetMouseDown, cell)(true);
                }
                else
                {
                    ա = 0;
                }
            }
            else
            {
                if (ؾ.Button != MouseButtons.Right || _0652.TreeControl == null)
                {
                    return;
                }
                if (!_0652.IsSelected)
                {
                    __instance.SelectNode(_0652, eTreeAction.Mouse);
                }
                if ((!__instance.MultiSelect && _0652.TreeControl.SelectedNode != _0652) || _0652.ContextMenu == null)
                {
                    return;
                }
                // the main purpose is to remove these lines, ContextMenu is obsoleted and has been removed from netcore.
                /*
                if (_0652.ContextMenu is ContextMenu)
                {
                    ContextMenu contextMenu = _0652.ContextMenu as ContextMenu;
                    contextMenu.Show(this, new Point(ؾ.X, ؾ.Y));
                }
                */
                else if (_0652.ContextMenu.GetType().FullName == "System.Windows.Forms.ContextMenuStrip")
                {
                    _0652.ContextMenu.GetType().InvokeMember("Show", BindingFlags.InvokeMethod, null, _0652.ContextMenu, new object[2]
                    {
                    __instance,
                    new Point(ؾ.X, ؾ.Y)
                    });
                }
                else if (_0652.ContextMenu.GetType().FullName == "DevComponents.DotNetBar.ButtonItem")
                {
                    Point point = __instance.PointToScreen(new Point(ؾ.X, ؾ.Y));
                    ((PopupItem)_0652.ContextMenu).SetSourceControl(__instance);
                    _0652.ContextMenu.GetType().InvokeMember("Popup", BindingFlags.InvokeMethod, null, _0652.ContextMenu, new object[1] { point });
                }
                else
                {
                    if (!_0652.ContextMenu.ToString().StartsWith(_0602) || _059B == null)
                    {
                        return;
                    }
                    string text = _0652.ContextMenu.ToString().Substring(_0602.Length);
                    object obj = _059B.GetType().InvokeMember("ContextMenus", BindingFlags.GetProperty, null, _059B, null);
                    int num = (int)obj.GetType().InvokeMember("IndexOf", BindingFlags.InvokeMethod, null, obj, new string[1] { text });
                    if (num >= 0)
                    {
                        IList list = obj as IList;
                        object obj2 = list[num];
                        try
                        {
                            obj2.GetType().InvokeMember("SetSourceControl", BindingFlags.InvokeMethod, null, obj2, new object[1] { __instance });
                        }
                        catch
                        {
                        }
                        Point point2 = __instance.PointToScreen(new Point(ؾ.X, ؾ.Y));
                        obj2.GetType().InvokeMember("Popup", BindingFlags.InvokeMethod, null, obj2, new object[1] { point2 });
                    }
                }
            }
#endif
            #endregion
        }

        [HarmonyPatch("OnMouseDown"), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnMouseDown(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> modifiedIL = new();
            var searchMethodInfo = AccessTools.Method(typeof(AdvTree), "ٹ");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(searchMethodInfo))
                {
                    var replaceMethodInfo = AccessTools.Method(typeof(AdvTreePatch), "ٹ");
                    var inst = new CodeInstruction(OpCodes.Call, replaceMethodInfo);
                    modifiedIL.Add(inst);
                }
                else
                {
                    modifiedIL.Add(instruction);
                }
            }
            return modifiedIL;
        }
    }
}

#endif