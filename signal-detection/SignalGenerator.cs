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
    public int Nb => bitsSequence.Count;

    /// <summary>
    /// Битрейт.
    /// </summary>
    public int BPS { get; }

    /// <summary>
    /// Частота дискретизации.
    /// </summary>
    public double fd { get; }

    /// <summary>
    /// Тип модуляции сигнала.
    /// </summary>
    private ModulationType Type { get; }

    /// <summary>
    /// Амплитуда несущего сигнала.
    /// </summary>
    public double a0 { get; }

    /// <summary>
    /// Частота несущего сигнала.
    /// </summary>
    public double f0 { get; }

    /// <summary>
    /// Начальная фаза несущего сигнала.
    /// </summary>
    public double phi0 { get; }

    /// <summary>
    /// Временной отрезок одного бита.
    /// </summary>
    public double tb => 1d / BPS;

    /// <summary>
    /// Число отсчётов для полезного сигнала.
    /// </summary>
    public int length => (int)(tb * Nb / dt);

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
    
    public List<PointD> GenerateSignals(int countBits, int startBit, List<double> args, double noise = 0)
    {
        var countNumbers = (int)(countBits * tb / dt);
        var resultSignal = new List<PointD>();

        // Генерация длинного сигнала.
        var longBitsSequence = new List<bool>();
        GenerateBitsSequence(startBit).ToList().ForEach(b => longBitsSequence.Add(b == '1'));
        bitsSequence.ForEach(b => longBitsSequence.Add(b));
        GenerateBitsSequence(countBits - Nb - startBit).ToList().ForEach(b => longBitsSequence.Add(b == '1'));

        for (var i = 0; i < countNumbers; i++)
        {
            var ti = dt * i;
            var bidx = (int)(ti / tb);
            var bi = double.Sign(longBitsSequence[bidx] ? 1 : 0);
            var yi = Type switch
            {
                ModulationType.ASK => (bi == 0 ? args[0] : args[1]) * double.Sin(2 * double.Pi * f0 * ti + phi0),
                ModulationType.FSK => a0 * double.Sin(2 * double.Pi * (f0 + (bi == 0 ? -1 : 1) * args[0]) * ti + phi0),
                ModulationType.PSK => a0 * double.Sin(2 * double.Pi * f0 * ti + phi0 + (bi == 1 ? double.Pi : 0)),
                _ => 0
            };
            resultSignal.Add(new PointD(ti, yi));

            // Вставка сигнала.
            if (bidx >= startBit && bidx < startBit + Nb)
            {
                digitalSignal.Add(new PointD(ti - startBit * tb, bi));
                modulatedSignal.Add(new PointD(ti - startBit * tb, yi));
            }
        }

        return resultSignal;
    }
    
    /// <summary>
    /// Генерация случайного числа с нормальным распределением.
    /// </summary>
    /// <param name="min">Минимальное число (левая граница)</param>
    /// <param name="max">Максимальное число (правая граница)</param>
    /// <param name="n">Количество случайных чисел, которые необходимо суммировать для достижения нормального распределения</param>
    /// <returns>Случайное нормально распределённое число</returns>
    private static double GetNormalRandom(double min, double max, int n = 12)
    {
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        var sum = 0d;
        for (var i = 0; i < n; i++)
            sum += rnd.NextDouble() * (max - min) + min;
        return sum / n;
    }

    /// <summary>
    /// Наложить шум на сигнал.
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="snrDb"></param>
    /// <returns></returns>
    public static void MakeNoise(ref List<PointD> signal, double snrDb)
    {
        // Генерация белового шума.
        var noise = new List<double>();
        for (var i = 0; i < signal.Count; i++)
            noise.Add(GetNormalRandom(-1d, 1d));
        
        // Нормировка шума.
        var snr = double.Pow(10, snrDb / 10);
        var norm = double.Sqrt(snr * signal.Sum(p => p.Y * p.Y) / noise.Sum(y => y * y));
        noise = noise.Select(y => y * norm).ToList();
        
        // Наложение шума.
        signal = signal.Zip(noise, (p, n) => new PointD(p.X, p.Y + n)).ToList();
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
    
    /// <summary>
    /// Генерация 
    /// </summary>
    /// <param name="countBits"></param>
    /// <returns></returns>
    public static string GenerateBitsSequence(int countBits)
    {
        var rnd = new Random(DateTime.Now.Millisecond);
        var bits = string.Empty;
        for (var i = 8; i <= countBits - 8 - countBits % 8; i += 8)
            bits += Convert.ToString(rnd.Next(0, 255), 2).PadLeft(8, '0');
        bits += Convert.ToString(rnd.Next(0, (int)double.Pow(2, 8 + countBits % 8) - 1), 2).PadLeft(8 + countBits % 8, '0');
        return bits;
    }
}