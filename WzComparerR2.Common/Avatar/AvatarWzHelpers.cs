using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    internal static class AvatarWzHelpers
    {
        public static Wz_Node ResolveUolChecked(this Wz_Node node)
        {
            // UOL chains are assumed acyclic; a broken target is not an ordinary missing value.
            while (node?.Value is Wz_Uol uol)
            {
                node = uol.HandleUol(node);
                if (node == null)
                    throw new InvalidDataException("Broken avatar UOL: " + uol.Uol);
            }

            return node;
        }

        public static Wz_Node ResolveOptionalUol(this Wz_Node node)
        {
            var visited = new HashSet<Wz_Node>();
            while (node?.Value is Wz_Uol uol)
            {
                if (!visited.Add(node))
                    return null;
                node = uol.HandleUol(node);
            }

            return node;
        }

        public static string ReadString(this Wz_Node node)
        {
            node = node.ResolveOptionalUol();
            return node.GetValueEx<string>(null);
        }

        public static Wz_Node FindNode(this Wz_Node root, string path)
        {
            Wz_Node node = root;

            // Each path segment may cross an image boundary or a UOL alias.
            foreach (string name in path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
            {
                node = node.ResolveOptionalUol()?.ExtractImageNode();
                if (node == null)
                    return null;
                node = node.Nodes[name];
            }

            return node.ResolveOptionalUol();
        }

        public static int ReadInt(this Wz_Node node, int defaultValue)
        {
            node = node.ResolveOptionalUol();
            return node.GetValueEx<int>(defaultValue);
        }

        public static int? ReadInt(this Wz_Node node)
        {
            node = node.ResolveOptionalUol();
            return node.GetValueEx<int?>(null);
        }

        public static bool TryReadPoint(this Wz_Node node, out Point value)
        {
            node = node.ResolveOptionalUol();
            Wz_Vector vector = node.GetValueEx<Wz_Vector>(null);
            if (vector == null)
            {
                value = Point.Empty;
                return false;
            }

            value = new Point(vector.X, vector.Y);
            return true;
        }

        public static Wz_Node ExtractImageNode(this Wz_Node node)
        {
            node = node.ResolveUolChecked();
            Wz_Image image = node.GetValueEx<Wz_Image>(null);
            if (image != null)
            {
                return image.TryExtract() ? image.Node : null;
            }

            return node;
        }

        public static Wz_Node RequireImageNode(this Wz_Node node, string path)
        {
            node = node.ResolveUolChecked();
            if (node == null)
                throw new KeyNotFoundException("Avatar resource not found: " + path);

            // Required resources preserve the extraction failure as the inner exception.
            Wz_Image image = node.GetValue<Wz_Image>(null);
            if (image != null)
            {
                if (!image.TryExtract(out Exception error))
                    throw new InvalidDataException("Could not extract avatar resource: " + path, error);
                return image.Node;
            }

            return node;
        }

        public static IEnumerable<Wz_Node> NumberedChildren(this Wz_Node node)
        {
            // Select the last definition of each numeric ID before sorting, preserving gaps.
            return node.Nodes
                .Select(child => (
                    node: child,
                    number: int.TryParse(child.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : (int?)null
                ))
                .Where(x => x.number.HasValue)
                .GroupBy(x => x.number.Value)
                .Select(group => group.Last())
                .OrderBy(x => x.number.Value)
                .Select(x => x.node);
        }

        public static Wz_Node FindNumberedChild(this Wz_Node node, int number, out bool hasNumberedChildren)
        {
            hasNumberedChildren = false;
            Wz_Node result = null;
            if (node == null)
                return null;

            // Continue after a match so later numeric aliases can replace it.
            foreach (Wz_Node child in node.Nodes)
            {
                if (!int.TryParse(child.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int childNumber))
                    continue;
                hasNumberedChildren = true;
                if (childNumber != number)
                    continue;
                result = child;
            }

            return result;
        }

        public static bool IsFrame(this Wz_Node node)
        {
            node = node.ResolveUolChecked();
            if (node == null)
                return false;
            if (node.Value != null)
                return false;

            // Recognize property-shaped frames without reading image pixels.
            if (node.Nodes.Count == 0 || node.Nodes["delay"] != null || node.Nodes["action"] != null)
                return true;
            return node.Nodes.Any(child => child.ResolveUolChecked()?.Value is Wz_Png);
        }
    }
}
