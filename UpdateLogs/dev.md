## 2017.7.23

### 共通
- GearGraphics.TextRenderer重构，排版与渲染分离，支持Monogame。

### MapRender
- 完整支持了UIWorldMap。


## 2017.7.20

### 基础
- 修复了技能编号8001xxxx无法自动链接的bug。


## 2017.7.8
积累更新

### 共通
- 因为发现了一个特殊的wz结构，调整了全部解决uol引用的代码。这影响到了全部动画加载相关的模块。

### WzLib
- 添加了可以支持自动挂接扩展wz文件(mob2, map2, etc.)的选项。

### MapRender
- 修复了一些可能造成内存泄漏的bug（但并无卵用）。