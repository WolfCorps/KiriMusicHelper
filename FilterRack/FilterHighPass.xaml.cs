using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterHighPass : UserControl
    {
        public HighpassFilter FilterRef
        {
            get => (HighpassFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(HighpassFilter), typeof(FilterHighPass));

        public FilterHighPass()
        {
            InitializeComponent();
        }
    }
}
