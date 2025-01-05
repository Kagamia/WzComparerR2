using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;

namespace WzComparerR2.Encoders
{
    public class FFmpegEncoder : GifEncoder
    {
        public static readonly string DefaultExecutionFileName = "ffmpeg";
        /// <summary>
        /// Default ffmpeg argument format to encode mp4 with avc H.264 video format.
        /// </summary>
        public static readonly string DefaultArgumentFormat = @$"-y -f rawvideo -pixel_format bgra -s %w*%h -r 1000/%t -i ""%i"" -vf ""crop=trunc(iw/2)*2:trunc(ih/2)*2"" -vcodec libx264 -pix_fmt yuv420p ""%o""";

        public static readonly string DefaultOutputFileExtension = ".mp4";

        public FFmpegEncoder()
        {
        }

        public string FFmpegBinPath { get; set; }
        public string FFmpegArgumentFormat { get; set; }
        public string OutputFileExtension { get; set; }

        private Process ffmpegProc;
        private NamedPipeServerStream server;
        private CancellationTokenSource ffmpegProcExitedCts;
        private StringBuilder ffmpegStdout = new StringBuilder();
        private StringBuilder ffmpegStderr = new StringBuilder();
        private bool disposed;

        public override GifEncoderCompatibility Compatibility => new GifEncoderCompatibility()
        {
            IsFixedFrameRate = true,
            MinFrameDelay = 1,
            MaxFrameDelay = int.MaxValue,
            FrameDelayStep = 1,
            AlphaSupportMode = AlphaSupportMode.NoAlpha,
            DefaultExtension = string.IsNullOrEmpty(OutputFileExtension) ? DefaultOutputFileExtension : OutputFileExtension,
            SupportedExtensions = new[] { ".*" },
        };

        public unsafe override void AppendFrame(IntPtr pBuffer, int delay)
        {
            if (server == null && ffmpegProc == null)
            {
                StartFFmpeg(delay).Wait();
            }

            int frameDataLen = Width * Height * 4;
            using UnmanagedMemoryStream ms = new((byte*)pBuffer.ToPointer(), frameDataLen, frameDataLen, FileAccess.Read);
            ms.CopyToAsync(server, 32768, ffmpegProcExitedCts?.Token ?? default).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task StartFFmpeg(int delay)
        {
            // create random pipe name
            string pipeName = $"{nameof(FFmpegEncoder)}-{Process.GetCurrentProcess().Id}-{(uint)Environment.TickCount}";

            // create named-pipe server and listen
            NamedPipeServerStream server = new(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte);
            var task1 = server.WaitForConnectionAsync();

            // start ffmpeg
            ProcessStartInfo psi = new()
            {
                FileName = string.IsNullOrEmpty(FFmpegBinPath) ? DefaultExecutionFileName : FFmpegBinPath,
                Arguments = SubstituteParams(string.IsNullOrEmpty(FFmpegArgumentFormat) ? DefaultArgumentFormat : FFmpegArgumentFormat,
                    @$"\\.\pipe\{pipeName}", Width, Height, delay, FileName),
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Process ffmpegProc = new()
            {
                StartInfo = psi,
            };
            ffmpegProc.OutputDataReceived += FFmpegProc_OutputDataReceived;
            ffmpegProc.ErrorDataReceived += FFmpegProc_ErrorDataReceived;
            ffmpegProc.Exited += FFmpegProc_Exited;
            ffmpegProc.Start();
            ffmpegProc.BeginOutputReadLine();
            ffmpegProc.BeginErrorReadLine();
            await task1.ConfigureAwait(false);

            this.server = server;
            this.ffmpegProc = ffmpegProc;
            ffmpegProcExitedCts = new CancellationTokenSource();
        }

        private void FFmpegProc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            ffmpegStdout?.AppendLine(e.Data);
        }

        private void FFmpegProc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ffmpegStderr?.AppendLine(e.Data);
        }

        private void FFmpegProc_Exited(object sender, EventArgs e)
        {
            ffmpegProcExitedCts?.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposed)
            {
                if (disposing)
                {
                    if (server != null)
                    {
                        if (server.IsConnected)
                        {
                            server.Flush();
                            server.Disconnect();
                        }
                        server.Dispose();
                    }
                    if (ffmpegProc != null)
                    {
                        if (!ffmpegProc.HasExited)
                        {
                            ffmpegProc.WaitForExit();
                        }
                        ffmpegProc.Dispose();
                    }
                    if (ffmpegProcExitedCts != null)
                    {
                        ffmpegProcExitedCts.Dispose();
                    }
                }

                ffmpegStdout = null;
                ffmpegStderr = null;
                disposed = true;
            }
        }

        private string SubstituteParams(string format, string inputFileName, int width, int height, int frameDelay, string outputFileName)
        {
            // %i: inputFileName
            // %w: width
            // %h: height
            // %t: frameDelay
            // %o: outputFileName
            // %%: escape '%' char
            return Regex.Replace(format, "%[iwhto%]", match => match.Value switch
            {
                "%i" => inputFileName,
                "%w" => width.ToString(),
                "%h" => height.ToString(),
                "%t" => frameDelay.ToString(),
                "%o" => outputFileName,
                "%%" => "%",
                _ => throw new FormatException($"Unknown format: {match.Value}")
            });
        }
    }
}
