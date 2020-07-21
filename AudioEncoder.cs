using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg.Events;

namespace KiriMusicHelper {
    public class AudioConversionItem
    {
        public FileInfo InputFile;
        public string OutputPath;
        public long Bitrate;
        public int SampleRate;
    }



    class AudioEncoder {

        


        public static async void Init() {
            //Set directory where app should look for FFmpeg 
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "FFmpeg"));
            FFmpeg.SetExecutablesPath(Path.Combine(Directory.GetCurrentDirectory(), "FFmpeg"));
            //Get latest version of FFmpeg. It's great idea if you don't know if you had installed FFmpeg.
            await Xabe.FFmpeg.Downloader.FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, FFmpeg.ExecutablesPath);
        }

        public static async Task<IMediaInfo> GetInfo(string path)
        {
            try
            {
                return await FFmpeg.GetMediaInfo(path);
            } catch (Exception ex)
            {
                return null;
            }



            
        }



        public delegate void ConversionProgressEventHandler(
            object sender,
            AudioConversionItem conversion,
            ConversionProgressEventArgs args);

        event ConversionProgressEventHandler OnProgress;


        public delegate void ConversionDoneEventHandler(
            object sender,
            AudioConversionItem conversion,
            IConversionResult result);

        event ConversionDoneEventHandler OnConversionDone;



        private async Task AudioConvert(ConcurrentQueue<AudioConversionItem> filesToConvert, CancellationToken cancelToken)
        {
            while(filesToConvert.TryDequeue(out AudioConversionItem fileToConvert))
            {
                var info = await GetInfo(fileToConvert.InputFile.FullName);


                IStream audioStream = info.AudioStreams.FirstOrDefault()
                    ?.SetCodec(AudioCodec.vorbis)
                    ?.SetBitrate(fileToConvert.Bitrate)
                    ?.SetSampleRate(fileToConvert.SampleRate);

                var conversion = FFmpeg.Conversions.New()
                    .AddStream(audioStream)
                    .SetOutput(fileToConvert.OutputPath);

                conversion.OnProgress += (sender, args) => OnProgress?.Invoke(this, fileToConvert, args);

                var result = await conversion.Start(cancelToken);
                OnConversionDone?.Invoke(this, fileToConvert, result);

            }
        }

        private async Task VideoConvert(ConcurrentQueue<AudioConversionItem> filesToConvert, CancellationToken cancelToken)
        {
            while(filesToConvert.TryDequeue(out AudioConversionItem fileToConvert))
            {
                var info = await GetInfo(fileToConvert.InputFile.FullName);


                IStream audioStream = info.AudioStreams.FirstOrDefault()
                    ?.SetCodec(AudioCodec.vorbis);

                
                var conversion = await FFmpeg.Conversions.FromSnippet
                    .ToOgv(fileToConvert.InputFile.FullName, fileToConvert.OutputPath);

                //ffmpeg -i input.mp4 -c:v libtheora -q:v 7 -c:a libvorbis -q:a 4 intro.ogv


                conversion.OnProgress += (sender, args) => OnProgress?.Invoke(this, fileToConvert, args);

                var result = await conversion.Start(cancelToken);
                OnConversionDone?.Invoke(this, fileToConvert, result);

            }
        }


        //



       


    }
}
