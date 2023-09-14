using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using ScottPlot;
using ScottPlot.Control;

namespace signal_detection;

public partial class MainWindow : Window
{
    private ModulatedSignalGenerator _signalGenerator;
    private ModulationType _modulationType;
    private Random _rnd = new (DateTime.Now.Millisecond);

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
        OnClickButtonGenerateBitsSequence(null, null);
        OnGenerateSignal(null,  null);
    }

    private void OnGenerateSignal(object sender, EventArgs e)
    {
        var bps = NudBps.Value ?? 10;
        var a0 = NudA0.Value ?? 1;
        var f0 = NudF0.Value ?? 1000;
        var phi0 = NudPhi0.Value ?? 0;
        var length = NudLength.Value + 1 ?? 1001;
        var fd = NudFd.Value ?? 1;
        var deltaA = NudDeltaA.Value ?? 10;
        var deltaF = NudDeltaF.Value ?? 1000;
        var deltaPhi = NudDeltaPhi.Value ?? double.Pi / 2d;
        
        // Получение битовой последовательности.
        var bitsSequenceList = TbBitsSequence.Text.Replace(" ", "").ToList();
        var bitsSequence = new List<bool>();
        bitsSequenceList.ForEach(b => bitsSequence.Add(b == '1'));
        
        // Формирование модулированного сигнала.
        _signalGenerator = new ModulatedSignalGenerator(bitsSequence, bps, _modulationType, fd, a0, f0, phi0);
        var resultSignal = _signalGenerator.GetModuletedSignal(length, 100);
        var digitalSignal = _signalGenerator.digitalSignal;
        var carrierSignal = _signalGenerator.carrierSignal;
        
        // Очистка графиков.
        ChartBitSequence.Plot.Clear();
        ChartCarrierSignal.Plot.Clear();
        ChartModulatedSignal.Plot.Clear();
        
        // Отрисовка графиков.
        ChartBitSequence.Plot.AddSignalXY(digitalSignal.Select(p => p.X).ToArray(), digitalSignal.Select(p => p.Y).ToArray());
        ChartBitSequence.Plot.SetAxisLimits(xMin: 0, xMax: digitalSignal.Max(p => p.X), yMin: -2, yMax: 2);
        ChartBitSequence.Refresh();
        
        ChartCarrierSignal.Plot.AddSignalXY(carrierSignal.Select(p => p.X).ToArray(), carrierSignal.Select(p => p.Y).ToArray());
        ChartCarrierSignal.Plot.SetAxisLimits(xMin: 0, xMax: carrierSignal.Max(p => p.X), yMin: -a0 * 1.2, yMax: a0 * 1.2);
        ChartCarrierSignal.Refresh();
        
        ChartModulatedSignal.Plot.AddSignalXY(resultSignal.Select(p => p.X).ToArray(), resultSignal.Select(p => p.Y).ToArray());
        ChartModulatedSignal.Plot.SetAxisLimits(xMin: 0, xMax: resultSignal.Max(p => p.X), yMin: -a0 * 1.2, yMax: a0 * 1.25);
        ChartModulatedSignal.Refresh();
    }
    
    private void OnClickButtonAddZero(object sender, RoutedEventArgs e)
    {
        TbBitsSequence.Text += TbBitsSequence.Text.Length % 5 == 0 ? " " : "";
        TbBitsSequence.Text += '0';
        ButtonGenerateSignal.IsEnabled = true;
        
    }

    private void OnClickButtonAddOne(object sender, RoutedEventArgs e)
    {
        TbBitsSequence.Text += TbBitsSequence.Text.Length % 5 == 0 ? " " : "";
        TbBitsSequence.Text += '1';
        ButtonGenerateSignal.IsEnabled = true;
    }

    private void OnClickButtonClearBits(object sender, RoutedEventArgs e)
    {
        TbBitsSequence.Clear();
        ButtonGenerateSignal.IsEnabled = false;
    }

    private void OnClickButtonGenerateBitsSequence(object sender, RoutedEventArgs e)
    {
        var length = NudNb.Value ?? 16;
        var bits = Convert.ToString(_rnd.Next(0, (int)double.Pow(2, length) - 1), 2).PadRight(length, '0'); 
        TbBitsSequence.Clear();
        for (var i = 0; i < bits.Length; i++)
        {
            TbBitsSequence.Text += i % 4 == 0 ? " " : "";
            TbBitsSequence.Text += bits[i];
        }

        ButtonGenerateSignal.IsEnabled = true;
        OnGenerateSignal(null, null);
    }

    private void OnCheckedRbIsAsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.ASK;
        GbAskParams.IsEnabled = true;
        GbFskParams.IsEnabled = false;
        GbPskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsFsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.FSK;
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = true;
        GbPskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsPsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.PSK;
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
        chart.Plot.Margins(x: 0.0, y: 0.8);
        chart.Plot.SetAxisLimits(xMin: 0);
        chart.Refresh();
    }
}