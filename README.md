*<s>使用前先大喊 niconiconi! poi! duang!以减少bug发生率</s>*

# WzComparerR2
这是一个用C#6.0/.Net4.0组装的冒险岛提取器...  
包含了一些奇怪的机能比如stringWZ搜索 客户端对比 装备模拟 地图模拟等等..  

# Modules
- **WzComparerR2** 主程序
- **WzComparerR2.Common** 一些通用类
- **WzComparerR2.PluginBase** 插件管理器
- **WzComparerR2.WzLib** wz文件读取相关
- **CharaSimResource** 用于装备模拟的资源文件
- **WzComparerR2.Updater** 程序更新器(未完成)
- **WzComparerR2.LuaConsole** (可选插件)Lua控制台
- **WzComparerR2.MapRender** (可选插件)地图仿真器
- **WzComparerR2.Avatar** (可选插件)纸娃娃
- **WzComparerR2.MonsterCard** (可选插件)怪物卡(已废弃)

# Usage
- **2.x**: Win7+/.net4.0+/dx11.0
- **1.x**: WinXp+/.net2.0+/dx9.0

# Compile
- vs2015 or vs2012/13+Roslyn
- put [CharaSimResource](https://github.com/Kagamia/CharaSimResource) together

# Credits
- **Fiel** ([Southperry](http://www.southperry.net))  wz文件读取代码改造自WzExtract 以及WzPatcher
- **Index** ([Exrpg](http://bbs.exrpg.com/space-uid-137285.html)) MapRender的原始代码 以及libgif
- **[DotNetBar](http://www.devcomponents.com/)**
- **[IMEHelper](https://github.com/JLChnToZ/IMEHelper)**  
- **[Spine-Runtime](https://github.com/EsotericSoftware/spine-runtimes)**
- 以及...忠实的<s>小白鼠</s>测试用户们
