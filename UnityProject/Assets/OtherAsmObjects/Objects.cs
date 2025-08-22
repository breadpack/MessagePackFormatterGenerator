internal struct InternalStruct {
    public   int a;
    internal int b;
}

public struct PublicStruct {
    public   float  f;
    private  double d;
    internal string s;

    public int i { get; set; }
}

public class ContainerClass {
    public class NestedClass {
        public int   a;
        public float b { get; set; }
    }

    public double container_class_a;
}

public struct IntStruct {
    public   int                        a;
    internal InternalStruct[]           b;
    public   PublicStruct[]             c;
    public   ContainerClass             d;
    public   ContainerClass.NestedClass e;
}