using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ScottPlot;
using ScottPlot.Control;

namespace signal_detection;

public partial class MainWindow
{
    private readonly BackgroundWorker _bgGenerateSignal, _bgResearch;
    private ModulatedSignalGenerator _signalGenerator;
    private ModulationType _modulationType;
    private Dictionary<string, object> _params1, _params2;
    private Dictionary<ModulationType, List<PointD>> _dependenceOnSnr;
    private int _maxIndex;

    public MainWindow()
    {
        InitializeComponent();

        _bgGenerateSignal = (BackgroundWorker)FindResource("BackgroundWorkerGenerateSignal");
        _bgResearch = (BackgroundWorker)FindResource("BackgroundWorkerConductResearch");
    }

    private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
    {
        RbIsAsk.IsChecked = true;

        // Настройка графиков.
        SetUpChart(ChartBitSequence, "Битовая последовательность", "Время, с", "Амплитуда");
        SetUpChart(ChartDesiredSignal, "Искомый сигнал", "Время, с", "Амплитуда");
        SetUpChart(ChartResearchedSignal, "Исследуемый сигнал", "Время, с", "Амплитуда");
        SetUpChart(ChartCrossCorrelation, "Взаимная корреляция сигналов", "Время, с", "Амплитуда");
        SetUpChart(ChartResearch, "Зависимость вероятности обнаружения сигнала от ОСШ", "Уровень шума, дБ", "Вероятность обнаружения");

        OnClickButtonGenerateBitsSequence(null, null);
        OnGenerateSignal(null, null);
    }

    #region ################# GENERATE SIGNALS #################

    private void OnGenerateSignal(object sender, EventArgs e)
    {
        if (_bgGenerateSignal.IsBusy)
            return;

        _params1 = new Dictionary<string, object>
        {
            ["bps"] = NudBps.Value ?? 10,
            ["a0"] = NudA0.Value ?? 1,
            ["f0"] = NudF0.Value ?? 1000,
            ["phi0"] = NudPhi0.Value ?? 0,
            ["fd"] = NudFd.Value ?? 1,
            ["startBit"] = NudStartBit.Value ?? 100,
            ["countBits"] = NudCountBits.Value ?? 200,
            ["modulationType"] = _modulationType,
            ["isNoise"] = CbIsNoise.IsChecked ?? false,
            ["SNR"] = NudSnr.Value ?? 5
        };
        switch (_modulationType)
        {
            case ModulationType.ASK:
                _params1["A1"] = NudA1.Value ?? 5;
                _params1["A2"] = NudA2.Value ?? 15;
                break;
            case ModulationType.FSK:
                _params1["dF"] = NudDeltaF.Value ?? 50;
                break;
            case ModulationType.PSK:
                break;
            default:
                throw new ArgumentException("Параметр не инициализирован");
        }

        // Получение битовой последовательности.
        var bitsSequence = new List<bool>();
        TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1'));
        _params1["bitsSequence"] = bitsSequence;

        ButtonGenerateSignal.IsEnabled = false;
        _bgGenerateSignal.RunWorkerAsync();
    }

    private void OnDoWorkBackgroundWorkerGenerateSignal(object sender, DoWorkEventArgs e)
    {
        try
        {
            // Формирование модулированного сигнала.
            _signalGenerator = new ModulatedSignalGenerator(_params1);
            _signalGenerator.GenerateSignals(_params1);

            // Наложение шума на сигналы.
            if ((bool)_params1["isNoise"])
                _signalGenerator.MakeNoise((double)_params1["SNR"]);

            // Вычисление взаимной корреляции.
            _signalGenerator.GetCrossCorrelation(out _maxIndex);
        }
        catch (Exception exception)
        {
            MessageBox.Show("Ошибка!", exception.Message);
        }
    }

    private void OnRunWorkerCompletedBackgroundWorkerGenerateSignal(object sender, RunWorkerCompletedEventArgs e)
    {
        var shift = _maxIndex * _signalGenerator.dt;
        TbInsertStartExpected.Text = (1d * (int)_params1["startBit"] / (int)_params1["bps"]).ToString("F3");
        TbInsertStartActual.Text = shift.ToString("F3");

        ChartDesiredSignal.Visibility = Visibility.Visible;
        ChartResearchedSignal.Visibility = Visibility.Visible;
        ChartCrossCorrelation.Visibility = Visibility.Visible;
        ChartResearch.Visibility = Visibility.Collapsed;

        // Очистка графиков.
        ChartBitSequence.Plot.Clear();
        ChartDesiredSignal.Plot.Clear();
        ChartResearchedSignal.Plot.Clear();
        ChartCrossCorrelation.Plot.Clear();

        // График битовой последовательности.
        ChartBitSequence.Plot.AddSignalXY(
            _signalGenerator.bitsSignal.Select(p => p.X).ToArray(),
            _signalGenerator.bitsSignal.Select(p => p.Y).ToArray()
        );
        ChartBitSequence.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.bitsSignal.Max(p => p.X), yMin: -2, yMax: 2);
        ChartBitSequence.Refresh();

