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

local function isSound(value)
  if value and type(value) == "userdata" and value:GetType().Name == 'Wz_Sound' then
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
local topWzPath = 'Sound\\Bgm00.img'
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
  local img = Wz_NodeExtension.GetNodeWzImage(n)
  
  if img then
    --extract wz image
    env:WriteLine('(extract)'..(img.Name))
    if img:TryExtract() then
    
      local dir = outputDir.."\\"..(n.FullPathToFile)
      local dirCreated = false
      
      --find all sound
      for n2 in enumAllWzNodes(img.Node) do
        local sound = n2.Value
        if isSound(sound) then
          
          local fn = n2.FullPath:sub(img.Name:len()+2):gsub("\\", "."):gsub(ivPattern, "")
          if not n2.Text:find("\\.") then
            if sound.SoundType == Wz_SoundType.Mp3 then
              fn = fn .. ".mp3"
            end
            if sound.SoundType == Wz_SoundType.WavRaw then
              fn = fn .. ".wav"
            end
          end
          fn = Path.Combine(dir, fn)
          
          --ensure dir exists
          if not dirCreated then
            if not Directory.Exists(dir) then
              Directory.CreateDirectory(dir)
            end
            dirCreated = true
          end
          
          --save sound
          env:WriteLine('(output)'..fn)
          File.WriteAllBytes(fn, sound:ExtractSound())
          env:WriteLine('(close)'..fn)
          
        end
      end
      
      img:Unextract()
    else --error
      
      env:WriteLine((img.Name)..' extract failed.')
      
    end --end extract
  end -- end type validate
end -- end foreach

env:WriteLine('--------Done.---------')