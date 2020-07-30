using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterVolume : UserControl
    {
        public VolumeFilter FilterRef
        {
            get => (VolumeFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(VolumeFilter), typeof(FilterVolume));

        public FilterVolume()
        {
            InitializeComponent();
        }
    }
}