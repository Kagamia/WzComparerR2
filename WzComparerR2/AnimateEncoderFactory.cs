using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.Common;
using WzComparerR2.Config;

namespace WzComparerR2
{
    public class AnimateEncoderFactory
    {
        private AnimateEncoderFactory()
        {

        }

        public static AnimateEncoderParams GetEncoderParams(int encoderID)
        {
            switch (encoderID)
            {
                default:
                case 0:
                    return new AnimateEncoderParams()
                    {
                        ID = 0,
                        EncoderType = typeof(BuildInGifEncoder),
                        FileExtension = ".gif",
                        FileDescription = "Gif图片",
                        SupportAlphaChannel = false,
                    };

                case 1:
                    return new AnimateEncoderParams()
                    {
                        ID = 1,
                        EncoderType = typeof(BuildInGifEncoder),
                        FileExtension = ".gif",
                        FileDescription = "Gif图片",
                        SupportAlphaChannel = false,
                    };

                case 2:
                    return new AnimateEncoderParams()
                    {
                        ID = 2,
                        EncoderType = typeof(BuildInGifEncoder),
                        FileExtension = ".png",
                        FileDescription = "APng图片",
                        SupportAlphaChannel = true,
                    };
            }
        }

        public static GifEncoder CreateEncoder(string fileName, int width, int height, ImageHandlerConfig config)
        {
            switch (config.GifEncoder.Value)
            {
                default:
                case 0:
                    {
                        var enc = new BuildInGifEncoder(fileName, width, height);
                        return enc;
                    }

                case 1:
                    {
                        var enc = new IndexGifEncoder(fileName, width, height);
                        return enc;
                    }

                case 2:
                    {
                        var enc = new BuildInApngEncoder(fileName, width, height);
                        enc.OptimizeEnabled = config.PaletteOptimized;
                        return enc;
                    }
            }
        }
    }

    public class AnimateEncoderParams
    {
        public int ID { get; set; }
        public Type EncoderType { get; set; }
        public string FileExtension { get; set; }
        public string FileDescription { get; set; }
        
        public bool SupportAlphaChannel { get; set; }
    }
}
