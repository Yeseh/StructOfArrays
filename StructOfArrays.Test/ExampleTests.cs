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
        Assert.Empty(soa.A);
        Assert.Empty(soa.B);
        Assert.Empty(soa.IsSus);

        Assert.Equal(2, soa2.Length);
        Assert.Equal(2, soa2.Capacity);
        Assert.Equal(2, soa.A.Count());
        Assert.Equal(2, soa.B.Count());
        Assert.Equal(2, soa.IsSus.Count());
    }

    [Fact]
    public void Get_Should_Return_A_Single_Item_With_The_Correct_Values()
    {
        var soa = new ExampleSOA([
            new Example(1, 1, true),
            new Example(2, 2, false),
        ]);

        Assert.Equal(2, soa.Length);
        Assert.Equal(2, soa.Capacity);

        var item = soa[0];
        Assert.Equal(1, (int)item.A);
        Assert.Equal(1, (int)item.B);
        Assert.True(item.IsSus);

        item = soa[1];
        Assert.Equal(2, (int)item.A);
        Assert.Equal(2, (int)item.B);
        Assert.False(item.IsSus);
    }

    [Fact]
    public void Set_ShouldOverwrite_AnEmptyValue()
    {
        var soa = new ExampleSOA(1);
        Assert.Equal(0, soa.Length);
        Assert.Equal(1, soa.Capacity);

        var example = new Example(1f, 1f, true);
        soa[0] = example;

        var val = soa[0];
        Assert.Equal(1, (int)val.A);
        Assert.Equal(1, (int)val.B);
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
        soa[1] = example;

        var val = soa[1];
        Assert.Equal(3, (int)val.A);
        Assert.Equal(3, (int)val.B);
        Assert.True(val.IsSus);

        var array = soa.ToExampleArray();
        Assert.Equal(2, array.Length);
    }
}