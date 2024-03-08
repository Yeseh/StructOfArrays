using System.Diagnostics;
using System.Runtime.InteropServices;

const int testSize = 2;
var random = new Random(12345);
var soa = new ExampleSOA(testSize);

Debug.Assert(soa.Length == 0);

int structSize = Marshal.SizeOf<Example>();
int structAlign = Alignment<Example>.Of();

Console.WriteLine($"Size: {structSize}, Align: {structAlign}");

var soa2 = new ExampleSOA([
    new Example(1, 1, true),
    new Example(2, 2, false),
]);

Debug.Assert(soa2.Length == testSize);
var zero = soa2.Get(0);
var one = soa2.Get(1);

Debug.Assert(soa2.MeasurementA.Length == 2);
Debug.Assert(soa2.MeasurementB.Length == 2);
Debug.Assert(soa2.IsSus.Length == 2);

Debug.Assert(zero.IsSus);
Debug.Assert(zero.MeasurementA == 1);
Debug.Assert(zero.MeasurementB == 1);

Debug.Assert(!one.IsSus);
Debug.Assert(one.MeasurementA == 2);
Debug.Assert(one.MeasurementB == 2);


internal struct Alignment<T> where T : unmanaged
{
    public byte Padding;
    public T Target;

    public static int Of() 
        => (int)Marshal.OffsetOf<Alignment<T>>(nameof(Alignment<T>.Target));
}

[StructLayout(LayoutKind.Sequential)]
// size: 24, align 8
public partial struct Example
{
    public double MeasurementA { get; set; }
    public double MeasurementB { get; set; }
    public bool IsSus { get; set; }

    public Example(double measurementA, double measurementB, bool isSus)
    {
        MeasurementA = measurementA; 
        MeasurementB = measurementB;
        IsSus = isSus;
    }
}

// TODO: There is some padding fuckery going on
//       Differences between interpreting bool as 1 byte or 4 bytes
[StructLayout(LayoutKind.Sequential)]
public partial struct ExampleSOA
{
    // Generated
    private static readonly int[] _sizes = [
        Marshal.SizeOf<double>(),
        Marshal.SizeOf<double>(),
        Marshal.SizeOf<bool>(),
    ];

    // Pre-calculated properties for memory layout 
    public static readonly int ItemAlignment = Alignment<Example>.Of();
    public static readonly int ItemSize = Marshal.SizeOf<Example>();
    public static readonly int ItemPadding = ItemSize - _sizes.Sum();

    // TODO: This needs to be re-calculated after resize
    private int[] _offsets = Array.Empty<int>();
    
    private byte[] _buffer = Array.Empty<byte>();

    private int _capacity = 0;

    private int _length = 0;

    public int Capacity {  get { return _capacity; } }

    public int Length {
        get => _length;
        private set => _length = value;
    } 

    public readonly ReadOnlySpan<double> MeasurementA
    {
        get
        {
            var targetSpan = _buffer.AsSpan()
                .Slice(_offsets[0], _length * _sizes[0]);
            var result = MemoryMarshal.Cast<byte, double>(targetSpan);

            return result;
        }
    }

    public ReadOnlySpan<double> MeasurementB
    {
        get
        {
            var targetSpan = _buffer.AsSpan()
                .Slice(_offsets[1], _length * _sizes[1]);
            var result = MemoryMarshal.Cast<byte, double>(targetSpan);
            
            return result;
        }
    }

    public ReadOnlySpan<bool> IsSus
    {
        get
        {
            var targetSpan = _buffer.AsSpan()
                .Slice(_offsets[2], _length * _sizes[2]);
            var result = MemoryMarshal.Cast<byte, bool>(targetSpan);

            return result;
        }
    }

    private void CalculateOffsets()
    {
        _offsets = new int[_sizes.Length];
        _offsets[0] = 0;
        int total = 0;
        for (int i = 1; i < _sizes.Length; i++)
        {
            var val = _sizes[i] * _capacity;
            if (i == _sizes.Length - 1)
            {
                total += ItemPadding * _capacity;
            }
            _offsets[i] = val + total;
            total += _offsets[i];
        }
    }

    public ExampleSOA(int capacity)
    {
        _capacity = capacity;
        _length = 0;
        var size = (ItemSize * _capacity) - (ItemPadding * _capacity);
        _buffer = new byte[size];

        CalculateOffsets();
    }

    public ExampleSOA(Example[] values)
    {
        _capacity = values.Length;
        _length = values.Length;
        var size = (ItemSize * _capacity) - (ItemPadding * _capacity);
        _buffer = new byte[size];

        CalculateOffsets();

        for (int i = 0;  i < _capacity; i++) 
        { 
            Set(i, values[i]); 
        }
    }

    public Example Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _length);

        Example res = new();
        var span = _buffer.AsSpan();

        res.MeasurementA = BitConverter.ToDouble(
            span.Slice(_offsets[0] + index * _sizes[0], _sizes[0])); // Convert first field

        res.MeasurementB = BitConverter.ToDouble(
            span.Slice(_offsets[1] + index * _sizes[1], _sizes[1])); // Convert second field

        res.IsSus = BitConverter.ToBoolean(
            span.Slice(_offsets[2] + index * _sizes[2], _sizes[2])); // Convert second field

        return res;
    }

    public void Set(int index, Example value) 
    { 
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _length);

        var bytes = BitConverter.GetBytes(value.MeasurementA);
        bytes.CopyTo(_buffer, _offsets[0] + index * _sizes[0]);

        bytes = BitConverter.GetBytes(value.MeasurementB);
        bytes.CopyTo(_buffer, _offsets[1] + index * _sizes[1]);

        // TODO: Bitconverter returns 1 byte for bool where Marshal returns 4
        bytes = BitConverter.GetBytes(value.IsSus);
        bytes.CopyTo(_buffer, _offsets[2] + index * _sizes[2]);
    }

    public Example[] ToExampleArray()
    {
        var result = new Example[_capacity];
        var span = _buffer.AsSpan();

        for (int i = 0; i < _capacity; i++)
        {
            result[i].MeasurementA = BitConverter.ToDouble(
                span.Slice(_offsets[0] + i * _sizes[0], _sizes[0]));
        }

        for (int i = 0; i < _capacity; i++)
        {
            result[i].MeasurementB = BitConverter.ToDouble(
                span.Slice(_offsets[1] + i * _sizes[1], _sizes[1]));
        }

        for (int i = 0; i < _capacity; i++)
        {
            result[i].IsSus = BitConverter.ToBoolean(
                span.Slice(_offsets[2] + i * _sizes[2], _sizes[2]));
        }

        return result;
    }
}

public partial class Program { }
