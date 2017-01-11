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

------------------------------------------------------------

-- all variables
local topNode = PluginManager.FindWz('Etc')
local outputDir = "D:\\wzDump"

------------------------------------------------------------
-- main function

if not topNode then
  env:WriteLine('Base.wz not loaded.')
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
    
      --dump as Xml
      local xmlFileName = outputDir.."\\"..(n.FullPathToFile)..".xml"
      local dir = Path.GetDirectoryName(xmlFileName)
    
      --ensure dir exists
      if not Directory.Exists(dir) then
        Directory.CreateDirectory(dir)
      end
      
      --create file
      env:WriteLine('(output)'..xmlFileName)
      local fs = File.Create(xmlFileName)
      local xw = XmlWriter.Create(fs)
      
      xw:WriteStartDocument(true);
      Wz_NodeExtension.DumpAsXml(img.Node, xw)
      xw:WriteEndDocument()
      
      xw:Flush()
      fs:Close()
      env:WriteLine('(close)'..xmlFileName)
      
      img:Unextract()
      
    else --error
      
      env:WriteLine((img.Name)..' extract failed.')
      
    end --end extract
  end -- end type validate
end -- end foreach

env:WriteLine('--------Done.---------')