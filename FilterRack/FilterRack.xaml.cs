using System;
using System.Collections.Generic;
using System.Linq;
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

namespace KiriMusicHelper.FilterRack
{

    public class FilterTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            if (element != null && item != null && item is IFilter)
            {

                switch (item)
                {
                    case HighpassFilter _: return element.FindResource("HighpassFilterTemplate") as DataTemplate;
                    case LowpassFilter _: return element.FindResource("LowpassFilterTemplate") as DataTemplate;
                    case RingmodulationFilter _: return element.FindResource("RingmodulationFilterTemplate") as DataTemplate;
                    case FoldbackFilter _: return element.FindResource("FoldbackFilterTemplate") as DataTemplate;
                    case RandomLossFilter _: return element.FindResource("RandomLossFilterTemplate") as DataTemplate;
                    case VolumeFilter _: return element.FindResource("VolumeFilterTemplate") as DataTemplate;
                    case VolumeMonitor _: return element.FindResource("VolumeMonitorTemplate") as DataTemplate;
                    case VolumeSetFilter _: return element.FindResource("VolumeSetFilterTemplate") as DataTemplate;
                }

            }

            return null;
        }
    }


    public partial class FilterRack : Window
    {



        public AudioEffect effect { get; set; }



        public FilterRack(AudioEffect eff)
        {
            effect = eff;
            InitializeComponent();
            this.DataContext = this;
        }

        private void AddFilter_Click(object sender, RoutedEventArgs e)
        {
            switch (((ComboBoxItem)NewFilterSelection.SelectedValue).Content)
            {
                case "High-Pass":
                    effect.AddFilter(new HighpassFilter(effect.SampleRate, 100));
                    break;
                case "Low-Pass":
                    effect.AddFilter(new LowpassFilter(effect.SampleRate, effect.SampleRate/2));
                    break;
                case "Foldback":
                    effect.AddFilter(new FoldbackFilter());
                    break;
                case "Ringmodulation":
                    effect.AddFilter(new RingmodulationFilter(effect.SampleRate));
                    break;
                case "Random Loss":
                    effect.AddFilter(new RandomLossFilter());
                    break;
                case "Volume Factor":
                    effect.AddFilter(new VolumeFilter());
                    break;
                case "Volume Monitor":
                    effect.AddFilter(new VolumeMonitor());
                    break;
                case "Volume Set":
                    effect.AddFilter(new VolumeSetFilter());
                    break;
            }
        }

        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        { 
            lock (effect.Filters)
            {
                effect.Filters.Clear();
            }
            

            switch (((ComboBoxItem)TemplateSelection.SelectedValue).Content)
            {
                case "TFAR Shortrange":
                    effect.AddFilter(new HighpassFilter(effect.SampleRate, 900));
                    effect.AddFilter(new LowpassFilter(effect.SampleRate, 3000));
                    effect.AddFilter(new FoldbackFilter());
                    effect.AddFilter(new RingmodulationFilter(effect.SampleRate));
                    break;
                case "TFAR Longrange":
                    effect.AddFilter(new HighpassFilter(effect.SampleRate, 520));
                    effect.AddFilter(new LowpassFilter(effect.SampleRate, 1300));
                    effect.AddFilter(new FoldbackFilter());
                    effect.AddFilter(new RingmodulationFilter(effect.SampleRate));
                    break;
                case "TFAR Airborne":
                    effect.AddFilter(new HighpassFilter(effect.SampleRate, 1000));
                    effect.AddFilter(new LowpassFilter(effect.SampleRate, 4000));
                    effect.AddFilter(new FoldbackFilter());
                    effect.AddFilter(new RingmodulationFilter(effect.SampleRate));
                    break;
                case "TFAR Underwater":
                    effect.AddFilter(new HighpassFilter(effect.SampleRate, 400));
                    effect.AddFilter(new LowpassFilter(effect.SampleRate, 1000));
                    effect.AddFilter(new FoldbackFilter());
                    effect.AddFilter(new RingmodulationFilter(effect.SampleRate));
                    effect.AddFilter(new RandomLossFilter());
                    break;
            }
        }
    }
}
