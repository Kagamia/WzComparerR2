import 'System.IO'

function isWzImage(value)
  return value and type(value) == "userdata" 
    and (value:GetType().Name == 'Wz_Image' or value:GetType().Name == 'Ms_Image')
end

function enumAllWzNodes(node) 
  return coroutine.wrap(function()
    coroutine.yield(node)
    for _,v in each(node.Nodes) do
      for child in enumAllWzNodes(v) do
        coroutine.yield(child)
      end
    end
  end)
end

local p = Path.GetInvalidFileNameChars()
local ivStr = ""
for i, v in each(p) do
  if v >= 32 then
    ivStr = ivStr .. string.char(v)
  end
end
local ivPattern = "["..ivStr.."]"

function removeInvalidPathChars(fileName)
  return fileName:gsub(ivPattern, "")
end

