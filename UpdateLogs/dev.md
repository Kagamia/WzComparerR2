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

