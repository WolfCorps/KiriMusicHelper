using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterRingmod : UserControl
    {
        public RingmodulationFilter FilterRef
        {
            get => (RingmodulationFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(RingmodulationFilter), typeof(FilterRingmod));

        public FilterRingmod()
        {
            InitializeComponent();
        }
    }
}