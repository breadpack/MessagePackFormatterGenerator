internal struct InternalStruct
{
    public   int    a;
    internal int    b;
}
public struct IntStruct
{
    public   int              a;
    internal InternalStruct[] b;
}