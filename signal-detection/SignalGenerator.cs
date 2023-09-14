using System;
using System.Collections.Generic;

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
    private double tb => (double)Nb / BPS;

    /// <summary>
    /// Число отсчётов для полезного сигнала.
    /// </summary>
    private int length => (int)(tb * Nb / dt) + 1;

    /// <summary>
    /// Шаг по времени.
    /// </summary>
    public double dt => 1d / fd;

    /// <summary>
    /// Цифровой сигнал.
    /// </summary>
    public List<PointD> digitalSignal { get; }

    /// <summary>
    /// Несущий сигнал.
    /// </summary>
    public List<PointD> carrierSignal { get; set; }

    /// <summary>
    /// Модулированный сигнал.
    /// </summary>
    public List<PointD> modulatedSignal { get; set; }

    /// <summary>
    /// 
    /// </summary>
    private double Noise { get; }

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
        this.carrierSignal = new List<PointD>();
        this.modulatedSignal = new List<PointD>();
    }

    public List<PointD> GetModuletedSignal(int n, double insertStart, double noise = 0)
    {
        var countNumbers = insertStart + length < n ? n : insertStart + length;
        var resultSignal = new List<PointD>();
        
        for (var i = 0; i < countNumbers; i++)
        {
            var ti = dt * i;
            var yi = a0 * Math.Sin(f0 * dt * i + phi0);
            
            carrierSignal.Add(new PointD(ti, yi));
                    
            // Формирование цифрового сигнала.
            if (i >= insertStart && i < insertStart + this.length - 1)
            {
                var tj = dt * (i - insertStart);
                var yj = double.Sign(bitsSequence[(int)(tj / tb)] ? 1 : 0);
                digitalSignal.Add(new PointD(tj, yj));
                
                switch (Type)
                {
                    case ModulationType.ASK:
                        resultSignal.Add(new PointD(ti, yi));
                        break;
                    case ModulationType.FSK:
                        resultSignal.Add(new PointD(ti, yi));
                        break;
                    case ModulationType.PSK:
                        resultSignal.Add(new PointD(ti, yi));
                        break;
                }
            }
            else
                resultSignal.Add(new PointD(ti, yi));
        }
        return resultSignal;
    }
}