# MessagePack Formatter Generator

MessagePack의 Custom Serialization을 위한 AOT Source Generator 입니다.

## Installation

### Nuget Package Manager

```bash
dotnet add package dev.breadpack.messagepackformattergenerator
```

### Unity Package Manager

```
https://github.com/breadpack/MessagePackFormatterGenerator.git?path=UnityPackage
```

## Usage

[MessagePackObject] 속성이 사용된 타입과 해당 타입의 멤버에 사용된 타입들을 Recursive하게 검색하면서 CustomFormatter를 생성합니다.

Type이 Blittable인 경우를 인식하여 Binary array로 빠르게 Serialzation 처리하는 Formatter를 생성합니다.

``` csharp
namespace TestObjects {
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
}
```

위와 같은 struct에 대해서는 다음과 같은 Formatter를 생성합니다.

``` csharp
using System;
using System.Buffers;
using System.Reflection;
using MessagePack;
using MessagePack.Formatters;
using Tests.Objects;

namespace TestObjects {
    public class StructFormatter : IMessagePackFormatter<Tests.Objects.Struct> {
        public void Serialize(ref MessagePackWriter writer, Tests.Objects.Struct value, MessagePack.MessagePackSerializerOptions options) {
            var resolver = options.Resolver;

            writer.WriteArrayHeader(4);
            var DoubleValue = typeof(Tests.Objects.Struct).GetField("Double", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value);
            writer.Write((Double)DoubleValue);
            writer.Write(value.X);
            writer.Write(value.String);
            writer.Write(value.Float);
        }
        public Tests.Objects.Struct Deserialize(ref MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) {
            options.Security.DepthStep(ref reader);

            var count = reader.ReadArrayHeader();
            var result = new Tests.Objects.Struct();
            var boxedResult = (object)result;
            var DoubleValue = reader.ReadDouble();
            typeof(Tests.Objects.Struct).GetField("Double", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxedResult, DoubleValue);
            result = (Tests.Objects.Struct)boxedResult;
            result.X = reader.ReadInt32();
            result.String = reader.ReadString();
            result.Float = reader.ReadSingle();
            reader.Depth--;
            return result;
        }
    }
}
```

생성된 코드를 보면 [IgnoreMember] 속성은 제외 한 것을 볼 수 있습니다.
Private 멤버의 경우 Reflection을 통해 값을 읽고 쓰기를 진행합니다.
Reflection으로 값을 읽고 쓸 때 ValueType의 경우 boxing 이슈가 있으므로 private 멤버들을 우선적으로 처리하여 boxing, unboxing이 한번씩만 일어나도록 처리하고 있습니다.

``` csharp
    [MessagePackObject]
    public unsafe struct BlittableStruct {
        public int X;

        [JsonProperty]
        public int Y;

        public fixed char Name[10];
        public fixed long Long[20];
    }
```
Blittable type의 경우에는 아래와 같이 byte array로 한번에 serialize 합니다.

``` csharp
    public class BlittableStructFormatter : IMessagePackFormatter<Tests.Objects.BlittableStruct> {
        public void Serialize(ref MessagePackWriter writer, Tests.Objects.BlittableStruct value, MessagePack.MessagePackSerializerOptions options) {
            var resolver = options.Resolver;

            unsafe {
                writer.Write(new ReadOnlySpan<byte>((byte*)&value, sizeof(Tests.Objects.BlittableStruct)));
            }
        }
        public Tests.Objects.BlittableStruct Deserialize(ref MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) {
            options.Security.DepthStep(ref reader);

            var result = new Tests.Objects.BlittableStruct();
            unsafe {
                var bytes = reader.ReadBytes().Value;
                var span = new Span<byte>(&result, sizeof(Tests.Objects.BlittableStruct));
                bytes.CopyTo(span);
            }
            reader.Depth--;
            return result;
        }
    }
```

unsafe 키워드를 사용하여 byte array로 struct를 한번에 serialize, deserialize를 수행하여 좀 더 나은 성능으로 처리합니다.