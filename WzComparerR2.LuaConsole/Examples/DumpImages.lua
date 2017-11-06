import 'WzComparerR2.PluginBase'
import 'WzComparerR2.WzLib'
import 'System.IO'
import 'System.Xml'

------------------------------------------------------------

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

local function isPng(value)
  if value and type(value) == "userdata" and value:GetType().Name == 'Wz_Png' then
    return true
  else 
    return false
  end
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
local topWzPath = 'Character'
local topNode = PluginManager.FindWz(topWzPath)
local outputDir = "D:\\wzDump"

------------------------------------------------------------
-- main function

if not topNode then
  env:WriteLine('"{0}" not loaded.', topWzPath)
  return
end



-- enum all wz_images
for n in enumAllWzNodes(topNode) do
  local value = n.Value
  if value and type(value) == "userdata" and value:GetType().Name == 'Wz_Image' then
    local img = value

    --extract wz image
    env:WriteLine('(extract)'..(img.Name))
    if img:TryExtract() then
    
      local dir = outputDir.."\\"..(n.FullPathToFile)
      local dirCreated = false
      
      --find all png
      for n2 in enumAllWzNodes(img.Node) do
        local png = n2.Value
        if isPng(png) and (png.Width>1 or png.Height>1) then
          
          local fn = n2.FullPath:sub(img.Name:len()+2):gsub("\\", "."):gsub(ivPattern, "")
          fn = Path.Combine(dir, fn .. ".png")
          
          --ensure dir exists
          if not dirCreated then
            if not Directory.Exists(dir) then
              Directory.CreateDirectory(dir)
            end
            dirCreated = true
          end
          
          --save as png
          local bmp = png:ExtractPng()
          bmp:Save(fn)
          bmp:Dispose()
          
        end
      end
      
      img:Unextract()
    else --error
      
      env:WriteLine((img.Name)..' extract failed.')
      
    end --end extract
  end -- end type validate
end -- end foreach

env:WriteLine('--------Done.---------')