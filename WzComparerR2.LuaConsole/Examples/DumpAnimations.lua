import 'WzComparerR2.PluginBase'
import 'WzComparerR2.WzLib'
import 'WzComparerR2.Common'
import 'System.IO'
import 'System.Xml'
import 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
import 'System.Drawing'

------------------------------------------------------------

local function isPng(value)
  if value and type(value) == "userdata" and value:GetType().Name == 'Wz_Png' then
    return true
  else 
    return false
  end
end

local function isDelay(node)
  if node.Text == "delay" and node:GetType().Name == 'Wz_Node' then
    return true
  else 
    return false
  end
end

local function enumAllWzNodes(node) 
  return coroutine.wrap(function()
    coroutine.yield(node)
    for _,v in each(node.Nodes) do
      for child in enumAllWzNodes(v) do
        coroutine.yield(child)
      end
    end
  end)
end

local function isPngWithDelay(node)
  if isPng(node.Value) then
    for n in enumAllWzNodes(node) do
      if isDelay(n) then
        return true
      end
    end
  end
  return false
end

local function enumAllWzPngNodesWithDelay(node) 
  return coroutine.wrap(function()
    if isPng(node.Value) and isPngWithDelay(node) then
      coroutine.yield(node)
    end
    for _, n in each(node.Nodes) do
      for child in enumAllWzPngNodesWithDelay(n) do
        coroutine.yield(child)
      end
    end
  end)
end

local function saveAnimatedImage(node, fn)
  local gif = Gif.CreateFromNode(node, null)
  local rect = gif:GetRect()
  local enc = BuildInApngEncoder(fn, rect.Width, rect.Height)
  enc.OptimizeEnabled = false
  gif:SaveGif(enc, fn, Color.Transparent)
end

local p = Path.GetInvalidFileNameChars()
local ivStr = ""
for i, v in each(p) do
  if v >= 32 then
    ivStr = ivStr .. string.char(v)
  end
end
local ivPattern = "["..ivStr.."]"
------------------------------------------------------------

-- all variables
local topWzPath = 'Skill'
local topNode = PluginManager.FindWz(topWzPath)
local outputDir = "D:\\wzDump"

------------------------------------------------------------
-- main function

if not topNode then
  env:WriteLine('"{0}" not loaded.', topWzPath)
  return
end

for n in enumAllWzNodes(topNode) do
  local img = Wz_NodeExtension.GetNodeWzImage(n)
  if img then
    --extract wz image
    env:WriteLine('(extract) '..(img.Name))

    if img:TryExtract() then
      local dir = outputDir.."\\"..(n.FullPathToFile)
      local dirCreated = false
      local fullpaths = {}

      for n2 in enumAllWzPngNodesWithDelay(img.Node) do
        local parentNode = n2.ParentNode
        local fullpath = parentNode.FullPath

        -- Skip if 'fullpath' already done
        if (not fullpaths[fullpath]) then
          local fn = fullpath:sub(img.Name:len()+2):gsub("\\", "."):gsub(ivPattern, "")
          fn = Path.Combine(dir, fn .. ".apng")

          --ensure dir exists
          if not dirCreated then
            if not Directory.Exists(dir) then
              Directory.CreateDirectory(dir)
            end
            dirCreated = true
          end

          saveAnimatedImage(parentNode, fn)
          
          fullpaths[fullpath] = true
        end
      end
      
      img:Unextract()

    else --error
      env:WriteLine((img.Name)..' extract failed.')
      
    end --end extract
  end -- end type validate
end -- end foreach

env:WriteLine('--------Done.---------')