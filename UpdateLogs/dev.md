##2016.10.08

###共通
- 添加了可以对img导出为xml的功能


##2016.09.26

###CharaSim
- 修复Item无法识别link没有图标的bug


##2016.09.16

###共通
- wzlib读取结构变更 Wz_Image.Node.Value不再指向Wz_Image自身的引用
- 修复因上一条导致的若干运行效果不正确的场合

###WzCompare
- 支持了CMS/TMS等wz合并对比时同名节点冲突的场合，目前可以分别对比

###CharaSim
- 修复恶魔盾牌MP/DF显示bug

###LuaConsole
- 添加了可以获取全局插件环境和事件的接口 因此在Lua中可以获取当前选中的Wz节点了


##2016.08.11

###共通
- 整理Lua的提取方式 直接绑定在顶层节点上 可能会影响对比

###Avatar
- 修复了纸娃娃无法识别Link的Bug


##2016.08.07

###共通
- 临时支持了CMST117新增的Lua节点特殊格式


##2016.07.31

###共通
- 修复了JMSwz无法解析的bug
- 加入了一个设置项 允许wz打开时自动排序 这个设置项可通过手动编辑setting文件配置
- wz显示的部分代码整理
- 动画部分代码整理 以兼容新的MapRender 

###CharaSim
- 支持显示KMST新增属性incARC

###Patcher
- 支持了新的64位长度标记的Patch.exe文件格式


##2016.07.11

###共通
- 修复了IndexGifEncoder编码纯色帧导致gif文件损坏的bug
- 修复了MonoGame多Device共存可能出现的bug

###CharaSim
- Eval计算支持了KMST新的log公式，说不定未来会用到
- 修复了属性Eval计算时因公式出现空白符解析失败的bug

###MapRender
- Xna引擎升级至MonoGame，功能部分恢复到之前的版本，并未支持KMST的压缩版客户端资源链接方式
- 暂时移除没用的输入法和聊天模块

