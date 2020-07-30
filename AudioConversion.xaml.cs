using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CSCore.Codecs;
using KiriMusicHelper.Annotations;
using KiriMusicHelper.Waveform;
using Ookii.Dialogs.Wpf;
using Binding = System.Windows.Data.Binding;
using Path = System.IO.Path;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace KiriMusicHelper {

    //https://stackoverflow.com/questions/15309008/binding-converterparameter
    [MarkupExtensionReturnType(typeof(BindingExpression))]
    [ContentProperty(nameof(Binding))]
public class ConverterBindableParameter : MarkupExtension
{
    #region Public Properties

    public Binding Binding { get; set; }
    public BindingMode Mode { get; set; }
    public IValueConverter Converter { get; set; }
    public Binding ConverterParameter { get; set; }

    #endregion

    public ConverterBindableParameter()
    { }

    public ConverterBindableParameter(string path)
    {
        Binding = new Binding(path);
    }

    public ConverterBindableParameter(Binding binding)
    {
        Binding = binding;
    }

    #region Overridden Methods

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var multiBinding = new MultiBinding();
        Binding.Mode = Mode;
        multiBinding.Bindings.Add(Binding);
        if (ConverterParameter != null)
        {
            ConverterParameter.Mode = BindingMode.OneWay;
            multiBinding.Bindings.Add(ConverterParameter);
        }
        var adapter = new MultiValueConverterAdapter
        {
            Converter = Converter
        };
        multiBinding.Converter = adapter;
        return multiBinding.ProvideValue(serviceProvider);
    }

    #endregion

    [ContentProperty(nameof(Converter))]
    private class MultiValueConverterAdapter : IMultiValueConverter
    {
        public IValueConverter Converter { get; set; }

        private object lastParameter;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Converter == null) return values[0]; // Required for VS design-time
            if (values.Length > 1) lastParameter = values[1];
            return Converter.Convert(values[0], targetType, lastParameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (Converter == null) return new object[] { value }; // Required for VS design-time

            return new object[] { Converter.ConvertBack(value, targetTypes[0], lastParameter, culture) };
        }
    }
}


    public class AudioConversionParameters : INotifyPropertyChanged
    {
        public string PboPrefix { get; set; } = "KiriMusic_A2";
        public string MusicClass { get; set; } = "Action";
        public event PropertyChangedEventHandler PropertyChanged;
    }


    public class AudioFile: INotifyPropertyChanged
    {
        public FileInfo InputFile;
        public bool Enabled { get; set; } = true;
        public string FileName { get; set; }
        public long BitRate { get; set; }
        public int SamplingRate { get; set; }
        public double Duration { get; set; }

        public double ConversionProgress { get; set; }

        public string GetCfgSoundsClass(AudioConversionParameters param)
        {
            return $"class {FileName} {{\n" +
                   $"    name=\"{FileName}\";\n" +
                   $"    sound[] = {{\"{param.PboPrefix}\\{FileName}.ogg\", 1.0, 1.0}};\n" +
                   $"    duration={Duration};\n" +
                   $"    musicClass=\"{param.MusicClass}\"\n" +
                   $"}};";

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AudioFilesToCfgMusicConverter : IValueConverter
    {
        //public AudioConversionParameters param;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is IEnumerable<AudioFile> audioFiles)) return "";
            if (!(parameter is AudioConversionParameters param)) return "";

            StringBuilder output = new StringBuilder();

            foreach (var audioFile in audioFiles)
            {
                output.AppendLine(audioFile.GetCfgSoundsClass(param));
            }

            return output.ToString();

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public partial class AudioConversion : UserControl, INotifyPropertyChanged {
        public AudioConversion() {
            InitializeComponent();

            var upax = ConfigText.GetBindingExpression(TextBox.TextProperty);

            var _inputBinding = new Binding("Value")
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Path = new PropertyPath("AudioFiles"),
                Source = this,
                Converter = new AudioFilesToCfgMusicConverter(),
                ConverterParameter = ConversionParameters
            };
         
            //_inputBinding.Converter = new AudioFilesToCfgMusicConverter {param = ConversionParameters};
            ConfigText.SetBinding(TextBox.TextProperty, _inputBinding);

            ConversionParameters.PropertyChanged += (sender, args) =>
            {
                var upx = ConfigText.GetBindingExpression(TextBox.TextProperty);

                upx.UpdateTarget();
            };
            encoder.OnProgress += (sender, conversion, args) =>
            {
                var file = AudioFiles.FirstOrDefault((x => x.FileName == conversion.FileName));
                if (file == null) return;
                file.ConversionProgress = args.Percent;
            };
        }

        public ObservableCollection<AudioFile> AudioFiles { get; set; } = new ObservableCollection<AudioFile>();
        public AudioConversionParameters ConversionParameters { get; set; } = new AudioConversionParameters();

        public int TargetBitrate { get; set; } = 192;

        private AudioEncoder encoder = new AudioEncoder();

       

        private bool FilterRackOpen { get; set; } = false;
        private bool LivePreviewOpen { get; set; } = false;
        private bool IsEncoding { get; set; } = false;

        public bool CanOpenLivePreview => FilterRackOpen && !LivePreviewOpen && !IsEncoding;
        public bool CanOpenFilterRack => !FilterRackOpen;
        public bool CanEncode => !IsEncoding;

        private async void LoadFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog x = new VistaFolderBrowserDialog();
            
            x.Description = "burp";

            if (!x.ShowDialog().Value) return;
            AudioFiles.Clear();

            string [] fileEntries = Directory.GetFiles(x.SelectedPath);
            foreach (string fileName in fileEntries)
            {
                var fileInfo = new FileInfo(fileName);
                var audioInfo = await AudioEncoder.GetInfo(fileInfo.FullName);
                if (audioInfo == null) continue;

                var audioStream = audioInfo.AudioStreams.FirstOrDefault();

                if (audioStream == null) continue;


                AudioFiles.Add(new AudioFile
                {
                    InputFile = fileInfo,
                    FileName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.FullName),
                    SamplingRate = audioStream.SampleRate,
                    BitRate = audioStream.Bitrate,
                    Duration = audioStream.Duration.TotalSeconds
                });
            }

            OnPropertyChanged("AudioFiles");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Audiodatei auswählen", Multiselect = true};

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            AudioFiles.Clear();
            foreach (string fileName in openFileDialog.FileNames)
            {
                var fileInfo = new FileInfo(fileName);
                var audioInfo = await AudioEncoder.GetInfo(fileInfo.FullName);
                var audioStream = audioInfo?.AudioStreams?.FirstOrDefault();

                if (audioStream == null) continue;


                AudioFiles.Add(new AudioFile
                {
                    InputFile = fileInfo,
                    FileName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.FullName),
                    SamplingRate = audioStream.SampleRate,
                    BitRate = audioStream.Bitrate,
                    Duration = audioStream.Duration.TotalSeconds
                });
            }

            OnPropertyChanged("AudioFiles");
        }

        private async void OpenEffectsRack_Click(object sender, RoutedEventArgs e)
        {
            await AudioEffect.Instance.SetSource(AudioEffect.CreateSource(AudioFiles.FirstOrDefault()?.InputFile.FullName));
            var p = new FilterRack.FilterRack(AudioEffect.Instance);
            p.Show();
            //AudioEffect.Instance.StartLivePlay();
            FilterRackOpen = true;
        }

        private async void OpenLivePlay_Click(object sender, RoutedEventArgs e)
        {
            var p = new LivePlay();
            await p.Init();
            p.Show();
            LivePreviewOpen = true;
        }

        private async void RunConversion_Click(object sender, RoutedEventArgs e)
        {
        
            var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "out");
            Directory.CreateDirectory(outputDirectory);


            IsEncoding = true;

            int threadCount = Math.Min(4, AudioFiles.Count);
            Task[] workers = new Task[threadCount];
            var queue = new ConcurrentQueue<AudioConversionItem>();
            var effectQueue = new ConcurrentQueue<Tuple<AudioEffect, FileInfo>>();

            if (AudioEffect.Instance.Filters.Count != 0)
            {
                foreach (var audioFile in AudioFiles)
                {
                    var effect = await AudioEffect.Instance.CopyFor(audioFile.InputFile);

                    effectQueue.Enqueue(new Tuple<AudioEffect, FileInfo>(effect, audioFile.InputFile));
                }
            }
            else
            {
                foreach (var audioFile in AudioFiles)
                {
                    queue.Enqueue(new AudioConversionItem
                    {
                        Bitrate = TargetBitrate * 1000,
                        InputFile = audioFile.InputFile,
                        OutputPath = Path.Combine(outputDirectory, Path.ChangeExtension(audioFile.InputFile.Name, ".ogg")),
                        FileName = System.IO.Path.GetFileNameWithoutExtension(audioFile.InputFile.Name),
                        SampleRate = 44100
                    });
                }
            }

            for (int i = 0; i < threadCount; ++i)
            {
                Task task = new Task(() =>
                {
                    while (effectQueue.TryDequeue(out Tuple<AudioEffect, FileInfo> fileToConvert))
                    {
                        var tempFile = fileToConvert.Item1.ProcessIntoTempFile();

                        queue.Enqueue(new AudioConversionItem
                        {
                            Bitrate = TargetBitrate * 1000,
                            InputFile = new FileInfo(tempFile),
                            OutputPath = Path.Combine(outputDirectory, Path.ChangeExtension(fileToConvert.Item2.Name, ".ogg")),
                            FileName = System.IO.Path.GetFileNameWithoutExtension(fileToConvert.Item2.Name),
                            SampleRate = 44100
                        });

                    }

                    while (queue.TryDequeue(out AudioConversionItem fileToConvert))
                    {
                        var subTask = encoder.AudioConvert(fileToConvert, CancellationToken.None);
                        subTask.Wait();
                    }

                });
                workers[i] = task;
                task.Start();
            }

            Task.WhenAll(workers).ContinueWith((x) =>
            {
                IsEncoding = false;
            });
            
        }
    }
}
