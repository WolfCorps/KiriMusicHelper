using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CSCore;
using CSCore.SoundOut;
using CSCore.Streams;
using KiriMusicHelper.Annotations;

namespace KiriMusicHelper.Waveform
{
    /// <summary>
    /// Interaction logic for LivePlay.xaml
    /// </summary>
    public partial class LivePlay : Window, INotifyPropertyChanged
    {
        private ObservableCollection<WaveformDataModel> _channels = new ObservableCollection<WaveformDataModel>();
        private NotificationSource _notificationSource;
        private ISoundOut _soundOut = new WasapiOut();


        public LivePlay()
        {
            InitializeComponent();
        }

        public async Task Init()
        {
            var task = LoadWaveformsAsync(AudioEffect.Instance.source);
            _notificationSource = new NotificationSource(new WaveformData.InterruptDisposeChainSource(AudioEffect.Instance.source)) { Interval = 100 };
            _notificationSource.BlockRead += (o, args) => { UpdatePosition(); };
            _soundOut.Initialize(_notificationSource.ToWaveSource());
            await task;
            _soundOut.Play();
        }

        public ObservableCollection<WaveformDataModel> Channels
        {
            get { return _channels; }
            private set
            {
                if (Equals(value, _channels))
                    return;
                _channels = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task LoadWaveformsAsync(ISampleSource waveSource)
        {
            Channels = null;
            //read the specified waveSource into n arrays of samples where n is the number of channels of the waveSource
            var channelData = await WaveformData.GetData(waveSource);
            ////by setting the Channels property, the Waveform Control automatically renders the waveform
            Channels =
                new ObservableCollection<WaveformDataModel>(channelData.Select(x => new WaveformDataModel { Data = x }));
        }

        private void UpdatePosition()
        {
            Dispatcher.InvokeAsync(() =>
            {
                var x = (float)AudioEffect.Instance.source.Position / WaveformData.Length;
                foreach (var waveformData in Channels)
                {
                    waveformData.PositionInPerc = x;
                }
            });
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_soundOut != null)
                _soundOut.Dispose();
           if (_notificationSource != null)
               _notificationSource.Dispose();
        }

        private void Waveform_OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (_notificationSource != null)
            {
                var position = (long)(e.Percentage * WaveformData.Length);
                position -= position % _notificationSource.WaveFormat.BlockAlign;
                _notificationSource.Position = position;

                foreach (var waveformData in Channels)
                {
                    waveformData.PositionInPerc = e.Percentage;
                }
            }
        }


        private void Play_Click(object sender, RoutedEventArgs e)
        {
            _soundOut.Play();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _soundOut.Stop();
        }
    }


    public class WaveformDataModel : INotifyPropertyChanged
    {
        private float[] _data;
        private double _positionInPerc;

        public IList<float> Data
        {
            get { return _data; }
            set
            {
                if (Equals(value, _data))
                    return;
                _data = value.ToArray();
                OnPropertyChanged();
            }
        }

        public double PositionInPerc
        {
            get { return _positionInPerc; }
            set
            {
                if (value.Equals(_positionInPerc))
                    return;
                _positionInPerc = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
