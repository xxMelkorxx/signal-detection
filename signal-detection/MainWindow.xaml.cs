using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using ScottPlot;
using ScottPlot.Control;

namespace signal_detection;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        RbIsAsk.IsChecked = true;

        // Настройка графиков.
        SetUpChart(ChartBitSequence, "Битовая последовательность", "Время, с", "Амплитуда");
        SetUpChart(ChartCarrierSignal, "Несущий сигнал", "Время, с", "Амплитуда");
        SetUpChart(ChartModulatedSignal, "Модулированный сигнал", "Время, с", "Амплитуда");
    }

    private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
    {
        
    }

    private void OnGenerateSignal(object sender, RoutedEventArgs e)
    {
        var nb = NudNb.Value ?? 100;
        var bps = NudBps.Value ?? 10;

        var a0 = NudA0.Value ?? 1;
        var f0 = NudF0.Value ?? 1000;
        var phi0 = NudPhi0.Value ?? 0;

        var length = NudLength.Value ?? 1000;
        var fd = NudFd.Value ?? 1;

        var modSgnlGen = new ModulatedSignalGenerator(new List<bool> { true, false, true }, 1, ModulationType.ASK, 1000d, 100d, 100000d, double.Pi / 2d);
        var resultSignal = modSgnlGen.GetModuletedSignal(1000, 100);
        ChartModulatedSignal.Plot.AddSignal(resultSignal.ToArray());
        ChartModulatedSignal.Refresh();
    }
    private void OnCheckedRbIsAsk(object sender, RoutedEventArgs e)
    {
        GbAskParams.IsEnabled = true;
        GbFskParams.IsEnabled = false;
        GbPskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsFsk(object sender, RoutedEventArgs e)
    {
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = true;
        GbPskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsPsk(object sender, RoutedEventArgs e)
    {
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = false;
        GbPskParams.IsEnabled = true;
    }

    private static void SetUpChart(IPlotControl chart, string title, string labelX, string labelY)
    {
        chart.Plot.Title(title);
        chart.Plot.XLabel(labelX);
        chart.Plot.YLabel(labelY);
        chart.Plot.XAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
        chart.Plot.YAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
        chart.Plot.XAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
        chart.Plot.YAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
        chart.Plot.Margins(x: 0.0, y: 0.6);
        chart.Plot.SetAxisLimits(xMin: 0, yMin: 0);
        chart.Refresh();
    }
}