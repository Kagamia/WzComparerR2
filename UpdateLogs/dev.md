## 2018.4.13

### 共通
- 修复了BGR565格式纹理对于win8前系统的支持。

### MapRender
- 屏蔽了因输入框失去焦点导致的按键处理异常的错误。

### Patcher
- 添加了CMS的补丁地址。


## 2018.4.10

### MapRender
- 修复下拉菜单点击无效的bug。
- 支持更多交互命令。


## 2018.4.9

### MapRender
- 支持隐藏NPC和怪物名字。
- 更新了MessageBox样式。
- 修复了UIChatBox渲染模糊和操作上的bug。


## 2018.4.8

### WzLib
- 修复了format517图片解码错误的bug。

### MapRender
- 添加UIMessageBox用于各种提示信息。
- 添加UIChatBox，并支持了输入法，用于交互和显示提示。
- 临时修复粒子系统的渲染错误。
- UIWorldmap支持右键返回。
- 修复了back图层的渲染错误，支持blend模式。
- UI布局略微调整。


## 2018.3.23

### CharaSim
- 支持了KMST1066版本拆分的03xx.img物品识别。


## 2018.3.22

### Wz提取
- 修复了一个可能导致Gif/Apng导出有锯齿的bug。

### WzLib
- 修复了GetValue()导致搜索效率低下的bug。
- 调整string.intern过滤条件，减少字符串池常驻内存占用。

### MapRender
- UIWorldMap支持更丰富的tooltip信息，并且支持点击传送了。
- 修复UI绘图资源的潜在内存泄露。

