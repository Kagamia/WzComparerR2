import 'WzComparerR2.PluginBase'
import 'WzComparerR2.WzLib'
import 'System.IO'
import 'System.Xml'

require 'Helper'

------------------------------------------------------------

local function isPng(value)
  return value and type(value) == "userdata" and value:GetType().Name == 'Wz_Png'
end

------------------------------------------------------------

-- all variables
local topWzPath = 'Character'
local topNode = PluginManager.FindWz(topWzPath)
local outputDir = "D:\\wzDump"
local errorList = {}  -- collect error messages here

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
    env:WriteLine('(extract) ' .. (img.Name))
    local success, extractErr = pcall(function()
      if img:TryExtract() then
        local dir = outputDir .. "\\" .. (n.FullPathToFile)
        local dirCreated = false

        for n2 in enumAllWzNodes(img.Node) do
          local png = n2.Value
          if isPng(png) and (png.Width > 1 or png.Height > 1) then
            local fn = n2.FullPath:sub(img.Name:len() + 2):gsub("\\", ".")
            fn = removeInvalidPathChars(fn)
            fn = Path.Combine(dir, fn .. ".png")

            if not dirCreated then
              if not Directory.Exists(dir) then
                Directory.CreateDirectory(dir)
              end
              dirCreated = true
            end

            -- Try saving PNG
            local successSave, saveErr = pcall(function()
              local bmp = png:ExtractPng()
              if bmp then
                bmp:Save(fn)
                bmp:Dispose()
              else
                error("ExtractPng() returned nil.")
              end
            end)

            if not successSave then
              local errMsg = string.format("Error saving PNG [%s]: %s", n2.FullPath, saveErr)
              env:WriteLine(errMsg)
              table.insert(errorList, errMsg)
            end
          end
        end
        img:Unextract()
      else
        local errMsg = string.format("%s extract failed.", img.Name)
        env:WriteLine(errMsg)
        table.insert(errorList, errMsg)
      end
    end)

    if not success then
      local errMsg = string.format("Unexpected error in image [%s]: %s", img.Name, extractErr)
      env:WriteLine(errMsg)
      table.insert(errorList, errMsg)
    end
  end
end

-- Final error report
if #errorList > 0 then
  env:WriteLine("-------- Error Summary --------")
  for i, err in ipairs(errorList) do
    env:WriteLine(err)
  end
end

env:WriteLine('-------- Done. --------')