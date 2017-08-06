using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.Avatar.UI;
using System.Linq;

namespace WzComparerR2.Avatar
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
        }

        protected override void OnLoad()
        {
            var f = new AvatarForm();
            f.PluginEntry = this;
            var tabCtrl = f.GetTabPanel();
            Context.AddTab(f.Text, tabCtrl);
            Context.SelectedNode1Changed += f.OnSelectedNode1Changed;
            Context.WzClosing += f.OnWzClosing;
            this.Tab = tabCtrl.TabItem;
        }

        public SuperTabItem Tab { get; private set; }

        public void btnSetting_Click(object sender, EventArgs e)
        {
            AvatarCanvas canvas = new AvatarCanvas();
            canvas.LoadZ();
            canvas.LoadActions();
            canvas.LoadEmotions();

            /*
            cmbAction.Items.Clear();
            foreach (var action in canvas.Actions)
            {
                ComboItem cmbItem = new ComboItem(action.Name);
                switch (action.Level)
                {
                    case 0:
                        cmbItem.FontStyle = System.Drawing.FontStyle.Bold;
                        cmbItem.ForeColor = Color.Indigo;
                        break;

                    case 1:
                        cmbItem.ForeColor = Color.Indigo;
                        break;
                }
                cmbAction.Items.Add(cmbItem);
            }*/

            canvas.ActionName = "stand1";
            canvas.EmotionName = "shine";
            canvas.TamingActionName = "stand1";
            AddPart(canvas, "Character\\00002000.img");
            AddPart(canvas, "Character\\00012000.img");
            AddPart(canvas, "Character\\Face\\00020000.img");
            AddPart(canvas, "Character\\Hair\\00030000.img");
            AddPart(canvas, "Character\\Coat\\01040036.img");
            AddPart(canvas, "Character\\Pants\\01060026.img");
            //AddPart(canvas, "Character\\Weapon\\01442000.img");
            //AddPart(canvas, "Character\\Weapon\\01382007.img");
            //AddPart(canvas, "Character\\Weapon\\01332000.img");
            //AddPart(canvas, "Character\\Weapon\\01342000.img");

            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);

            //foreach (var action in canvas.Actions)
            foreach (var action in new[] { "walk1", "jump", "stand1"})
            {
                Gif gif = new Gif();
                var actionFrames = canvas.GetActionFrames(action);
                foreach (var frame in actionFrames)
                {
                    if (frame.Delay != 0)
                    {
                        var bone = canvas.CreateFrame(frame, faceFrames[0], null);
                        var bmp = canvas.DrawFrame(bone);

                        Point pos = bmp.OpOrigin;
                        pos.Offset(frame.Flip ? new Point(-frame.Move.X, frame.Move.Y) : frame.Move);
                        GifFrame f = new GifFrame(bmp.Bitmap, new Point(-pos.X, -pos.Y), Math.Abs(frame.Delay));
                        gif.Frames.Add(f);
                    }
                }
                

                var gifFile = gif.EncodeGif(Color.Transparent);
                string fileName = "D:\\ms\\new_" + action.Replace('\\', '.');
                gifFile.Save(fileName + (gif.Frames.Count == 1 ? ".png" : ".gif"));

                var fd = new System.Drawing.Imaging.FrameDimension(gifFile.FrameDimensionsList[0]);
                //获取帧数(gif图片可能包含多帧，其它格式图片一般仅一帧)
                int count = gifFile.GetFrameCount(fd);
                for (int i = 0; i < count; i++)
                {
                    gifFile.SelectActiveFrame(fd, i);
                    gifFile.Save(fileName + "_" + i + ".png", System.Drawing.Imaging.ImageFormat.Png);
                }

                gifFile.Dispose();
            }
            
            if (true)
            {

                Gif gif = CreateChair(canvas);
                var gifFile = gif.EncodeGif(Color.Transparent, 0);
                string fileName = "D:\\d16";

                if (false)
                {
                    var fd = new System.Drawing.Imaging.FrameDimension(gifFile.FrameDimensionsList[0]);
                    //获取帧数(gif图片可能包含多帧，其它格式图片一般仅一帧)
                    int count = gifFile.GetFrameCount(fd);
                    for (int i = 0; i < count; i++)
                    {
                        gifFile.SelectActiveFrame(fd, i);
                        gifFile.Save(fileName + "_" + i + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                gifFile.Save(fileName + (gif.Frames.Count == 1 ? ".png" : ".gif"));
                gifFile.Dispose();
            }
        }

        private Gif CreateContinueAction(AvatarCanvas canvas)
        {
            string afterImage = null;
            Wz_Node defaultAfterImageNode = null;
            if (canvas.Weapon != null)
            {
                afterImage = canvas.Weapon.Node.FindNodeByPath(false, "info", "afterImage").GetValueEx<string>(null);
                if (!string.IsNullOrEmpty(afterImage))
                {
                    defaultAfterImageNode = PluginManager.FindWz("Character\\Afterimage\\" + afterImage + ".img\\10");
                }
            }

            GifCanvas gifCanvas = new GifCanvas();
            gifCanvas.Layers.Add(new GifLayer());
            int delay = 0;
            //foreach (string act in new[] { "alert", "swingP1PoleArm", "doubleSwing", "tripleSwing" })
            //foreach (var act in new object[] { "alert", "swingP1PoleArm", "overSwingDouble", "overSwingTriple" })
            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);
            //foreach (string act in new[] { "PBwalk1", "PBstand4", "PBstand5" })

            foreach (var act in new object[] {
                
                PluginManager.FindWz("Skill\\2312.img\\skill\\23121004"),
                "stand1",
                PluginManager.FindWz("Skill\\2312.img\\skill\\23121052"),
                //PluginManager.FindWz("Skill\\2112.img\\skill\\21120010"),

                //PluginManager.FindWz("Skill\\200.img\\skill\\2001002"),
                //PluginManager.FindWz("Skill\\230.img\\skill\\2301003"),
                //PluginManager.FindWz("Skill\\230.img\\skill\\2301004"),
                //PluginManager.FindWz("Skill\\231.img\\skill\\2311003"),

                //PluginManager.FindWz("Skill\\13100.img\\skill\\131001010"),
                //"PBwalk1"
            })
            {
                string actionName = null;
                Wz_Node afterImageNode = null;
                List<Gif> effects = new List<Gif>();

                if (act is string)
                {
                    actionName = (string)act;
                }
                else if (act is Wz_Node)
                {
                    Wz_Node skillNode = (Wz_Node)(object)act;
                    actionName = skillNode.FindNodeByPath("action\\0").GetValueEx<string>(null);
                    if (!string.IsNullOrEmpty(afterImage))
                    {
                        afterImageNode = skillNode.FindNodeByPath("afterimage\\" + afterImage);
                    }

                    for (int i = -1; ; i++)
                    {
                        Wz_Node effNode = skillNode.FindNodeByPath("effect" + (i > -1 ? i.ToString() : ""));
                        if (effNode == null)
                            break;
                        effects.Add(Gif.CreateFromNode(effNode, PluginManager.FindWz));
                    }
                }

                if (string.IsNullOrEmpty(actionName))
                {
                    continue;
                }

                //afterImageNode = afterImageNode ?? defaultAfterImageNode;


                //添加特效帧
                foreach (var effGif in effects)
                {
                    if (effGif != null && effGif.Frames.Count > 0)
                    {
                        var layer = new GifLayer();
                        if (delay > 0)
                        {
                            layer.AddBlank(delay);
                        }
                        effGif.Frames.ForEach(af => layer.AddFrame((GifFrame)af));
                        gifCanvas.Layers.Add(layer);
                    }
                }

                //添加角色帧
                ActionFrame[] actionFrames = canvas.GetActionFrames(actionName);
                for (int i = 0; i < actionFrames.Length; i++)
                {
                    var frame = actionFrames[i];

                    if (frame.Delay != 0)
                    {
                        //绘制角色主动作
                        var bone = canvas.CreateFrame(frame, null, null);
                        var bmp = canvas.DrawFrame(bone);
                        GifFrame f = new GifFrame(bmp.Bitmap, bmp.Origin, Math.Abs(frame.Delay));
                        gifCanvas.Layers[0].Frames.Add(f);

                        //寻找刀光帧
                        if (afterImageNode != null)
                        {
                            var afterImageAction = afterImageNode.FindNodeByPath(false, actionName, i.ToString());
                            if (afterImageAction != null)
                            {
                                Gif aGif = Gif.CreateFromNode(afterImageAction, PluginManager.FindWz);
                                if (aGif != null && aGif.Frames.Count > 0) //添加新图层
                                {
                                    var layer = new GifLayer();
                                    if (delay > 0)
                                    {
                                        layer.AddBlank(delay);
                                    }
                                    aGif.Frames.ForEach(af => layer.AddFrame((GifFrame)af));
                                    gifCanvas.Layers.Add(layer);
                                }
                            }
                        }

                        delay += f.Delay;
                    }

                }

            }

            return gifCanvas.Combine();
        }

        private Gif CreateKeyDownAction(AvatarCanvas canvas)
        {
            string afterImage = null;
            Wz_Node defaultAfterImageNode = null;
            if (canvas.Weapon != null)
            {
                afterImage = canvas.Weapon.Node.FindNodeByPath(false, "info", "afterImage").GetValueEx<string>(null);
                if (!string.IsNullOrEmpty(afterImage))
                {
                    defaultAfterImageNode = PluginManager.FindWz("Character\\Afterimage\\" + afterImage + ".img\\10");
                }
            }

            GifCanvas gifCanvas = new GifCanvas();
            var layers = new List<Tuple<GifLayer, int>>();
            var actLayer = new GifLayer();

            //gifCanvas.Layers.Add(new GifLayer());
            int delay = 0;
            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);

            var skillNode = PluginManager.FindWz("Skill\\2112.img\\skill\\21120018");
            var actionName = skillNode.FindNodeByPath("action\\0").GetValueEx<string>(null);

            int keydownCount = 2;

            foreach (var part in new [] {"prepare", "keydown", "keydownend"})
            {
                var effects = new List<Tuple<Gif,int>>();

                for (int i = -1; ; i++)
                {
                    Wz_Node effNode = skillNode.FindNodeByPath(part + (i > -1 ? i.ToString() : ""));
                    if (effNode == null)
                        break;
                    var gif = Gif.CreateFromNode(effNode, PluginManager.FindWz);
                    var z = effNode.FindNodeByPath("z").GetValueEx(0);
                    effects.Add(new Tuple<Gif, int>(gif, z));
                }

                int effDelay = 0;
                //添加特效帧
                foreach (var effGif in effects)
                {
                    if (effGif.Item1 != null && effGif.Item1.Frames.Count > 0)
                    {
                        var layer = new GifLayer();
                        if (delay > 0)
                        {
                            layer.AddBlank(delay);
                        }

                        int fDelay = 0;

                        for(int i = 0, i0 = part == "keydown" ? keydownCount : 1; i < i0; i++)
                        {
                            effGif.Item1.Frames.ForEach(af => layer.AddFrame((GifFrame)af));
                            layers.Add(new Tuple<GifLayer, int>(layer,effGif.Item2));
                            fDelay+= effGif.Item1.Frames.Select(f => f.Delay).Sum();
                        }

                        effDelay = Math.Max(fDelay, effDelay);
                    }
                }

                delay += effDelay;
            }


            //添加角色帧
            ActionFrame[] actionFrames = canvas.GetActionFrames(actionName);
            int adelay = 0;
            while (adelay < delay)
            {
                for (int i = 0; i < actionFrames.Length; i++)
                {
                    var frame = actionFrames[i];

                    if (frame.Delay != 0)
                    {
                        //绘制角色主动作
                        var bone = canvas.CreateFrame(frame, null, null);
                        var bmp = canvas.DrawFrame(bone);
                        GifFrame f = new GifFrame(bmp.Bitmap, bmp.Origin, Math.Abs(frame.Delay));
                        actLayer.Frames.Add(f);
                        adelay += f.Delay;
                        //delay += f.Delay;
                    }
                }
            }

            layers.Add(new Tuple<GifLayer, int>(actLayer, 0));
            //按照z排序
            layers.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            gifCanvas.Layers.AddRange(layers.Select(t => t.Item1));

            return gifCanvas.Combine();
        }

        private Gif CreateChair(AvatarCanvas canvas)
        {
            GifCanvas gifCanvas = new GifCanvas();
            var layers = new List<Tuple<GifLayer, int>>();
            var actLayer = new GifLayer();

            //gifCanvas.Layers.Add(new GifLayer());
            int delay = 0;
            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);

            var ChairNode = PluginManager.FindWz(@"Item\Install\0301.img\03015660");
            var actionName = "sit";
            var pos = ChairNode.FindNodeByPath(@"info\bodyRelMove").GetValueEx<Wz_Vector>(null);
            
            Point browPos = new Point(-5, -48);

            //添加特效帧
            {
                var effects = new List<Tuple<Gif, int>>();

                for (int i = 1; ; i++)
                {
                    Wz_Node effNode = ChairNode.FindNodeByPath("effect"+( i > 1 ? i.ToString() : ""));
                    if (effNode == null)
                        break;
                    var gif = Gif.CreateFromNode(effNode, PluginManager.FindWz);
                    var z = effNode.FindNodeByPath("z").GetValueEx(0);
                    var isPos = effNode.Nodes["pos"].GetValueEx(0);


                    delay = Math.Max(delay, gif.Frames.Sum(f => f.Delay));

                    var layer = new GifLayer();
                    if (isPos == 1)
                    {
                        layer.Frames.AddRange(gif.Frames.Select(f =>
                        {
                            GifFrame frame = (GifFrame)f;
                            frame.Origin = new Point(frame.Origin.X - browPos.X, frame.Origin.Y - browPos.Y);
                            return frame;
                        }));
                    }
                    else
                    {
                        layer.Frames.AddRange(gif.Frames.Select(f => (GifFrame)f));
                    }
                   
                    layers.Add(new Tuple<GifLayer, int>(layer, z));
                }
            }

            //添加角色帧
            ActionFrame[] actionFrames = canvas.GetActionFrames(actionName);
            int adelay = 0;
            var bodyMove = pos == null ? Point.Empty : new Point(pos.X, pos.Y);
            while (adelay < delay)
            {
                for (int i = 0; i < actionFrames.Length; i++)
                {
                    var frame = actionFrames[i];

                    if (frame.Delay != 0)
                    {
                        //绘制角色主动作
                        var bone = canvas.CreateFrame(frame, faceFrames[0], null);
                        bone.Position = bodyMove;
                        var bmp = canvas.DrawFrame(bone);
                        
                        GifFrame f = new GifFrame(bmp.Bitmap, bmp.Origin, Math.Abs(frame.Delay));
                        actLayer.Frames.Add(f);
                        adelay += f.Delay;
                        //delay += f.Delay;
                    }
                }
            }

            layers.Add(new Tuple<GifLayer, int>(actLayer, 0));
            //按照z排序
            layers.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            gifCanvas.Layers.AddRange(layers.Select(t => t.Item1));

            return gifCanvas.Combine();
        }

        void AddPart(AvatarCanvas canvas, string imgPath)
        {
            Wz_Node imgNode = PluginManager.FindWz(imgPath);
            if (imgNode != null)
            {
                canvas.AddPart(imgNode);
            }
        }
    }
}
