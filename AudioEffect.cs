using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using CSCore;
using CSCore.Codecs;
using CSCore.DSP;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using CSCore.Streams;
using KiriMusicHelper.Annotations;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace KiriMusicHelper
{

    public interface IAudioEffectSource
    {
        Task<ISampleSource> GetSource();
        Task<int> GetSampleRate();
    }

    class AudioEffectSourceConversion : IAudioEffectSource
    {
        string inputPath;
        string tempFilePath;
        bool ready = false;

        public AudioEffectSourceConversion(string path)
        {
            inputPath = path;
            tempFilePath = TempFileManager.Instance.GetNewTempFileExt(".flac");
        }

        public async Task<int> GetSampleRate()
        {
            var info = await AudioEncoder.GetInfo(inputPath);
            return info.AudioStreams.FirstOrDefault()?.SampleRate ?? 0; //#TODO cache samplerate
        }

        public async Task<ISampleSource> GetSource()
        {
            if (!ready) {
                var info = await AudioEncoder.GetInfo(inputPath);

                IStream audioStream = info.AudioStreams.FirstOrDefault()
                    ?.SetCodec(AudioCodec.flac);

                var conversion = FFmpeg.Conversions.New()
                    .AddStream(audioStream)
                    .SetOutput(tempFilePath);

                var result = await conversion.Start();
            }

            var source = CodecFactory.Instance.GetCodec(tempFilePath)
               .ToSampleSource();

            return source;
        }
    }

    class AudioEffectSourceDirect : IAudioEffectSource
    {
        string inputPath;
        public AudioEffectSourceDirect(string path)
        {
            inputPath = path;
        }

        public async Task<int> GetSampleRate()
        {
            var info = await AudioEncoder.GetInfo(inputPath);
            return info.AudioStreams.FirstOrDefault()?.SampleRate ?? 0;
        }

        public async Task<ISampleSource> GetSource()
        {
            var source = CodecFactory.Instance.GetCodec(inputPath)
               .ToSampleSource();

            return source;
        }
    }

    // https://github.com/filoe/cscore/blob/master/Samples/FfmpegSample/Program.cs
    //class AudioEffectSourceFFMPEG : IAudioEffectSource
    //{
    //    string inputPath;
    //    public AudioEffectSourceDirect(string path)
    //    {
    //        inputPath = path;
    //    }
    //
    //    public async Task<int> GetSampleRate()
    //    {
    //        var info = await AudioEncoder.GetInfo(inputPath);
    //        return info.AudioStreams.FirstOrDefault()?.SampleRate ?? 0;
    //    }
    //
    //    public async Task<ISampleSource> GetSource()
    //    {
    //        IWaveSource ffmpegDecoder =
    //            new CSCore.ffFfmpeg.FfmpegDecoder(inputPath)
    //
    //        return source;
    //    }
    //}




    public class AudioEffect//: AudioEffectLivePlay
    {
        public static AudioEffect Instance = new AudioEffect();
        public static IAudioEffectSource CreateSource(string path)
        {
            if (path == null) return null;

            var ext = Path.GetExtension(path);//.TrimStart('.');
            var supportedExtensions = CodecFactory.Instance.GetSupportedFileExtensions();
            if (supportedExtensions.Contains(ext))
                return new AudioEffectSourceDirect(path);
            else
                return new AudioEffectSourceConversion(path);

        }
        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
        public static float TFARSignalLossToErrorLevel(float signalLoss)
        {

            List<float> levels = new List<float>{ 0.0f, 0.150000006f, 0.300000012f, 0.600000024f, 0.899999976f, 0.950000048f, 0.960000038f, 0.970000029f, 0.980000019f, 0.995000005f, 0.997799993f, 0.998799993f, 0.99999f };

            var part = Clamp((int)(signalLoss * 10), 0, levels.Count - 2);
            var from = levels[part];
            var to = levels[part + 1];

            var result = from + (to - from) * (signalLoss - part / 10);
            return result;


        }


        public static async Task DoStuff()
        {
            var test = CodecFactory.SupportedFilesFilterEn;
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = CodecFactory.SupportedFilesFilterEn };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var effect = Instance;
            await effect.SetSource(AudioEffect.CreateSource(openFileDialog.FileName));
            effect.Filters.Add(new HighpassFilter(effect.SampleRate, 520)); // Console.WriteLine("HighpassFilter @4kHz");
            effect.Filters.Add(new LowpassFilter(effect.SampleRate, 1300)); // Console.WriteLine("LowpassFilter @1kHz");
            //effect.Filters.Add(new PeakFilter(effect.SampleRate, 2000, 15, 10)); // Console.WriteLine("PeakFilter @2kHz; bandWidth = 15; gain = 10dB");
            effect.Filters.Add(new FoldbackFilter()); // Console.WriteLine("PeakFilter @2kHz; bandWidth = 15; gain = 10dB");
            effect.Filters.Add(new RingmodulationFilter(effect.SampleRate)); // Console.WriteLine("PeakFilter @2kHz; bandWidth = 15; gain = 10dB");
            //effect.StartLivePlay();
            //var outPath = TempFileManager.Instance.GetNewTempFileExt(".wav");
            //effect.source.ToWaveSource().WriteToFile(outPath);
            //
            //AudioEncoder encoder = new AudioEncoder();
            //
            //var queue = new ConcurrentQueue<AudioConversionItem>();
            //queue.Enqueue(new AudioConversionItem
            //{
            //    Bitrate = 192000,
            //    InputFile = new FileInfo(outPath),
            //    OutputPath = TempFileManager.Instance.GetNewTempFileExt(".ogg"),
            //    SampleRate = effect.SampleRate
            //});
            //
            //await encoder.AudioConvert(queue, CancellationToken.None);
        }


        public BiQuadFilterSource source;
        public ObservableCollection<IFilter> Filters { get; set; } = new ObservableCollection<IFilter>();
        public int SampleRate => source.WaveFormat.SampleRate;

        public void AddFilter(IFilter newFilter)
        {
            lock (Filters)
            {
                Filters.Add(newFilter);
            }

            if (Filters.Any(x => x.NeedFullPass)) DoFilterFullPass();

        }

        public void DoFilterFullPass()
        {
            int index = 0;
            foreach (var filter in Filters)
            {
                if (filter.NeedFullPass)
                {
                    source.DoFullPass(index);
                    filter.FullPassFinished();
                }
                    
                index++;
            }
        }

        public async Task SetSource(IAudioEffectSource src)
        {
            if (src == null) return;
            var newSource = (await src.GetSource()).AppendSource(x => new BiQuadFilterSource(x, this));
            lock (Filters)
            {
                source = newSource;
                //base.SetLiveSource(source);
            }
        }

        public async Task<AudioEffect> CopyFor(FileInfo audioFileInputFile)
        {
            var result = new AudioEffect();
            await result.SetSource(AudioEffect.CreateSource(audioFileInputFile.FullName));

            foreach (var filter in Filters)
            {
                result.Filters.Add(filter.Clone());
            }
            result.DoFilterFullPass();

            return result;
        }

        public string ProcessIntoTempFile()
        {
            var outPath = TempFileManager.Instance.GetNewTempFileExt(".wav");
            source.ToWaveSource().WriteToFile(outPath);
            return outPath;
        }
    }

    public class BiQuadFilterSource : SampleAggregatorBase
    {
        private AudioEffect _effect;

        public BiQuadFilterSource(ISampleSource source, AudioEffect effect) : base(source) 
        {
            _effect = effect;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            if (BaseSource == null)
                return 0;
            lock (_effect.Filters)
            {
                int read = base.Read(buffer, offset, count);
                foreach (var filter in _effect.Filters) 
                {
                    if (filter != null)
                    {
                        filter.PreProcess(buffer, offset, count);
                        for (int i = 0; i < read; i++)
                        {
                            buffer[i + offset] = filter.Process(buffer[i + offset]);
                        }
                    }
                };
                return read;
            }
        }

        public void DoFullPass(int numberOfFilters)
        {
            if (BaseSource == null) return;

            int filterIndex = 0;


            var buffer = new float[base.Length];
            int read = base.Read(buffer, 0, buffer.Length);
            Position = 0;

            foreach (var filter in _effect.Filters)
            {
                filter.PreProcess(buffer, 0, read);

                if (filterIndex == numberOfFilters) filter.FullPassProgress(buffer, read);
                else
                {
                    for (int i = 0; i < read; i++)
                    {
                        buffer[i] = filter.Process(buffer[i]);
                    }
                }

                filterIndex++;
                if (filterIndex > numberOfFilters) break;
                
            };
        }

    }

    public class FoldbackFilter: IFilter
    {
        public float SignalLoss { get; set; } = 0.5f;
        public float errorLevel => AudioEffect.TFARSignalLossToErrorLevel(SignalLoss); //errorLevel
        public float threshold;
        public float Process(float input)
        {
            if (threshold < 0.00001) return 0.0f;
            if (input > threshold || input < -threshold) {
                input = Math.Abs(Math.Abs((float)Math.IEEERemainder(input - threshold, threshold * 4)) - threshold * 2) - threshold;
            }
            return input;
        }

        public void PreProcess(float[] input, int offset, int sampleCount)
        {
            float acc = 0.0f;
            for (int i = 0; i < sampleCount; i++) acc += Math.Abs(input[i + offset]);
            var avg = acc / sampleCount;
            var _base = 0.005;

            var x = avg / _base;
            threshold = (float)(0.3f * (1.0f - errorLevel) * x);
        }
        public void FullPassProgress(float[] input, int sampleCount) { }
        public void FullPassFinished() { }
        public bool NeedFullPass => false;

        public IFilter Clone()
        {
            return new FoldbackFilter { SignalLoss = SignalLoss };
        }
    }

    public class RingmodulationFilter : IFilter
    {

        public float SignalLoss { get; set; } = 0.5f;
        public float mixRate => AudioEffect.TFARSignalLossToErrorLevel(SignalLoss); //errorLevel
        private float phase = 0;
        private float sampleRate;

        public RingmodulationFilter(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public float Process(float input)
        {
            float multiple = (float)(input * Math.Sin(phase * 1.57079632679489661923f)); //#define PI_2     1.57079632679489661923f
            phase += (90.0f * 1.0f / sampleRate);
            if (phase > 1.0f) phase = 0.0f;
            return input * (1.0f - mixRate) + multiple * mixRate;
        }

        public void PreProcess(float[] input, int offset, int sampleCount) { }
        public void FullPassProgress(float[] input, int sampleCount) { }
        public void FullPassFinished() { }
        public bool NeedFullPass => false;

        public IFilter Clone()
        {
            return new RingmodulationFilter((int)sampleRate) { SignalLoss = SignalLoss };
        }
    }

    public class RandomLossFilter : IFilter
    {
        private Random randGen = new Random();

        public float SignalLoss { get; set; } = 0.5f;
        public float errorLevel => AudioEffect.TFARSignalLossToErrorLevel(SignalLoss);

        public RandomLossFilter()
        {
        }

        public float Process(float input)
        {
            if (randGen.NextDouble() < errorLevel) return 0;
            return input;
        }

        public void PreProcess(float[] input, int offset, int sampleCount) { }
        public void FullPassProgress(float[] input, int sampleCount) { }
        public void FullPassFinished() { }
        public bool NeedFullPass => false;

        public IFilter Clone()
        {
            return new RandomLossFilter { SignalLoss = SignalLoss };

        }
    }

    public class VolumeFilter :  IFilter
    {
        public float Volume { get; set; } = 1;
        public VolumeFilter()
        {
        }

        public float Process(float input)
        {
            return input * Volume;
        }

        public void PreProcess(float[] input, int offset, int sampleCount) { }
        public void FullPassProgress(float[] input, int sampleCount) { }
        public void FullPassFinished() { }
        public bool NeedFullPass => false;

        public IFilter Clone()
        {
            return this;
        }
    }

    public class VolumeMonitor : IFilter, INotifyPropertyChanged
    {
        public float Volume { get; private set; } = 0;
        private float tempVolume = 0;
        public VolumeMonitor()
        {
        }

        public float Process(float input)
        {
            return input;
        }

        public void PreProcess(float[] input, int offset, int sampleCount) { }

        public void FullPassProgress(float[] input, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var abs = Math.Abs(input[i]);
                if (abs > tempVolume) tempVolume = abs;
            }
        }

        public void FullPassFinished()
        {
            Volume = tempVolume;
            tempVolume = 0;
        }
        public bool NeedFullPass => true;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IFilter Clone()
        {
            return this;
        }
    }

    public class VolumeSetFilter : IFilter, INotifyPropertyChanged
    {
        public float Volume { get; set; } = 1;
        private float actualVolume = 0;
        private float factor = 1;
        public VolumeSetFilter()
        {
        }

        public float Process(float input)
        {
            return input * factor;
        }

        public void PreProcess(float[] input, int offset, int sampleCount) { }

        public void FullPassProgress(float[] input, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var abs = Math.Abs(input[i]);
                if (abs > actualVolume) actualVolume = abs;
            }
        }

        public void FullPassFinished()
        {
            factor = Volume / actualVolume;
        }
        public bool NeedFullPass => true;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == nameof(Volume))
                factor = Volume / actualVolume;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IFilter Clone()
        {
            return this;
        }
    }

    public class HighpassFilter : CSCore.DSP.HighpassFilter, IFilter
    {
        public int MaxFrequency => base.SampleRate / 2;

        public HighpassFilter(int sampleRate, double frequency)
            : base(sampleRate, frequency)
        {
        }
        public void PreProcess(float[] input, int offset, int sampleCount) { }
        public void FullPassProgress(float[] input, int sampleCount) { }
        public void FullPassFinished() { }
        public bool NeedFullPass => false;

        public IFilter Clone()
        {
            return new HighpassFilter(SampleRate, Frequency);
        }
    }

    public class LowpassFilter : CSCore.DSP.LowpassFilter, IFilter
    {
        public int MaxFrequency => base.SampleRate / 2;
        public LowpassFilter(int sampleRate, double frequency)
            : base(sampleRate, frequency)
        {
        }

        public void PreProcess(float[] input, int offset, int sampleCount) {}
        public void FullPassProgress(float[] input, int sampleCount) {}
        public void FullPassFinished() {}
        public bool NeedFullPass => false;

        public IFilter Clone()
        {
            return new LowpassFilter(SampleRate, Frequency);
        }
    }






    public interface IFilter
    {
        void PreProcess(float[] input, int offset, int sampleCount);
        float Process(float input);

        void FullPassProgress(float[] input, int sampleCount);

        void FullPassFinished();

        bool NeedFullPass { get; }

        IFilter Clone();
    }



    public class AudioEffectLivePlay: IDisposable
    {
        private ISoundOut soundOutput = new WasapiOut();
        ISampleSource source;

        //https://github.com/filoe/cscore/tree/master/Samples/CSCoreWaveform
        protected void SetLiveSource(ISampleSource src)
        {
            source = src;
        }

        public void StartLivePlay()
        {
            soundOutput.Stop();

            soundOutput.Initialize(source.ToWaveSource());
            soundOutput.Play();
        }

        public void StopLivePlay()
        {
            soundOutput.Stop();
        }

        public void Dispose()
        {
            soundOutput.Dispose();
        }
    }

}