        // График искомого сигнала.
        ChartDesiredSignal.Plot.AddSignalXY(
            _signalGenerator.desiredSignal.Select(p => p.X).ToArray(),
            _signalGenerator.desiredSignal.Select(p => p.Y).ToArray()
        );
        var yMax = _signalGenerator.desiredSignal.Max(p => double.Abs(p.Y));
        ChartDesiredSignal.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.desiredSignal.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
        ChartDesiredSignal.Refresh();

        // График исследуемого сигнала.
        ChartResearchedSignal.Plot.AddSignalXY(
            _signalGenerator.researchedSignal.Select(p => p.X).ToArray(),
            _signalGenerator.researchedSignal.Select(p => p.Y).ToArray()
        );
        ChartResearchedSignal.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.researchedSignal.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
        ChartResearchedSignal.Plot.AddVerticalLine((int)_params1["startBit"] * _signalGenerator.tb, Color.Green);
        ChartResearchedSignal.Plot.AddVerticalLine(((int)_params1["startBit"] + ((List<bool>)_params1["bitsSequence"]).Count) * _signalGenerator.tb, Color.Green);
        ChartResearchedSignal.Plot.AddVerticalLine(shift, Color.Red);
        ChartResearchedSignal.Refresh();

        // График корреляции искомого и исследуемого сигналов.
        ChartCrossCorrelation.Plot.AddSignalXY(
            _signalGenerator.crossCorrelation.Select(p => p.X).ToArray(),
            _signalGenerator.crossCorrelation.Select(p => p.Y).ToArray()
        );
        yMax = _signalGenerator.crossCorrelation.Max(p => double.Abs(p.Y));
        ChartCrossCorrelation.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.crossCorrelation.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
        ChartCrossCorrelation.Plot.AddVerticalLine((int)_params1["startBit"] * _signalGenerator.tb, Color.Green);
        ChartCrossCorrelation.Plot.AddVerticalLine(shift, Color.Red);
        ChartCrossCorrelation.Refresh();

