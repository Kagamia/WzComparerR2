using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Controls.Primitives;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Themes;
using EmptyKeys.UserInterface.Mvvm;
using JLChnToZ.IMEHelper;

namespace WzComparerR2.MapRender.UI
{
    class TextBoxEx : TextBox
    {
        public static readonly DependencyProperty IMEEnabledProperty = DependencyProperty.Register("IMEEnabled", typeof(bool), typeof(TextBoxEx), new FrameworkPropertyMetadata(false));
        public TextBoxEx()
        {
            this.SetResourceReference(StyleProperty, MapRenderResourceKey.TextBoxExStyle);
            this.textEditor = new TextEditorProxy(this);
            this.undoManager = new UndoManagerProxy(this);

            this._ScrollViewerGet = (Func<ScrollViewer>)typeof(TextBoxBase)
                    .GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetGetMethod(true)
                    .CreateDelegate(typeof(Func<ScrollViewer>), this);
        }

        public event EventHandler<TextEventArgs> TextSubmit;

        private TextEditorProxy textEditor;
        private UndoManagerProxy undoManager;
        private Func<ScrollViewer> _ScrollViewerGet;
        
        //IMEhandler
        private IMEHandler imeHandler;
        private DateTime lastCompositionTime;
        private bool prevHasComposition;
        private readonly TimeSpan imeEndThreshold = TimeSpan.FromSeconds(0.033);

        public bool IMEEnabled
        {
            get { return (bool)this.GetValue(IMEEnabledProperty); }
            set { this.SetValue(IMEEnabledProperty, value); }
        }

