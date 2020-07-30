using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterRandomLoss : UserControl
    {
        public RandomLossFilter FilterRef
        {
            get => (RandomLossFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(RandomLossFilter), typeof(FilterRandomLoss));

        public FilterRandomLoss()
        {
            InitializeComponent();
        }
    }
}