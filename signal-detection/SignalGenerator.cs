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
    /// 
    /// </summary>
    private List<bool> bitsSequence { get; }

    /// <summary>
    /// 
    /// </summary>
    private int Nb => bitsSequence.Count;

    /// <summary>
    /// 
    /// </summary>
    private int BPS { get; }

    /// <summary>
    /// 
    /// </summary>
    private double fd { get; }

    /// <summary>
    /// 
    /// </summary>
    private ModulationType Type { get; }

    /// <summary>
    /// 
    /// </summary>
    private double a0 { get; }

    /// <summary>
    /// 
    /// </summary>
    private double f0 { get; }

    /// <summary>
    /// 
    /// </summary>
    private double phi0 { get; }

    /// <summary>
    /// 
    /// </summary>
    private double tb => (double)Nb / BPS;

    /// <summary>
    /// 
    /// </summary>
    private int length => (int)(tb / dt) + 1;

    /// <summary>
    /// 
    /// </summary>
    public double dt => 1d / fd;

    /// <summary>
    /// 
    /// </summary>
    private List<double> digitalSignal { get; set; }

    /// <summary>
    /// 
    /// </summary>
    private List<double> carrierSignal { get; set; }

    /// <summary>
    /// 
    /// </summary>
    private List<double> modulatedSignal { get; set; }

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

        this.digitalSignal = new List<double>();
        this.carrierSignal = new List<double>();
        this.modulatedSignal = new List<double>();
    }

    public List<double> GetModuletedSignal(int n, double insertStart, double noise = 0)
    {
        var countNumbers = insertStart + length < n ? n : 2 * n - (insertStart - length);
        var resultSignal = new List<double>();
        switch (Type)
        {
            case ModulationType.ASK:
                for (var i = 0; i < countNumbers; i++)
                {
                    resultSignal.Add(a0 * Math.Sin(f0 * dt + phi0));
                    for (var j = insertStart; j < this.length; j++)
                    {
                        
                        // TO DO
                    }
                }

                break;
            case ModulationType.FSK:
                for (var i = 0; i < countNumbers; i++)
                {
                    resultSignal.Add(a0 * Math.Sin(f0 * dt + phi0));
                    for (var j = insertStart; j < this.length; j++)
                    {
                        // TO DO
                    }
                }
                
                break;
            case ModulationType.PSK:
                for (var i = 0; i < countNumbers; i++)
                {
                    resultSignal.Add(a0 * Math.Sin(f0 * dt + phi0));
                    for (var j = insertStart; j < this.length; j++)
                    {
                        // TO DO
                    }
                }
                
                break;
        }

        return resultSignal;
    }
}