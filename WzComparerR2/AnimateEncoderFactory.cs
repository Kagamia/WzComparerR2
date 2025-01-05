using System;
using System.Collections.Generic;
using WzComparerR2.Config;
using WzComparerR2.Encoders;

namespace WzComparerR2
{
    public static class AnimateEncoderFactory
    {
        static AnimateEncoderFactory()
        {
            registeredEncoders = new Dictionary<int, IAnimateEncoderProvider>();
            RegisterEncoders();
        }

        private static Dictionary<int, IAnimateEncoderProvider> registeredEncoders;

        private static void RegisterEncoders()
        {
            registeredEncoders.Add(0, new AnimateEncoderProvider<BuildInGifEncoder>
            {
                ID = 0,
                Name = nameof(BuildInGifEncoder),
                CreateEncoderCallback = () => new BuildInGifEncoder(),
            });

            registeredEncoders.Add(1, new AnimateEncoderProvider<IndexGifEncoder>
            {
                ID = 1,
                Name = nameof(IndexGifEncoder),
                CreateEncoderCallback = () => new IndexGifEncoder(),
            });

            registeredEncoders.Add(2, new AnimateEncoderProvider<BuildInApngEncoder>
            {
                ID = 2,
                Name = nameof(BuildInApngEncoder),
                CreateEncoderCallback = () => new BuildInApngEncoder(),
                ConfigureEncoderCallback = (encoder, config) =>
                {
                    encoder.OptimizeEnabled = config.PaletteOptimized;
                }
            });

            registeredEncoders.Add(3, new AnimateEncoderProvider<FFmpegEncoder>
            {
                ID = 3,
                Name = nameof(FFmpegEncoder),
                CreateEncoderCallback = () => new FFmpegEncoder(),
                ConfigureEncoderCallback = (encoder, config) =>
                {
                    encoder.FFmpegBinPath = config.FFmpegBinPath;
                    encoder.FFmpegArgumentFormat = config.FFmpegArgument;
                    encoder.OutputFileExtension = config.FFmpegOutputFileExtension;
                }
            });
        }

        public static GifEncoder CreateEncoder(ImageHandlerConfig config)
        {
            return CreateEncoder(config.GifEncoder, config);
        }

        public static GifEncoder CreateEncoder(int id, ImageHandlerConfig config)
        {
            if (!registeredEncoders.TryGetValue(id, out var provider))
            {
                throw new Exception($"Encoder ID {id} has not registered");
            }

            var encoder = provider.CreateEncoder();
            provider.ConfigureEncoder(encoder, config);
            return encoder;
        }

        public interface IAnimateEncoderProvider
        {
            GifEncoder CreateEncoder();
            void ConfigureEncoder(GifEncoder encoder, ImageHandlerConfig config);
        }

        public class AnimateEncoderProvider<T> : IAnimateEncoderProvider where T : GifEncoder
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public Func<T> CreateEncoderCallback { get; set; }
            public Action<T, ImageHandlerConfig> ConfigureEncoderCallback { get; set; }

            public GifEncoder CreateEncoder()
            {
                if (this.CreateEncoderCallback == null)
                {
                    throw new ArgumentNullException(nameof(CreateEncoderCallback));
                }

                return this.CreateEncoderCallback();
            }

            public void ConfigureEncoder(GifEncoder encoder, ImageHandlerConfig config)
            {
                if (this.ConfigureEncoderCallback != null)
                {
                    this.ConfigureEncoderCallback((T)encoder, config);
                }
            }
        }
    }
}
