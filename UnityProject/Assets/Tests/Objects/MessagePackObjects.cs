using MessagePack;
#if UNITY_2022_3_OR_NEWER
using Unity.Plastic.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif

namespace Tests.Objects {
    [MessagePackObject]
    public abstract class AbstractParent { }

    public abstract class AbstractParentWithoutAttribute { }

    [MessagePackObject]
    public struct Struct {
        public int X { get; set; }

        [IgnoreMember]
        public int Y { get; set; }

        public string String;
        public float  Float;

        [JsonProperty]
        private double Double;

        public void   SetDouble(double value) => Double = value;
        public double GetDouble()             => Double;
    }

    public class NullableStructMember : AbstractParent {
        public Struct? Struct;
    }

    [MessagePackObject]
    public unsafe struct BlittableStruct {
        public int X;

        [JsonProperty]
        public int Y;

        public fixed char Name[10];
        public fixed long Long[20];
    }

    public struct BlittableNestedStruct {
        public BlittableStruct[] Nested;
    }
}