        private ScrollViewer ScrollViewer
        {
            get { return this._ScrollViewerGet(); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var imeHandler = this.GetIMEService();
            bool imeEnabled = imeHandler != null && imeHandler.Enabled;
            bool hasComposition = imeEnabled && !string.IsNullOrEmpty(imeHandler.Composition);
            bool isCompositionEnd = (DateTime.Now - lastCompositionTime) >= imeEndThreshold;
            /*  
             *  keys  | noIME  | IME_noComp | IME_comp
             *  --------------------------------------
             *  chars | pass   | handle     | handle
             *  cmds  | pass   | pass       | handle
             *  enter | submit | submit     | handle
             */

            switch (e.Key)
            {
                case KeyCode.Enter:
                    e.Handled = true;
                    if (!imeEnabled || !hasComposition)
                    {
                        if (isCompositionEnd)
                        {
                            this.Submit();
                        }
                    }
                    break;

                case KeyCode.Y:
                case KeyCode.Z:
                case KeyCode.X:
                case KeyCode.C:
                case KeyCode.V:
                case KeyCode.A:
                    if (Keyboard.IsControlPressed)
                    {
                        if (hasComposition)
                        {
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case KeyCode.Left:
                case KeyCode.Up:
                case KeyCode.Right:
                case KeyCode.Down:
                case KeyCode.Home:
                case KeyCode.End:
                case KeyCode.Delete:
                    if (hasComposition)
                    {
                        e.Handled = true;
                    }
                    break;

                case KeyCode.Back:
                    if (imeEnabled)
                    {
                        if (hasComposition || !isCompositionEnd)
                        {
                            e.Handled = true;
                        }
                    }
                    break;

                case KeyCode.Escape:
                    if (InputManager.Current.FocusedElement == this)
                    {
                        InputManager.Current.ClearFocus();
                        e.Handled = true;
                    }
                    break;

                default:
                    if (imeEnabled)
                    {
                        e.Handled = true;
                    }
                    break;
            }

            if (e.Key == KeyCode.A && Keyboard.IsControlPressed)
            {
                e.Handled = true;
                this.SelectAll();
            }

            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(object sender, RoutedEventArgs e)
        {
            base.OnGotFocus(sender, e);

            if (this.IMEEnabled)
            {
                if (this.imeHandler == null)
                {
                    this.imeHandler = this.GetIMEService();
                }
                if (this.imeHandler != null)
                {
                    this.imeHandler.Enabled = true;
                    this.imeHandler.onResultReceived += ImeHandler_onResultReceived;
                    this.imeHandler.onCompositionReceived += ImeHandler_onCompositionReceived;
                }
            }
        }

        protected override void OnLostFocus(object sender, RoutedEventArgs e)
        {
            base.OnLostFocus(sender, e);

            if (this.imeHandler != null)
            {
                this.imeHandler.onResultReceived -= ImeHandler_onResultReceived;
                this.imeHandler.onCompositionReceived -= ImeHandler_onCompositionReceived;
                this.imeHandler.Enabled = false;
                this.imeHandler = null;
            }
        }

        private void Submit()
        {
            string text = this.Text;
            this.textEditor.SetText("");
            this.undoManager.Clear();
            this.textEditor.RaiseTextContainerChanged();
            InputManager.Current.ClearFocus();

            if (!string.IsNullOrEmpty(text))
            {
                this.OnTextSubmit(new TextEventArgs(text));
            }
        }

        private void ImeHandler_onResultReceived(object sender, IMEResultEventArgs e)
        {
            if (e.result >= 0x20)
            {
                this.InsertText(e.result.ToString());
            }
        }

        private void ImeHandler_onCompositionReceived(object sender, EventArgs e)
        {
            var imeWindow = sender as IMENativeWindow;
            if (imeWindow != null)
            {
                bool hasComposition = !string.IsNullOrEmpty(imeWindow.CompositionString);
                if (hasComposition!=this.prevHasComposition)
                {
                    lastCompositionTime = DateTime.Now;
                }
                this.prevHasComposition = hasComposition;

                var caretPos = this.textEditor.GetCaretVisualOffset(1, 1);
                var scrollViewer = this.ScrollViewer;
                if (scrollViewer != null)
                {
                    caretPos.X += scrollViewer.VisualPosition.X;
                    caretPos.Y += scrollViewer.VisualPosition.Y;

                    caretPos.Y += scrollViewer.Padding.Top - this.VerticalOffset;
                    switch (this.TextAlignment)
                    {
                        case TextAlignment.Left:
                            caretPos.X += scrollViewer.Padding.Left - this.HorizontalOffset;
                            break;
                        case TextAlignment.Right:
                            caretPos.X += -scrollViewer.Padding.Left - scrollViewer.Padding.Right - this.HorizontalOffset;
                            break;
                    }
                }
                else
                {
                    caretPos.X += this.VisualPosition.X;
                    caretPos.Y += this.VisualPosition.Y;
                }
                IMM.COMPOSITIONFORM form = new IMM.COMPOSITIONFORM();
                form.dwStyle = IMM.CFSPoint;
                form.ptCurrentPos.x = (int)(caretPos.X);
                form.ptCurrentPos.y = (int)(caretPos.Y);
                bool success = IMM.SetCompositionWindow(imeWindow.IMEContext, ref form);
            }
        }

        private void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (this.textEditor.SelectionLength > 0)
            {
                this.textEditor.StoreSelection(this.undoManager);
            }

            var sb = this.textEditor.StringBuilder;
            if (this.MaxLength != 0 && sb.Length + text.Length > this.MaxLength)
            {
                text = text.Substring(0, this.MaxLength - sb.Length);

                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
            }

            var caretIndex = this.textEditor.CaretIndex;
            if (caretIndex >= 0 && caretIndex <= sb.Length)
            {
                sb.Insert(caretIndex, text);
                this.textEditor.RecordInsertText(this.undoManager, caretIndex, text);
            }
            else
            {
                sb.Append(text);
                this.textEditor.RecordInsertText(this.undoManager, -1, text);
            }
            this.textEditor.CaretIndex = caretIndex + text.Length;
            this.textEditor.RaiseTextContainerChanged();
        }

        private IMEHandler GetIMEService()
        {
            var imeHandler = ServiceManager.Instance.GetService<IMEHandler>();
            return imeHandler;
        }

        protected virtual void OnTextSubmit(TextEventArgs e)
        {
            this.TextSubmit?.Invoke(this, e);
        }

        public static Style CreateStyle()
        {
            var style = TextBoxStyle.CreateTextBoxStyle();
            style.TargetType = typeof(TextBoxEx);
            return style;
        }

        private class TextEditorProxy
        {
            public TextEditorProxy(TextBox textBox)
            {
                object target = typeof(TextBoxBase)
                    ?.GetProperty("TextEditor", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetGetMethod(true)
                    ?.Invoke(textBox, null);

                if (target == null)
                {
                    throw new Exception("Get TextEditor failed.");
                }

                this.InitMethodCache(target);
                this.Target = target;
            }

            public object Target { get; private set; }

            private Func<int> _CaretIndexGet;
            private Action<int> _CaretIndexSet;
            private Func<int> _SelectionLengthGet;
            private Action<int> _SelectionLengthSet;
            private Func<int> _SelectionStartIndexGet;
            private Action<int> _SelectionStartIndexSet;
            private Action _RaiseTextContainerChanged;
            private MethodInfo _StoreSelectionMethod;
            private Action<string> _SetTextMethod;
            private Func<float, float, PointF> _GetCaretVisualOffsetMethod;
            private FieldInfo _StringBuilderField;
            private Type _insertTextHistoryType;
            private ConstructorInfo _insertTextHistoryCtor;


            public int CaretIndex
            {
                get { return this._CaretIndexGet(); }
                set { this._CaretIndexSet(value); }
            }

            public int SelectionLength
            {
                get { return this._SelectionLengthGet(); }
                set { this._SelectionLengthSet(value); }
            }

            public int SelectionStartIndex
            {
                get { return this._SelectionStartIndexGet(); }
                set { this._SelectionStartIndexSet(value); }
            }

            public StringBuilder StringBuilder
            {
                get { return (StringBuilder)this._StringBuilderField.GetValue(this.Target); }
            }

            public void RaiseTextContainerChanged()
            {
                this._RaiseTextContainerChanged();
            }

            public void StoreSelection(UndoManagerProxy undoManager)
            {
                this._StoreSelectionMethod.Invoke(this.Target, new object[] { undoManager.Target });
            }

            public void SetText(string text)
            {
                this._SetTextMethod(text);
            }

            public PointF GetCaretVisualOffset(float dpiX, float dpiY)
            {
                return this._GetCaretVisualOffsetMethod(dpiX, dpiY);
            }

            public void RecordInsertText(UndoManagerProxy undoManager, int position, string text)
            {
                object memento = this._insertTextHistoryCtor.Invoke(new object[] { position, text });
                undoManager.Store(memento);
            }


            private void InitMethodCache(object target)
            {
                var type = target.GetType();
                var flag = BindingFlags.Instance | BindingFlags.NonPublic;

                var caretIndexProp = type.GetProperty("CaretIndex", flag);
                this._CaretIndexGet = (Func<int>)caretIndexProp.GetGetMethod(true).CreateDelegate(typeof(Func<int>), target);
                this._CaretIndexSet = (Action<int>)caretIndexProp.GetSetMethod(true).CreateDelegate(typeof(Action<int>), target);

                var selectionLengthProp = type.GetProperty("SelectionLength", flag);
                this._SelectionLengthGet = (Func<int>)selectionLengthProp.GetGetMethod(true).CreateDelegate(typeof(Func<int>), target);
                this._SelectionLengthSet = (Action<int>)selectionLengthProp.GetSetMethod(true).CreateDelegate(typeof(Action<int>), target);

                var selectionIndexProp = type.GetProperty("SelectionStartIndex", flag);
                this._SelectionStartIndexGet = (Func<int>)selectionIndexProp.GetGetMethod(true).CreateDelegate(typeof(Func<int>), target);
                this._SelectionStartIndexSet = (Action<int>)selectionIndexProp.GetSetMethod(true).CreateDelegate(typeof(Action<int>), target);

                this._RaiseTextContainerChanged = (Action)type.GetMethod("RaiseTextContainerChanged", flag).CreateDelegate(typeof(Action), target);
                this._StoreSelectionMethod = type.GetMethod("/*Ԟ", flag);
                this._SetTextMethod = (Action<string>)type.GetMethod("SetText", flag).CreateDelegate(typeof(Action<string>), target);
                this._GetCaretVisualOffsetMethod = (Func<float, float, PointF>)type.GetMethod("GetCaretVisualOffset", flag).CreateDelegate(typeof(Func<float, float, PointF>), target);
                this._StringBuilderField = type.GetField("/*Ԗ", flag);

                this._insertTextHistoryType = type.Assembly.GetType(@"EmptyKeys.UserInterface.Documents./\*Յ");
                this._insertTextHistoryCtor = this._insertTextHistoryType.GetConstructor(new[] { typeof(int), typeof(string) });
            }
        }

        private class UndoManagerProxy
        {
            public UndoManagerProxy(TextBox textBox)
            {
                object target = typeof(TextBoxBase)
                    ?.GetProperty("UndoManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetGetMethod(true)
                    ?.Invoke(textBox, null);

                if (target == null)
                {
                    throw new Exception("Get UndoManager failed.");
                }

                this.InitMethodCache(target);
                this.Target = target;
            }

            public object Target { get; private set; }

            private MethodInfo _StoreMethod;
            private Action _ClearMethod;

            public void Store(object memento)
            {
                this._StoreMethod.Invoke(this.Target, new object[] { memento });
            }

            public void Clear()
            {
                this._ClearMethod();
            }

            private void InitMethodCache(object target)
            {
                var type = target.GetType();
                this._StoreMethod = type.GetMethod("Store");
                this._ClearMethod = (Action)type.GetMethod("Clear").CreateDelegate(typeof(Action), target);
            }
        }
    }

    class TextEventArgs : EventArgs
    {
        public TextEventArgs(string text)
        {
            this.Text = text;
        }

        public string Text { get; private set; }
    }
}
