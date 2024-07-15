using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using NUnit.Framework;
using Tests;
using Tests.Objects;
#if UNITY_ENGINE
using UnityEngine;
using UnityEngine.TestTools;
#endif

public class MessagePackSerializerTester
{
    // A Test behaves as an ordinary method
    [Test]
    public void MessagePackSerializerSimplePasses()
    {
        var s = new Struct() {
            X      = 1,
            Y      = 2,
            String = "Hello",
            Float  = 3.14f,
        };
        s.SetDouble(3.14159);

        var serialized   = MessagePackSerializer.Serialize(s, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        var deserialized = MessagePackSerializer.Deserialize<Struct>(serialized, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        
        Assert.That(deserialized.X, Is.EqualTo(s.X));
        Assert.That(deserialized.String, Is.EqualTo(s.String));
        Assert.That(deserialized.Float, Is.EqualTo(s.Float));
        Assert.That(deserialized.GetDouble(), Is.EqualTo(s.GetDouble()));
    }

    [Test]
    public unsafe void TestBlittableStruct() {
        var s = new BlittableStruct() {
            X = 1,
            Y = 2,
        };
        var name = new Span<char>(s.Name, 10);
        "Hello".AsSpan().CopyTo(name);
        for (var i = 0; i < 20; i++) {
            s.Long[i] = i;
        }
        
        var serialized = MessagePackSerializer.Serialize(s, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        var deserialized = MessagePackSerializer.Deserialize<BlittableStruct>(serialized, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        
        Assert.That(deserialized.X, Is.EqualTo(s.X));
        Assert.That(deserialized.Y, Is.EqualTo(s.Y));
        
        // compare fixed array
        for (var i = 0; i < 10; i++) {
            Assert.That(deserialized.Name[i], Is.EqualTo(s.Name[i]));
        }
        for (var i = 0; i < 20; i++) {
            Assert.That(deserialized.Long[i], Is.EqualTo(s.Long[i]));
        }
    }
    
    [Test]
    public void TestNullableStructMember() {
        var s = new NullableStructMember() {
            Struct = new Struct() {
                X = 1,
                Y = 2,
                String = "Hello",
                Float = 3.14f,
            },
            IntStruct = new IntStruct()
            {
                a = 3,
            }
        };
        s.Struct.Value.SetDouble(3.14159);

        var serialized = MessagePackSerializer.Serialize(s, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        var deserialized = MessagePackSerializer.Deserialize<NullableStructMember>(serialized, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Struct);
        Assert.That(deserialized.Struct.Value.X, Is.EqualTo(s.Struct.Value.X));
        Assert.That(deserialized.Struct.Value.Y, Is.Not.EqualTo(s.Struct.Value.Y));
        Assert.That(deserialized.Struct.Value.String, Is.EqualTo(s.Struct.Value.String));
        Assert.That(deserialized.Struct.Value.Float, Is.EqualTo(s.Struct.Value.Float));
        Assert.That(deserialized.Struct.Value.GetDouble(), Is.EqualTo(s.Struct.Value.GetDouble()));
        Assert.NotNull(deserialized.IntStruct);
        Assert.That(deserialized.IntStruct.Value.a, Is.EqualTo(s.IntStruct.Value.a));
    }
    
    [Test]
    public void TestNullableStructMemberNull() {
        var s = new NullableStructMember() {
            Struct = null
        };

        var serialized = MessagePackSerializer.Serialize(s, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        var deserialized = MessagePackSerializer.Deserialize<NullableStructMember>(serialized, MessagePackSerializerOptions.Standard.WithResolver(FormatterResolver.Instance));
        
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Struct);
    }
}
