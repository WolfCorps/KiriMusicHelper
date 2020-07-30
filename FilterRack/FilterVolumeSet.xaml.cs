using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterVolumeSet : UserControl
    {
        public VolumeSetFilter FilterRef
        {
            get => (VolumeSetFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(VolumeSetFilter), typeof(FilterVolumeSet));

        public FilterVolumeSet()
        {
            InitializeComponent();
        }
    }
}