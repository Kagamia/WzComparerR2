using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.Avatar
{
    public class Bone
    {
        public Bone(string name)
        {
            this.Name = name;
            this.Children = new List<Bone>();
            this.Skins = new List<Skin>();
        }

        public string Name { get; set; }
        public Point Position { get; set; }
        public List<Bone> Children { get; private set; }
        public List<Skin> Skins { get; private set; }

        public BoneGroup Group { get; set; }
        public ActionFrame Property { get; set; }

        private Bone parent;
        public Bone Parent
        {
            get { return this.parent; }
            set
            {
                Bone oldParent = this.parent;
                if (oldParent != value)
                {
                    if (oldParent != null)
                    {
                        oldParent.Children.Remove(this);
                    }
                    if (value != null)
                    {
                        value.Children.Add(this);
                    }

                    this.parent = value;
                }
            }
        }

        public Bone FindChild(string name)
        {
            foreach (Bone bone in Children)
            {
                if (bone.Name == name) return bone;
                if (bone.Children.Count > 0)
                {
                    Bone c = bone.FindChild(name);
                    if (c != null) return c;
                }
            }
            return null;
        }
    }

    public enum BoneGroup
    {
        Unknown = 0,
        Character,
        Taming
    }
}
