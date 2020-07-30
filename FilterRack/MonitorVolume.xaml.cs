using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class MonitorVolume : UserControl
    {
        public VolumeMonitor FilterRef
        {
            get => (VolumeMonitor)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(VolumeMonitor), typeof(MonitorVolume));

        public MonitorVolume()
        {
            InitializeComponent();
        }
    }
}