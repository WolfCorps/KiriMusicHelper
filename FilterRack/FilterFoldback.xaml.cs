using System.Windows;
using System.Windows.Controls;

namespace KiriMusicHelper.FilterRack
{
    public partial class FilterFoldback : UserControl
    {
        public FoldbackFilter FilterRef
        {
            get => (FoldbackFilter)GetValue(FilterRefProperty);
            set => SetValue(FilterRefProperty, value);
        }

        public static readonly DependencyProperty FilterRefProperty = DependencyProperty.Register(nameof(FilterRef), typeof(FoldbackFilter), typeof(FilterFoldback));

        public FilterFoldback()
        {
            InitializeComponent();
        }
    }
}