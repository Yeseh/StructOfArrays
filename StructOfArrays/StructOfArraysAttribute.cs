namespace StructOfArrays;

[AttributeUsage(AttributeTargets.Struct)]
public class StructOfArraysAttribute : Attribute
{
    /// <summary>
    /// Used to calculate string sizes. 
    /// Each string property will be treated as this size.
    /// </summary>
    public int StringLength { get; set; } = default; 
}

