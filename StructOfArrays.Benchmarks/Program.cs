using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;


BenchmarkRunner.Run<Benchmarks>();

[SimpleJob(RuntimeMoniker.Net80)]
//[SimpleJob(RuntimeMoniker.NativeAot80)]
[MarkdownExporter, RPlotExporter]
[MemoryDiagnoser]
[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.TotalCycles, HardwareCounter.BranchMispredictions)]
public class Benchmarks
{
    public static int Count = 100_000;

    public static Random Random = new(12345);

    private Example MakeExample()
    {
        return new Example()
        {
            A = Random.NextDouble(),
            B = Random.NextDouble(),
            IsSus = Random.Next(0, 1) == 1,
        };
    }

    [Benchmark(Baseline = true)]
    public void Full_ListLinqBaseline() // How you would normally do ith
    {
        List<Example> list = new();
        for (int i = 0; i < Count; i++) 
        { 
            list.Add(MakeExample());
        }

        _ = list.Select(e => e.IsSus ? 1 : 0)
            .Sum();
    }

    [Benchmark]
    public void Full_ListWithCapacity()
    {
        List<Example> list = new(Count);
        for (int i = 0; i < Count; i++) 
        { 
            list.Add(MakeExample());
        }

        var susCount = 0;
        foreach (Example example in list)
        {
            if (example.IsSus) { susCount++; }
        }
    }

    [Benchmark]
    public void Full_ArrayWithCapacity()
    {
        Example[] list = new Example[Count];
        for (int i = 0; i < Count; i++) 
        { 
            list[i] = MakeExample();
        }

        var susCount = 0;
        foreach (Example example in list)
        {
            if (example.IsSus) { susCount++; }
        }
    }

    [Benchmark]
    public void GetItemsByIndex_ArrayWithCapacity()
    {
        Example[] list = new Example[Count];
        for (int i = 0; i < Count; i++)
        {
            list[i] = MakeExample();
        }

        for (int i = 0; i<Count ; i++)
        {
            _ = list[i];
        }
    }

    [Benchmark]
    public void Fill_ArrayWithCapacity()
    {
        Example[] list = new Example[Count];
        for (int i = 0; i < Count; i++)
        {
            list[i] = MakeExample();
        }
    }

    [Benchmark]
    public void Full_SOADirectIndexing()
    {
        ExampleSOA soa = new(Count);
        for (int i = 0; i < Count; i++) 
        {
            soa.Add(MakeExample());
        }

        var susCount = 0;
        for (int i = 0; i < Count; i++)
        {
            if (soa[i].IsSus) { susCount++; }
        }
    }

    [Benchmark]
    public void GetItemsByIndex_SOA()
    {
        ExampleSOA soa = new(Count);
        for (int i = 0; i < Count; i++) 
        {
            soa.Add(MakeExample());
        }

        for (int i = 0; i < Count; i++)
        {
            _ = soa[i];
        }
    }

    [Benchmark]
    public void Fill_SOA()
    {
        ExampleSOA soa = new(Count);
        for (int i = 0; i < Count; i++) 
        {
            soa.Add(MakeExample());
        }
    }

    [Benchmark]
    public void Full_SOAFieldEnumerator()
    {
        ExampleSOA soa = new(Count);
        for (int i = 0; i < Count; i++) 
        {
            soa.Add(MakeExample());
        }

        var susCount = 0;
        foreach (int example in soa.IsSus)
        {
            if (example == 1) { susCount++; }
        }
    }

    [Benchmark]
    public void Full_SOAItemsEnumerator()
    {
        ExampleSOA soa = new(Count);
        for (int i = 0; i < Count; i++) 
        {
            soa.Add(MakeExample());
        }

        var susCount = 0;
        foreach (Example example in soa)
        {
            if (example.IsSus) { susCount++; }
        }
    }
}
