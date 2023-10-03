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
    private Random _rnd = new(DateTime.Now.Millisecond);

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
    {
        RbIsAsk.IsChecked = true;

        // Настройка графиков.
        SetUpChart(ChartBitSequence, "Битовая последовательность", "Время, с", "Амплитуда");
        SetUpChart(ChartModulatedSignal, "Искомый сигнал", "Время, с", "Амплитуда");
        SetUpChart(ChartResultSignal, "Исследуемый сигнал", "Время, с", "Амплитуда");
        SetUpChart(ChartCrossCorrelation, "Взаимная корреляция сигналов", "Время, с", "Амплитуда");

        OnClickButtonGenerateBitsSequence(null, null);
        OnGenerateSignal(null, null);
    }

    private void OnGenerateSignal(object sender, EventArgs e)
    {
        var bps = NudBps.Value ?? 10;
        var a0 = NudA0.Value ?? 1;
        var f0 = NudF0.Value ?? 1000;
        var phi0 = NudPhi0.Value ?? 0;
        var length = NudLength.Value + 1 ?? 1001;
        var fd = NudFd.Value ?? 1;
        var startBit = NudStartBit.Value ?? 100;
        var countBits = NudCountBits.Value ?? 200;
        var args = new List<double>();
        switch (_modulationType)
        {
            case ModulationType.ASK:
                args.Add(NudA1.Value ?? 5);
                args.Add(NudA2.Value ?? 15);
                break;
            case ModulationType.FSK:
                // args.Add(NudF1.Value ?? 500);
                // args.Add(NudF2.Value ?? 1500);
                args.Add(NudDeltaF.Value ?? 50);
                break;
            case ModulationType.PSK:
                break;
        }

        // Получение битовой последовательности.
        var bitsSequenceList = TbBitsSequence.Text.Replace(" ", "").ToList();
        var bitsSequence = new List<bool>();
        bitsSequenceList.ForEach(b => bitsSequence.Add(b == '1'));

        // Формирование модулированного сигнала.
        _signalGenerator = new ModulatedSignalGenerator(bitsSequence, bps, _modulationType, fd, a0, f0, phi0);
        var resultSignal = _signalGenerator.GenerateSignals(countBits, startBit, args);
        var digitalSignal = _signalGenerator.digitalSignal;
        var modulatedSignal = _signalGenerator.modulatedSignal;
        
        // Наложение шума на сигналы.
        if (CbIsNoise.IsChecked ?? false)
        {
            ModulatedSignalGenerator.MakeNoise(ref resultSignal, NudSnr.Value ?? 5);
            ModulatedSignalGenerator.MakeNoise(ref modulatedSignal, NudSnr.Value ?? 5);
        }
        
        // Вычисление взаимной корреляции.
        var crossCorrelation = ModulatedSignalGenerator.GetCrossCorrelation(resultSignal, modulatedSignal, out var maxIndex);
        var shift = maxIndex * _signalGenerator.dt;
        
        TbInsertStartExpected.Text = ((double)startBit / bps).ToString("F3");
        TbInsertStartActual.Text = shift.ToString("F3");

        // Очистка графиков.
        ChartBitSequence.Plot.Clear();
        ChartModulatedSignal.Plot.Clear();
        ChartResultSignal.Plot.Clear();
        ChartCrossCorrelation.Plot.Clear();

        // Отрисовка графиков.
        ChartBitSequence.Plot.AddSignalXY(digitalSignal.Select(p => p.X).ToArray(), digitalSignal.Select(p => p.Y).ToArray());
        ChartBitSequence.Plot.SetAxisLimits(xMin: 0, xMax: digitalSignal.Max(p => p.X), yMin: -2, yMax: 2);
        ChartBitSequence.Refresh();

        ChartModulatedSignal.Plot.AddSignalXY(modulatedSignal.Select(p => p.X).ToArray(), modulatedSignal.Select(p => p.Y).ToArray());
        var yMax = modulatedSignal.Max(p => double.Abs(p.Y));
        ChartModulatedSignal.Plot.SetAxisLimits(xMin: 0, xMax: modulatedSignal.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
        ChartModulatedSignal.Refresh();

        ChartResultSignal.Plot.AddSignalXY(resultSignal.Select(p => p.X).ToArray(), resultSignal.Select(p => p.Y).ToArray());
        ChartResultSignal.Plot.SetAxisLimits(xMin: 0, xMax: resultSignal.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
        ChartResultSignal.Plot.AddVerticalLine(startBit * _signalGenerator.tb, Color.Green);
        ChartResultSignal.Plot.AddVerticalLine(shift, Color.Red);
        ChartResultSignal.Refresh();

        ChartCrossCorrelation.Plot.AddSignalXY(crossCorrelation.Select(p => p.X).ToArray(), crossCorrelation.Select(p => p.Y).ToArray());
        yMax = crossCorrelation.Max(p => double.Abs(p.Y));
        ChartCrossCorrelation.Plot.SetAxisLimits(xMin: 0, xMax: crossCorrelation.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
        ChartCrossCorrelation.Plot.AddVerticalLine(startBit * _signalGenerator.tb, Color.Green);
        ChartCrossCorrelation.Plot.AddVerticalLine(shift, Color.Red);
        ChartCrossCorrelation.Refresh();
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
        var bits = ModulatedSignalGenerator.GenerateBitsSequence(length);

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
        // GbPskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsFsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.FSK;
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = true;
        // GbPskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsPsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.PSK;
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = false;
        // GbPskParams.IsEnabled = true;
    }
    
    private void OnCheckedCheckBoxIsNoise(object sender, RoutedEventArgs e)
    {
        NudSnr.IsEnabled = CbIsNoise.IsChecked ?? false;
        OnGenerateSignal(null, null);
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
        chart.Configuration.Quality = QualityMode.High;
        chart.Configuration.DpiStretch = false;
        chart.Refresh();
    }
}