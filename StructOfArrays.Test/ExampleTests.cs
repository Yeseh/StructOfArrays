namespace StructOfArrays.Test;

public class ExampleTests
{
    [Fact]
    public void Constructors_Should_Create_SOA_With_Correct_Length_And_Capacity()
    {
        var soa = new ExampleSOA(2);
        var soa2 = new ExampleSOA([
            new Example(1, 1, true),
            new Example(2, 2, false),
        ]);

        Assert.Equal(0, soa.Length);
        Assert.Equal(2, soa.Capacity);
        Assert.Equal(2, soa2.Length);
        Assert.Equal(2, soa2.Capacity);
    }

    [Fact]
    public void Set_ShouldOverwrite_AnEmptyValue()
    {
        var soa = new ExampleSOA(1);
        Assert.Equal(0, soa.Length);
        Assert.Equal(1, soa.Capacity);

        var example = new Example(1f, 1f, true);
        soa.Set(0, example);

        var val = soa.Get(0);
        Assert.Equal(1, (int)val.MeasurementA);
        Assert.Equal(1, (int)val.MeasurementB);
        Assert.True(val.IsSus);
    }

    [Fact]
    public void Set_ShouldOverWrite_AnExistingValue()
    {
        var soa = new ExampleSOA([
            new Example(1, 1, true),
            new Example(2, 2, false),
        ]);
        Assert.Equal(2, soa.Length);
        Assert.Equal(2, soa.Capacity);

        var example = new Example(3, 3, true);
        soa.Set(1, example);

        var val = soa.Get(1);
        Assert.Equal(3, (int)val.MeasurementA);
        Assert.Equal(3, (int)val.MeasurementB);
        Assert.True(val.IsSus);
    }
}