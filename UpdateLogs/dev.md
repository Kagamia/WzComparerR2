## 2018.7.5

### 共通
- 添加了一个api以利于插件输出error.log。
- 更新了buildin gif encoder以修复某些场合导出错误的bug。

### WzLib
- 修复了上次修复Uol链接推断的bug。
- 修复了上次新增支持读取独立的img文件的bug。

### CharaSim
- 修复某些动作下发型渲染错误的bug。
- 支持了宠物套装的渲染，添加宠物过期时间。
- 支持显示GMS按时间奖励的套装效果。

### MapRender
- 调整了粒子系统渲染效果。
- Worldmap支持了KMST1070的QuestLimit分阶段渲染机制。


## 2018.6.20

### 共通
- 支持了按照imgID进行排序。
- 配置文件中默认开启wz自动排序和自动加载扩展wz。
- File-Option里添加了一些新的配置项。

### WzLib
- 修复了wz类型推断的bug。
- 修复Uol链接推断的bug。
- 支持读取独立的img文件。
- 支持跳过img校验和检测，以识别老旧版本的客户端。

### CharaSim
- 称号支持模拟包含任务数量宏字符串。
- 添加Ark特殊装备类型的模拟。
- 添加KMST1069的新属性nbdR识别。

### MapRender
- 修复了阿斯旺地图特有的wz怪物声明的识别。


## 2018.4.25

### WzLib
- 重新设计数据结构，以减少内存占用。

### CharaSim
- 支持套装显示点装图标。
- 支持徽章的tag显示。
- 龙神的v5技能模拟可以超过等级上限了。
- 重新设计StringLinker数据结构，以减少内存占用。

### MapRender
- 修复了MapRender窗口反复开关引发的的内存泄露。


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

