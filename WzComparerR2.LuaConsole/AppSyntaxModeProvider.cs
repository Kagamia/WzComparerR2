using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using ICSharpCode.TextEditor.Document;

namespace WzComparerR2.LuaConsole
{
    public class AppSyntaxModeProvider : ISyntaxModeFileProvider
    {
        public AppSyntaxModeProvider()
        {
            this.list = new List<SyntaxMode>();
            UpdateSyntaxModeList();
        }

        private List<SyntaxMode> list;

        public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
        {
            try
            {
                string resourceName = Path.GetFileNameWithoutExtension(syntaxMode.FileName);
                byte[] fileContent = Properties.Resources.ResourceManager.GetObject(resourceName) as byte[];
                if (fileContent != null)
                {
                    Stream stream = new MemoryStream(fileContent, false);
                    return new XmlTextReader(stream);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ICollection<SyntaxMode> SyntaxModes
        {
            get { return list; }
        }

        public void UpdateSyntaxModeList()
        {
            this.list.Clear();
            this.list.Add(new SyntaxMode("Lua.xshd", "Lua", "*.lua"));
        }
    }
}
