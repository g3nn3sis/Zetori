// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Zetori.Replay.Render
{
    internal sealed class VideoEncoder : IDisposable
    {
        private const string Preset = "fast";
        private const int Crf = 16;
        private const string AudioBitrate = "320k";
        private const int KeptErrorLines = 20;
        private const int AbortWaitMilliseconds = 2000;
        private const int FinishTimeoutMilliseconds = 60000;
        private const int ErrorDrainMilliseconds = 2000;

        private const int BytesPerPixel = 3;

        private readonly Process process;
        private readonly string outputPath;
        private readonly int frameSize;
        private readonly BlockingCollection<byte[]> frames = new BlockingCollection<byte[]>();
        private readonly ConcurrentBag<byte[]> spareFrames = new ConcurrentBag<byte[]>();
        private readonly Queue<string> errorLines = new Queue<string>();
        private readonly ManualResetEventSlim errorsEnded = new ManualResetEventSlim();
        private readonly Task writer;
        private int pending;
        private volatile bool faulted;
        private bool finished;

        private VideoEncoder(Process process, VideoFormat format, string outputPath)
        {
            this.process = process;
            this.outputPath = outputPath;
            frameSize = format.Width * format.Height * BytesPerPixel;
            process.ErrorDataReceived += (_, e) => KeepErrorLine(e.Data);
            process.BeginErrorReadLine();
            writer = Task.Factory.StartNew(WriteFrames, TaskCreationOptions.LongRunning);
        }

        internal int Pending => Volatile.Read(ref pending);

        internal bool Faulted => faulted || writer.IsFaulted;

        internal static VideoEncoder Start(string ffmpegPath, VideoFormat format, string audioPath, string outputPath)
        {
            var info = new ProcessStartInfo(ffmpegPath, Arguments(format, audioPath, outputPath))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };
            Process process = Process.Start(info) ?? throw new InvalidOperationException("FFmpeg did not start.");
            try
            {
                return new VideoEncoder(process, format, outputPath);
            }
            catch
            {
                Stop(process);
                process.Dispose();
                throw;
            }
        }

        internal static string Arguments(VideoFormat format, string audioPath, string outputPath)
        {
            string video = $"-f rawvideo -pix_fmt rgb24 -video_size {format.Width}x{format.Height} -framerate {format.FrameRate} -i pipe:0";
            string conversion = "-vf vflip,scale=in_primaries=bt709:in_transfer=bt709:out_primaries=bt709:out_transfer=bt709:out_color_matrix=bt709:out_range=tv,format=yuv420p";
            string encoding = $"-c:v libx264 -preset {Preset} -crf {Crf} -g {format.FrameRate} -profile:v high";
            string tags = "-colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv";
            return $"-hide_banner -nostats -loglevel error -y {video} -i \"{audioPath}\" -map 0:v:0 -map 1:a:0 {conversion} {encoding} {tags} -c:a aac -b:a {AudioBitrate} \"{outputPath}\"";
        }

        internal byte[] RentFrame() => spareFrames.TryTake(out byte[]? spare) ? spare : new byte[frameSize];

        internal void Write(byte[] frame)
        {
            if (frame.Length != frameSize) throw new ArgumentException($"A frame must be {frameSize} bytes, not {frame.Length}.", nameof(frame));
            Interlocked.Increment(ref pending);
            frames.Add(frame);
        }

        internal Task Finish()
        {
            frames.CompleteAdding();
            return Task.Run(() =>
            {
                writer.Wait();
                CloseInput();
                bool exited = process.WaitForExit(FinishTimeoutMilliseconds);
                if (!exited) Stop(process);
                errorsEnded.Wait(ErrorDrainMilliseconds);
                finished = true;
                if (exited && process.ExitCode == 0 && !faulted) return;
                RemoveOutput();
                throw new InvalidOperationException($"{FailureSummary(exited)}:\n{ErrorText()}");
            });
        }

        internal bool Abort()
        {
            if (!frames.IsAddingCompleted) frames.CompleteAdding();
            Stop(process);
            finished = true;
            return RemoveOutput();
        }

        public void Dispose()
        {
            if (!finished) Abort();
            bool writerEnded = writer.Wait(AbortWaitMilliseconds);
            process.Dispose();
            errorsEnded.Dispose();
            if (writerEnded) frames.Dispose();
        }

        private void WriteFrames()
        {
            Stream input = process.StandardInput.BaseStream;
            foreach (byte[] frame in frames.GetConsumingEnumerable())
            {
                try
                {
                    if (!faulted) input.Write(frame, 0, frame.Length);
                }
                catch (Exception)
                {
                    faulted = true;
                }
                finally
                {
                    spareFrames.Add(frame);
                    Interlocked.Decrement(ref pending);
                }
            }
        }

        private string FailureSummary(bool exited)
        {
            if (!exited) return $"FFmpeg did not finish within {FinishTimeoutMilliseconds / 1000} s";
            if (process.ExitCode == 0) return "FFmpeg stopped taking frames before the render ended";
            return $"FFmpeg exited with code {process.ExitCode}";
        }

        private static void Stop(Process process)
        {
            if (!process.HasExited) TryKill(process);
            process.WaitForExit(AbortWaitMilliseconds);
        }

        private static bool TryKill(Process process)
        {
            try
            {
                process.Kill();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private void CloseInput()
        {
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                faulted = true;
            }
        }

        private void KeepErrorLine(string? line)
        {
            if (line == null) errorsEnded.Set();
            if (string.IsNullOrWhiteSpace(line)) return;
            lock (errorLines)
            {
                errorLines.Enqueue(line!);
                if (errorLines.Count > KeptErrorLines) errorLines.Dequeue();
            }
        }

        private string ErrorText()
        {
            lock (errorLines) return string.Join("\n", errorLines);
        }

        private bool RemoveOutput()
        {
            try
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
