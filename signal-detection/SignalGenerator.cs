using System;
using System.Collections.Generic;
using System.Linq;

namespace signal_detection;

public enum ModulationType
{
    ASK,
    FSK,
    PSK
}

public class ModulatedSignalGenerator
{
    /// <summary>
    /// Битовая последовательность.
    /// </summary>
    private List<bool> bitsSequence { get; }

    /// <summary>
    /// Длина битовой последовательности.
    /// </summary>
    private int Nb => bitsSequence.Count;

    /// <summary>
    /// Битрейт.
    /// </summary>
    private int BPS { get; }

    /// <summary>
    /// Частота дискретизации.
    /// </summary>
    private double fd { get; }

    /// <summary>
    /// Тип модуляции сигнала.
    /// </summary>
    private ModulationType Type { get; }

    /// <summary>
    /// Амплитуда несущего сигнала.
    /// </summary>
    private double a0 { get; }

    /// <summary>
    /// Частота несущего сигнала.
    /// </summary>
    private double f0 { get; }

    /// <summary>
    /// Начальная фаза несущего сигнала.
    /// </summary>
    private double phi0 { get; }

    /// <summary>
    /// Временной отрезок одного бита.
    /// </summary>
    private double tb => 1d / BPS;

    /// <summary>
    /// Число отсчётов для полезного сигнала.
    /// </summary>
    private int length => (int)(tb * Nb / dt);

    /// <summary>
    /// Шаг по времени.
    /// </summary>
    public double dt => 1d / fd;

    /// <summary>
    /// Цифровой сигнал.
    /// </summary>
    public List<PointD> digitalSignal { get; }

    /// <summary>
    /// Модулированный сигнал.
    /// </summary>
    public List<PointD> modulatedSignal { get; }

    /// <summary>
    /// 
    /// </summary>
    private double Noise { get; }
    
    private Random _rnd = new(DateTime.Now.Millisecond);
    
    public ModulatedSignalGenerator(List<bool> bitsSequence, int bps, ModulationType type, double fd, double a0, double f0, double phi0)
    {
        this.bitsSequence = new List<bool>();
        bitsSequence.ForEach(bit => this.bitsSequence.Add(bit));
        this.Type = type;
        this.BPS = bps;
        this.fd = fd;
        this.a0 = a0;
        this.f0 = f0;
        this.phi0 = phi0;

        this.digitalSignal = new List<PointD>();
        this.modulatedSignal = new List<PointD>();
    }

    public List<PointD> GetModuletedSignal(int n, int insertStart, List<double> args, double noise = 0)
    {
        var countNumbers = insertStart + this.length < n ? n : insertStart + this.length;
        var resultSignal = new List<PointD>();
        var insertTime = dt * insertStart;
        
        // Генерации последовательности для длинного сигнала.
        var bits = string.Empty;
        for (var i = 0; i < 16; i++)
            bits += Convert.ToString(_rnd.Next(0, 255), 2).PadLeft(8, '0');
        var secondBitsSequence = new List<bool>();
        bits.ToList().ForEach(b => secondBitsSequence.Add(b == '1'));
        
        double yi;
        for (var i = 0; i < countNumbers; i++)
        {
            var ti = dt * i;
            // Вставка сигнала.
            if (i >= insertStart && i < insertStart + this.length - 1)
            {
                var bi1 = double.Sign(bitsSequence[(int)((ti - insertTime) / tb)] ? 1 : 0);
                digitalSignal.Add(new PointD(ti - insertTime, bi1));
                // var shift = 0d;
                // if (i != insertStart && (int)digitalSignal[i - insertStart - 1].Y != bi1)
                    // shift = double.Asin(modulatedSignal[i - insertStart - 1].Y / a0);
                yi = Type switch
                {
                    ModulationType.ASK => (bi1 == 0 ? args[0] : args[1]) * double.Sin(2 * double.Pi * f0 * ti + phi0),
                    ModulationType.FSK => a0 * double.Sin(2 * double.Pi * (f0 + (bi1 == 0 ? -1 : 1) * args[0]) * ti + phi0),
                    ModulationType.PSK => a0 * double.Sin(2 * double.Pi * f0 * ti + phi0 + (bi1 == 1 ? double.Pi : 0)),
                    _ => 0
                };
                modulatedSignal.Add(new PointD(ti - insertTime, yi));
                resultSignal.Add(new PointD(ti, yi));
            }
            // Формирование длинного сигнала.
            else
            {
                var bi2 = double.Sign(secondBitsSequence[(int)(ti / ((double)countNumbers / secondBitsSequence.Count * dt))] ? 1 : 0);
                yi = Type switch
                {
                    ModulationType.ASK => (bi2 == 0 ? a0 * 0.75 : a0 * 1.25) * double.Sin(2 * double.Pi * f0 * ti + phi0),
                    ModulationType.FSK => a0 * double.Sin(2 * double.Pi * (f0 + (bi2 == 0 ? -1 : 1) * 25) * ti + phi0),
                    ModulationType.PSK => a0 * double.Sin(2 * double.Pi * f0 * ti + double.Pi / 4 + (bi2 == 1 ? double.Pi : 0)),
                    _ => 0
                };
                resultSignal.Add(new PointD(ti, yi));
            }
        }

        return resultSignal;
    }

    /// <summary>
    /// Взаимная корреляция двух сигналов.
    /// </summary>
    /// <param name="s1"></param>
    /// <param name="s2"></param>
    /// <param name="maxIndex"></param>
    /// <returns></returns>
    public static List<PointD> GetCrossCorrelation(List<PointD> s1, List<PointD> s2, out int maxIndex)
    {
        var result = new List<PointD>();
        var maxCorr = double.MinValue;
        var index = 0;
        for (var i = 0; i < s1.Count - s2.Count + 1; i++)
        {
            var corr = 0d;
            for (var j = 0; j < s2.Count; j++)
                corr += s1[i + j].Y * s2[j].Y;
            result.Add(new PointD(s1[i].X, corr / s1.Count));

            if (result[i].Y > maxCorr)
            {
                maxCorr = result[i].Y;
                index = i;
            }
        }

        maxIndex = index;
        return result;
    }
}