using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterLowPass : UserControl
    {
        public LowpassFilter FilterRef
        {
            get => (LowpassFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(LowpassFilter), typeof(FilterLowPass));

        public FilterLowPass()
        {
            InitializeComponent();
        }
    }
}
