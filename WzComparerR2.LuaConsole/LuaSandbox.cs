using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WzComparerR2.WzLib;
using NLua;

namespace WzComparerR2.LuaConsole
{
    public class LuaSandbox : IDisposable
    {
        public LuaSandbox(object env)
        {
            this.Env = env;
        }

        public Lua Lua { get; private set; }
        public object Env { get; private set; }
        private bool hookEventAttched;

        public void InitLuaEnv(string scriptBaseDir = null, bool forceReset = false)
        {
            if (this.Lua != null)
            {
                if (forceReset)
                {
                    this.Lua.Dispose();
                    this.Lua = null;
                }
                else
                {
                    return;
                }
            }

            var lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            lua.LoadCLRPackage();
            lua["env"] = this.Env;

            // TODO: will move this function to external lua file
            lua.DoString(@"
local t_IEnumerable = {}
t_IEnumerable.typeRef = luanet.import_type('System.Collections.IEnumerable')
t_IEnumerable.GetEnumerator = luanet.get_method_bysig(t_IEnumerable.typeRef, 'GetEnumerator')

local t_IEnumerator = {}
t_IEnumerator.typeRef = luanet.import_type('System.Collections.IEnumerator')
t_IEnumerator.MoveNext = luanet.get_method_bysig(t_IEnumerator.typeRef, 'MoveNext')
t_IEnumerator.get_Current = luanet.get_method_bysig(t_IEnumerator.typeRef, 'get_Current')

function each(userData)
  if type(userData) == 'userdata' then
    local i = 0;
    local ienum = t_IEnumerable.GetEnumerator(userData)
    return function()
      if ienum and t_IEnumerator.MoveNext(ienum) then
        i = i + 1
        return i, t_IEnumerator.get_Current(ienum)
      end
      return nil, nil
    end
  end
end
");

            // Only set the package path on the first run.
            // path order:
            // 1. script file folder
            // 2. luaConsole plugin folder
            // 3. wzComparerR2 folder
            // 4. current folder
            string pluginBaseDir = Path.GetDirectoryName(typeof(Entry).Assembly.Location);
            string wcR2Folder = Application.StartupPath;

            List<string> packagePath = new List<string>(16);
            foreach (var baseDir in new[] { scriptBaseDir, pluginBaseDir, wcR2Folder })
            {
                if (!string.IsNullOrEmpty(baseDir))
                {
                    packagePath.Add(Path.Combine(baseDir, "?.lua"));
                    packagePath.Add(Path.Combine(baseDir, "?", "init.lua"));
                    packagePath.Add(Path.Combine(baseDir, "lua", "?.lua"));
                    packagePath.Add(Path.Combine(baseDir, "lua", "?", "init.lua"));
                }
            }
            packagePath.Add(Path.Combine(".", "?.lua"));
            packagePath.Add(Path.Combine(".", "?", "init.lua"));
            lua.SetObjectToPath("package.path", string.Join(";", packagePath.Distinct()));

            // Register commonly used delegate types
            lua.RegisterLuaDelegateType(typeof(WzComparerR2.GlobalFindNodeFunction), typeof(LuaGlobalFindNodeFunctionHandler));

            this.Lua = lua;
        }

        public async Task<object[]> DoStringAsync(string chunk, CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(this.HookCancellationEvent);
            try
            {
                return await Task.Run(() => this.Lua.DoString(chunk), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.RemoveCancellationEvent();
            }
        }

        private void HookCancellationEvent()
        {
            if (!this.hookEventAttched)
            {
                this.Lua.DebugHook += this.Lua_DebugHook;
                this.Lua.SetDebugHook(KeraLua.LuaHookMask.Count, 1);
                this.hookEventAttched = true;
            }
        }

        private void RemoveCancellationEvent()
        {
            if (this.hookEventAttched)
            {
                this.Lua.DebugHook -= this.Lua_DebugHook;
                this.Lua.RemoveDebugHook();
                this.hookEventAttched = false;
            }
        }

        private void Lua_DebugHook(object sender, NLua.Event.DebugHookEventArgs e)
        {
            ((Lua)sender).State.Error("Operation cancelled.");
        }

        public void Dispose()
        {
            if (this.Lua != null)
            {
                this.Lua.Dispose();
                this.Lua = null;
            }
        }

        class LuaGlobalFindNodeFunctionHandler : NLua.Method.LuaDelegate
        {
            Wz_Node CallFunction(string wzPath)
            {
                object[] args = { wzPath };
                object[] inArgs = { wzPath };
                int[] outArgs = { };

                object ret = CallFunction(args, inArgs, outArgs);

                return (Wz_Node)ret;
            }
        }
    }
}
