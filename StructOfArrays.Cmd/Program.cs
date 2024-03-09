using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

const int testSize = 100_000;
var random = new Random(12345);
int structSize = Marshal.SizeOf<Example>();
int structAlign = Alignment<Example>.Of();
Console.WriteLine($"Is LE: {BitConverter.IsLittleEndian}");
Console.WriteLine($"Size: {structSize}, Align: {structAlign}");


var soa = new ExampleSOA(testSize);
Debug.Assert(soa.Length == 0);
Debug.Assert(soa.Capacity == testSize);

for (int i = 0; i < testSize; i++)
{
    soa.Add(MakeExample());
}

Debug.Assert(soa.A.Length == testSize);

Console.WriteLine();

Example MakeExample()
{
    return new Example()
    {
        A = random.NextDouble(),
        B = random.NextDouble(),
        IsSus = random.Next(0, 1) == 1,
    };
}

internal struct Alignment<T> where T : unmanaged
{
    public byte Padding;
    public T Target;

    public static int Of() 
        => (int)Marshal.OffsetOf<Alignment<T>>(nameof(Alignment<T>.Target));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
// size: 24, align 8
public partial struct Example
{
    public double A { get; set; }
    public double B { get; set; }
    public bool IsSus { get; set; }

    public Example(double a, double b, bool isSus)
    {
        A = a; 
        B = b;
        IsSus = isSus;
    }
}

// TODO: Figure out what the API is.
// Get the raw buffer and return every element to _capacity or just the filled items with _length?
public partial struct ExampleSOA : IEnumerable<Example>
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

    public readonly ReadOnlySpan<double> A
    {
        get
        {
            var targetSpan = _buffer.AsSpan().Slice(_offsets[0], _length * _sizes[0]);
            var result = MemoryMarshal.Cast<byte, double>(targetSpan);
            return result;
        }
    }

    public ReadOnlySpan<double> B
    {
        get
        {
            var targetSpan = _buffer.AsSpan().Slice(_offsets[1], _length * _sizes[1]);
            var result = MemoryMarshal.Cast<byte, double>(targetSpan);
            return result;
        }
    }

    public ReadOnlySpan<int> IsSus
    {
        get
        {
            var targetSpan = _buffer.AsSpan().Slice(_offsets[2], _length * _sizes[2]);
            var result = MemoryMarshal.Cast<byte, int>(targetSpan);
            return result;
        }
    }

    // NOTE: This needs to be redone on resize
    private void CalculateOffsets()
    {
        _offsets = new int[_sizes.Length];
        _offsets[0] = 0;
        int total = 0;
        for (int i = 1; i < _sizes.Length; i++)
        {
            var val = _sizes[i - 1] * _capacity;
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

    private Example Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _length);

        Example res = new();
        var span = _buffer.AsSpan();

        var aSpan = span.Slice(_offsets[0] + index * _sizes[0], _sizes[0]);
        var bSpan = span.Slice(_offsets[1] + index * _sizes[1], _sizes[1]);
        var isSusSpan = span.Slice(_offsets[2] + index * _sizes[2], _sizes[2]);

        if (BitConverter.IsLittleEndian) 
        {
            res.A = BinaryPrimitives.ReadDoubleLittleEndian(aSpan);
            res.B = BinaryPrimitives.ReadDoubleLittleEndian(bSpan);
            res.IsSus = BinaryPrimitives.ReadInt32LittleEndian(isSusSpan) == 1;
        }
        else 
        { 
            res.A = BinaryPrimitives.ReadDoubleBigEndian(aSpan);
            res.B = BinaryPrimitives.ReadDoubleBigEndian(bSpan);
            res.IsSus = BinaryPrimitives.ReadInt32BigEndian(isSusSpan) == 1;
        }

        return res;
    }

    public void Add(Example value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(_length, _capacity);
        Set(_length, value);
        _length++;
    }

    private void Set(int index, Example value) 
    { 
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _length);

        var span = _buffer.AsSpan();

        // Some math is off here, writing to wrong pl
        var aSpan = span.Slice(_offsets[0] + index * _sizes[0], _sizes[0]);
        var bSpan = span.Slice(_offsets[1] + index * _sizes[1], _sizes[1]);
        var isSusSpan = span.Slice(_offsets[2] + index * _sizes[2], _sizes[2]);

        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(aSpan, value.A);
            BinaryPrimitives.WriteDoubleLittleEndian(bSpan, value.B);
            BinaryPrimitives.WriteInt32LittleEndian(isSusSpan, value.IsSus ? 1 : 0);
        }
        else
        {
            BinaryPrimitives.WriteDoubleBigEndian(aSpan, value.A);
            BinaryPrimitives.WriteDoubleBigEndian(bSpan, value.B);
            BinaryPrimitives.WriteInt32BigEndian(isSusSpan, value.IsSus ? 1 : 0);
        }
    }

    public Example[] ToExampleArray()
    {
        var result = new Example[_capacity];
        var span = _buffer.AsSpan();

        var aSpan = span.Slice(_offsets[0], _capacity * _sizes[0]);
        var bSpan = span.Slice(_offsets[1], _capacity * _sizes[1]);
        var isSusSpan = span.Slice(_offsets[2], _capacity * _sizes[2]);

        if (BitConverter.IsLittleEndian) 
        { 
            for (int i = 0; i < _capacity; i++) { result[i].A = BinaryPrimitives.ReadDoubleLittleEndian(aSpan); }
            for (int i = 0; i < _capacity; i++) { result[i].B = BinaryPrimitives.ReadDoubleLittleEndian(bSpan); }
            for (int i = 0; i < _capacity; i++) { result[i].IsSus = BinaryPrimitives.ReadInt32LittleEndian(isSusSpan) == 1; }
        }
        else
        {
            for (int i = 0; i < _capacity; i++) { result[i].A = BinaryPrimitives.ReadDoubleBigEndian(aSpan); }
            for (int i = 0; i < _capacity; i++) { result[i].B = BinaryPrimitives.ReadDoubleBigEndian(bSpan); }
            for (int i = 0; i < _capacity; i++) { result[i].IsSus = BinaryPrimitives.ReadInt32BigEndian(isSusSpan) == 1; }
        }

        return result;
    }

    public IEnumerator<Example> GetEnumerator()
    {
        for (int i = 0; i < _length; i++) 
        {
            yield return Get(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Example this[int index]
    {
        get => Get(index);
        set => Set(index, value);
    }
}

public partial class Program { }
