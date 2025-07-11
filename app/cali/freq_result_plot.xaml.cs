using NAudio.Wave;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
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
using System.Timers;
using Renci.SshNet;
using System.Diagnostics;
using System.Security.AccessControl;
using static sound_test.app.SlaveCom;

namespace sound_test.app.cali
{
    /// <summary>
    /// freq_result_plot.xaml 的交互逻辑
    /// </summary>
    public partial class freq_result_plot : Window
    {
        private WaveInEvent waveIn;
        private PlotModel plotModel;
        private LineSeries waveformSeries, waveformSeries2, waveformSeries3;
        List<Window_cali.db_cali> db_Cali;
        public freq_result_plot(List<Window_cali.db_cali> _db_Calis)
        {
            db_Cali = _db_Calis;
            InitializeComponent();
            InitializePlot();
        }

        private void InitializePlot()
        {
            plotModel = new PlotModel { Title = "Microphone Waveform" };
            waveformSeries = new LineSeries { Title = "Waveform" };
            waveformSeries2 = new LineSeries { Title = "Waveform" };
            waveformSeries3 = new LineSeries { Title = "Waveform" };
            plotModel.Series.Add(waveformSeries);
            plotModel.Series.Add(waveformSeries2);
            plotModel.Series.Add(waveformSeries3);
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 2000,
                Title = "Amplitude"
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum =3000,
                Title = "Time (s)"
            });

            WaveformPlot.Model = plotModel;
            waveformSeries.Points.Clear();
            foreach (var r in db_Cali)
            {
                waveformSeries.Points.Add(new DataPoint(r.freq, r.result/10));
                waveformSeries2.Points.Add(new DataPoint(r.freq, r.Loffset/10));
                waveformSeries3.Points.Add(new DataPoint(r.freq, r.Roffset / 10));
            }
        }

    }
}
