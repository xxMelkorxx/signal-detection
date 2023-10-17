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
    /// Шаг по времени.
    /// </summary>
    public double dt => 1d / fd;

    /// <summary>
    /// Цифровой сигнал.
    /// </summary>
    public List<PointD> bitsSignal { get; }

    /// <summary>
    /// Искомый сигнал.
    /// </summary>
    public List<PointD> desiredSignal { get; private set; }

    /// <summary>
    /// Исследуемый сигнал.
    /// </summary>
    public List<PointD> researchedSignal { get; private set; }

    /// <summary>
    /// Взаимная корреляция искомого сигнала с исследуемым.
    /// </summary>
    public List<PointD> crossCorrelation { get; }

    public ModulatedSignalGenerator(IReadOnlyDictionary<string, object> sParams)
    {
        bitsSequence = (List<bool>)sParams["bitsSequence"];
        Type = (ModulationType)sParams["modulationType"];
        BPS = (int)sParams["bps"];
        fd = (double)sParams["fd"];
        a0 = (double)sParams["a0"];
        f0 = (double)sParams["f0"];
        phi0 = (double)sParams["phi0"];

        bitsSignal = new List<PointD>();
        desiredSignal = new List<PointD>();
        researchedSignal = new List<PointD>();
        crossCorrelation = new List<PointD>();
    }

    public void GenerateSignals(Dictionary<string, object> sParams)
    {
        var countNumbers = (int)((int)sParams["countBits"] * tb / dt);

        // Генерация длинного сигнала.
        var longBitsSequence = new List<bool>();
        GenerateBitsSequence((int)sParams["startBit"]).ToList().ForEach(b => longBitsSequence.Add(b == '1'));
        bitsSequence.ForEach(b => longBitsSequence.Add(b));
        GenerateBitsSequence((int)sParams["countBits"] - Nb - (int)sParams["startBit"]).ToList().ForEach(b => longBitsSequence.Add(b == '1'));

        for (var i = 0; i < countNumbers; i++)
        {
            var ti = dt * i;
            var bidx = (int)(ti / tb);
            var bi = double.Sign(longBitsSequence[bidx] ? 1 : 0);
            var yi = Type switch
            {
                ModulationType.ASK => (bi == 0 ? (double)sParams["A1"] : (double)sParams["A2"]) * double.Sin(2 * double.Pi * f0 * ti + phi0),
                ModulationType.FSK => a0 * double.Sin(2 * double.Pi * (f0 + (bi == 0 ? -1 : 1) * (double)sParams["dF"]) * ti + phi0),
                ModulationType.PSK => a0 * double.Sin(2 * double.Pi * f0 * ti + phi0 + (bi == 1 ? double.Pi : 0)),
                _ => 0
            };
            researchedSignal.Add(new PointD(ti, yi));

            // Вставка сигнала.
            if (bidx >= (int)sParams["startBit"] && bidx < (int)sParams["startBit"] + Nb)
            {
                bitsSignal.Add(new PointD(ti - (int)sParams["startBit"] * tb, bi));
                desiredSignal.Add(new PointD(ti - (int)sParams["startBit"] * tb, yi));
            }
        }
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
    /// Генерация отнормированного белого шума.
    /// </summary>
    /// <param name="countNumbers">Число отсчётов</param>
    /// <param name="energySignal">Энергия сигнала, на который накладывается шум</param>
    /// <param name="snrDb">Уровень шума в дБ</param>
    /// <returns></returns>
    private static IEnumerable<double> GenerateNoise(int countNumbers, double energySignal, double snrDb)
    {
        var noise = new List<double>();
        for (var i = 0; i < countNumbers; i++)
            noise.Add(GetNormalRandom(-1d, 1d));

        // Нормировка шума.
        var snr = double.Pow(10, -snrDb / 10);
        var norm = double.Sqrt(snr * energySignal / noise.Sum(y => y * y));

        return noise.Select(y => y * norm).ToList();
    }


    /// <summary>
    /// Наложить шум на сигнал.
    /// </summary>
    /// <param name="snrDb"></param>
    /// <returns></returns>
    public void MakeNoise(double snrDb)
    {
        // Наложение шума на искомый сигнал.
        desiredSignal = desiredSignal.Zip(
                GenerateNoise(researchedSignal.Count, researchedSignal.Sum(p => p.Y * p.Y), snrDb),
                (p, n) => new PointD(p.X, p.Y + n))
            .ToList();

        // Наложение шума на исследуемый сигнал.
        researchedSignal = researchedSignal.Zip(
                GenerateNoise(researchedSignal.Count, researchedSignal.Sum(p => p.Y * p.Y), snrDb),
                (p, n) => new PointD(p.X, p.Y + n))
            .ToList();
    }

    /// <summary>
    /// Взаимная корреляция искомого и исследуемого сигнала.
    /// </summary>
    /// <param name="maxIndex"></param>
    /// <returns></returns>
    public void GetCrossCorrelation(out int maxIndex)
    {
        var maxCorr = double.MinValue;
        var index = 0;
        for (var i = 0; i < researchedSignal.Count - desiredSignal.Count + 1; i++)
        {
            var corr = 0d;
            for (var j = 0; j < desiredSignal.Count; j++)
                corr += researchedSignal[i + j].Y * desiredSignal[j].Y;
            crossCorrelation.Add(new PointD(researchedSignal[i].X, corr / desiredSignal.Count));

            if (crossCorrelation[i].Y > maxCorr)
            {
                maxCorr = crossCorrelation[i].Y;
                index = i;
            }
        }

        maxIndex = index;
    }

    /// <summary>
    /// Генерация битовой последовательности.
    /// </summary>
    /// <param name="countBits"></param>
    /// <returns></returns>
    public static string GenerateBitsSequence(int countBits)
    {
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        var bits = string.Empty;
        for (var i = 8; i <= countBits - 8 - countBits % 8; i += 8)
            bits += Convert.ToString(rnd.Next(0, 255), 2).PadLeft(8, '0');
        bits += Convert.ToString(rnd.Next(0, (int)double.Pow(2, 8 + countBits % 8) - 1), 2).PadLeft(8 + countBits % 8, '0');

        return bits;
    }
}