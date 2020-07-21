using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KiriMusicHelper.Annotations;
using Ookii.Dialogs.Wpf;
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
        public string PboPrefix { get; set; } = "";
        public string MusicClass { get; set; } = "Action";
        public event PropertyChangedEventHandler PropertyChanged;
    }


    public class AudioFile
    {
        public FileInfo InputFile;
        public bool Enabled { get; set; } = true;
        public string FileName { get; set; }
        public long BitRate { get; set; }
        public int SamplingRate { get; set; }
        public double Duration { get; set; }

        public string GetCfgSoundsClass(AudioConversionParameters param)
        {
            return $"class {FileName} {{\n" +
                   $"    name=\"{FileName}\";\n" +
                   $"    sound[] = {{\"{param.PboPrefix}\\{FileName}.ogg\", 1.0, 1.0}};\n" +
                   $"    duration={Duration};\n" +
                   $"    musicClass=\"{param.MusicClass}\"\n" +
                   $"}};";

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

        }

        public ObservableCollection<AudioFile> AudioFiles { get; set; } = new ObservableCollection<AudioFile>();
        public AudioConversionParameters ConversionParameters { get; set; } = new AudioConversionParameters();

        private async void LoadFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog x =new VistaFolderBrowserDialog();
            
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
    }
}
