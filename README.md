# AutoMessage

An ultra-fast expression-based binary serializer/deserializer using pre-emitted serializers/deserializers with support for transmitting types even if they do not exist on the other assembly.

# Examples

Below a few examples for simple usage.

## Structure definition (supports both fields and properties)

```C#
public class TestDependency
{
    public string TestSByte { get; set; }
    public long TestLong1;
    public long TestLong2;
}

public class Test
{
    public int[] TestIntArray { get; set; }
    public TestDependency[] TestDependency1;
    public List<TestDependency> TestDependency3;
    public TestDependency TestDependency2;
    public int TestInt;
    public string TestString;
    public byte[][] TestData { get; set; }
}
```

## Simplest serialization

```C#
var data = MessageSerializer.Serialize(hello);
```

## Preallocated arrays serialization

```C#
var size = MessageSerializer.GetSize(hello);
var d = new byte[size];

using (Stream stream = new MemoryStream(d))
using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
{
    MessageSerializer.Serialize(writer, hello);
}
```

## Simplest deserialization

```C#
var hello = MessageDeserializer.Deserialize<Hello>();
```