        ButtonGenerateSignal.IsEnabled = true;
    }

    #endregion

    #region ################# CONDUCT RESEARCH #################

    private void OnClickButtonConductResearch(object sender, RoutedEventArgs e)
    {
        ButtonConductResearch.Visibility = Visibility.Collapsed;
        ProgressResearch.Visibility = Visibility.Visible;

        _dependenceOnSnr = new Dictionary<ModulationType, List<PointD>>
        {
            [ModulationType.ASK] = new(),
            [ModulationType.FSK] = new(),
            [ModulationType.PSK] = new(),
        };

        _params2 = new Dictionary<string, object>
        {
            ["bps"] = NudBps.Value ?? 10,
            ["a0"] = NudA0.Value ?? 1,
            ["f0"] = NudF0.Value ?? 1000,
            ["phi0"] = NudPhi0.Value ?? 0,
            ["fd"] = NudFd.Value ?? 1,
            ["startBit"] = NudStartBit.Value ?? 100,
            ["countBits"] = NudCountBits.Value ?? 200,
            ["modulationType"] = _modulationType,
            ["meanOrder"] = NudMeanOrder.Value ?? 50,
            ["snrFrom"] = NudSnrFrom.Value ?? -20,
            ["snrTo"] = NudSnrTo.Value ?? 10,
            ["snrStep"] = NudSnrStep.Value ?? 0.5,
            ["A1"] = NudA1.Value ?? 5,
            ["A2"] = NudA2.Value ?? 15,
            ["dF"] = NudDeltaF.Value ?? 50
        };

        // Получение битовой последовательности.
        var bitsSequence = new List<bool>();
        TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1'));
        _params2["bitsSequence"] = bitsSequence;

        ProgressResearch.Value = 0;
        ProgressResearch.Maximum = 3 * (int)_params2["meanOrder"] * (((int)_params2["snrTo"] - (int)_params2["snrFrom"]) / (double)_params2["snrStep"] + 1);

        _bgResearch.RunWorkerAsync();
    }

    private void OnDoWorkBackgroundWorkerConductResearch(object sender, DoWorkEventArgs e)
    {
        try
        {
            var meanOrder = (int)_params2["meanOrder"];
            var snrFrom = (int)_params2["snrFrom"];
            var snrTo = (int)_params2["snrTo"];
            var snrStep = (double)_params2["snrStep"];
            var startBit = (int)_params2["startBit"];
            var index = 0;

            Parallel.For(0, 3, type =>
            {
                Parallel.For(0, (int)((snrTo - snrFrom) / snrStep + 2), n =>
                {
                    var p = 0;
                    var snr = snrFrom + n * snrStep;
                    Parallel.For(0, meanOrder, i =>
                    {
                        // Формирование модулированного сигнала.
                        _params2["modulationType"] = (ModulationType)type;
                        var sg = new ModulatedSignalGenerator(_params2);
                        sg.GenerateSignals(_params2);
                        sg.MakeNoise(snr);
                        sg.GetCrossCorrelation(out _maxIndex);
                        if ((startBit - 1) * sg.tb <= _maxIndex * sg.dt && _maxIndex * sg.dt <= (startBit + 1) * sg.tb)
                            p++;

                        // Обновление ProgressBar.
                        _bgResearch.ReportProgress(++index);
                    });
                    _dependenceOnSnr[(ModulationType)type].Add(new PointD(snr, (double)p / meanOrder));
                });
                _dependenceOnSnr[(ModulationType)type] = _dependenceOnSnr[(ModulationType)type].OrderBy(p => p.X).ToList();
            });
        }
        catch (Exception exception)
        {
            MessageBox.Show("Ошибка!", exception.Message);
        }
    }

    private void OnRunWorkerCompletedBackgroundWorkerConductResearch(object sender, RunWorkerCompletedEventArgs e)
    {
        ChartDesiredSignal.Visibility = Visibility.Collapsed;
        ChartResearchedSignal.Visibility = Visibility.Collapsed;
        ChartCrossCorrelation.Visibility = Visibility.Collapsed;
        ChartResearch.Visibility = Visibility.Visible;

        // Отрисовка графика зависимости p от SNR. 
        ChartResearch.Plot.Clear();
        ChartResearch.Plot.AddSignalXY(
            _dependenceOnSnr[ModulationType.ASK].Select(p => p.X).ToArray(),
            _dependenceOnSnr[ModulationType.ASK].Select(p => p.Y).ToArray(),
            Color.Red,
            "ASK"
        );
        ChartResearch.Plot.AddSignalXY(
            _dependenceOnSnr[ModulationType.FSK].Select(p => p.X).ToArray(),
            _dependenceOnSnr[ModulationType.FSK].Select(p => p.Y).ToArray(),
            Color.Green,
            "FSK"
        );
        ChartResearch.Plot.AddSignalXY(
            _dependenceOnSnr[ModulationType.PSK].Select(p => p.X).ToArray(),
            _dependenceOnSnr[ModulationType.PSK].Select(p => p.Y).ToArray(),
            Color.Blue,
            "PSK"
        );
        ChartResearch.Plot.Legend();
        ChartResearch.Plot.SetAxisLimits(xMin: (int)_params2["snrFrom"], xMax: (int)_params2["snrTo"], yMin: 0, yMax: 1);
        ChartResearch.Refresh();

        ButtonConductResearch.Visibility = Visibility.Visible;
        ProgressResearch.Visibility = Visibility.Collapsed;
    }

    private void OnProgressChangedBackgroundWorkerConductResearch(object sender, ProgressChangedEventArgs e)
    {
        ProgressResearch.Value = e.ProgressPercentage;
    }

    #endregion

    #region ################# GENERATE BIT SEQUENCE #################

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

    #endregion

    #region ################# ONCHECKED #################

    private void OnCheckedRbIsAsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.ASK;
        GbAskParams.IsEnabled = true;
        GbFskParams.IsEnabled = false;
    }

    private void OnCheckedRbIsFsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.FSK;
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = true;
    }

    private void OnCheckedRbIsPsk(object sender, RoutedEventArgs e)
    {
        _modulationType = ModulationType.PSK;
        GbAskParams.IsEnabled = false;
        GbFskParams.IsEnabled = false;
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

    #endregion
}