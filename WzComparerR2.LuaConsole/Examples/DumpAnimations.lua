import 'WzComparerR2.PluginBase'
import 'WzComparerR2.WzLib'
import 'WzComparerR2.Common'
import 'WzComparerR2.Encoders'
import 'System.IO'
import 'System.Xml'
import 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
import 'System.Drawing'
import 'System.Drawing.Imaging'

require 'Helper'

------------------------------------------------------------

local function isPng(value)
  return value and type(value) == "userdata" and value:GetType().Name == 'Wz_Png'
end

local function isDelay(node)
  return node.Text == "delay" and node:GetType().Name == 'Wz_Node'
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

local function findNodeFunc(path)
  return PluginManager.FindWz(path)
end

local t_IGifFrame = {}
t_IGifFrame.typeRef = luanet.import_type('WzComparerR2.Common.IGifFrame')
t_IGifFrame.Draw = luanet.get_method_bysig(t_IGifFrame.typeRef, 'Draw', "System.Drawing.Graphics", "System.Drawing.Rectangle")

local function saveAnimatedImage(node, fileName)
  local gif = Gif.CreateFromNode(node, findNodeFunc)
  local rect = gif:GetRect()
  local enc = BuildInApngEncoder()
  enc:Init(fileName, rect.Width, rect.Height)
  enc.OptimizeEnabled = false
  
  for i,frame in each(gif.Frames) do
    local bmp = Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb)
    local g = Graphics.FromImage(bmp)
    t_IGifFrame.Draw(frame, g, rect)
    g:Dispose()
    enc:AppendFrame(bmp, frame.Delay)
    bmp:Dispose()
    bmp = nil
  end
  
  enc:Dispose()
  
  for i,frame in each(gif.Frames) do
    frame.Bitmap:Dispose();
  end
  gif = nil
end

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
          local fn = fullpath:sub(img.Name:len()+2):gsub("\\", ".")
          fn = removeInvalidPathChars(fn)
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