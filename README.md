# WzComparerR2
-Translated for English use by PirateIzzy (work in progress)

- NOTE: As of November 21, 2020, you will need to re-clone this repository if you cloned it before.

# Modules
- **WzComparerR2** 
- **WzComparerR2.Common** 
- **WzComparerR2.PluginBase** 
- **WzComparerR2.WzLib** 
- **CharaSimResource** 
- **WzComparerR2.LuaConsole** 
- **WzComparerR2.MapRender**
- **WzComparerR2.Avatar**

# Prerequisite
- **2.x**: Win7sp1+/.net4.5.2+/dx11.0
- **1.x**: WinXp+/.net2.0+/dx9.0

# Installation
```sh
git clone --recurse-submodules -j8 git://github.com/Kagamia/WzComparerR2.git
```
Clone repository with submodules.

# Compile
- vs2017 or higher

# How to Use:
- Download **WzComparerR2.zip** from [Releases](https://github.com/PirateIzzy/WzComparerR2-Code/releases) and extract the ZIP to a location of your choice (note that you may need to make adjustments in your AntiVirus's settings for it to work).

- **To view WZ files**
- Open the Base.wz of a MapleStory version of your choice. Right click and Sort to organize the files alphanumerically (if not automatically sorted already).
- Under Modules, click the QuickView dropdown and select AutoQuickView (for easier viewing).
- Take a look at the data of your choice under the WzView tab.

- **To compare WZ files**
- Open two of the same .wz file from the same MapleStory version (for example, 2 Base.wz files of GMS 192 and 193). Note that Base.wz will provide all the changes.
- Select the WzCompare tab and check/uncheck the 4 boxes. Uncheck ResolvePngLink to make the comparison faster, and click EasyCompare.
- Select the location for your comparisons. After doing so, select the old/new .wz files in the prompt box. Click OK to start the comparison (note: will take a while for some versions of MapleStory)

- **Note for Mob.wz**
- I removed the display of Knockback and Avoidability because these two stats are irrelevant as of the V update, and have no impact on monsters now. As to why Nexon keeps including it, who knows. I also have no clue what mobType is for.
- The last part of the Mob AutoQuickView, ILFSHDP, refers to elements Ice, Lightning, Fire, Poison, Holy, Dark, Physical (respectively). - 0=Neutral (no special elemental advantages/penalties), 1=Immune (element does 1 damage), 2=Resistant (element does half damage), 3=Weak (element does +50% final damage)

# Credits and Acknowledgement
- **Fiel** ([Southperry](http://www.southperry.net))  
- **Index** ([Exrpg](http://bbs.exrpg.com/space-uid-137285.html)) 
- [DotNetBar](http://www.devcomponents.com/)
- [SharpDX](https://github.com/sharpdx/SharpDX) & [Monogame](https://github.com/MonoGame/MonoGame)
- [BassLibrary](http://www.un4seen.com/)
- [IMEHelper](https://github.com/JLChnToZ/IMEHelper)
- [Spine-Runtime](https://github.com/EsotericSoftware/spine-runtimes)
- [EmptyKeysUI](https://github.com/EmptyKeys)
- **[@KENNYSOFT](https://github.com/KENNYSOFT)** and his WcR2-KMS version.
- **[@Kagamia](https://github.com/Kagamia)** and his WcR2-CMS version.
- **[@Spadow](https://github.com/Sunaries)** and his assistance on translations.
	
