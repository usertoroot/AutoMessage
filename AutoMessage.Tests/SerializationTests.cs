using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMessage.Tests
{
    public class SerializationTests
    {
        public class NestedComplexObjectChildA
        {
            public int A = 31;
            public bool B { get; set; } = true;
            public float C { get; set; } = 1.3123f;
            public double D = 6.4341;
            public int[] E = new int[] { 13, 31, 213, 12321, 321, 3, 1, 23, 1, 32 };
            public List<int> F = new List<int> { 321, 3, 1, 23, 1, 32 };
            public int[] G { get; set; } = new int[] { 3, 1, 23, 1, 32 };

            public override bool Equals(object obj)
            {
                if (obj is NestedComplexObjectChildA o)
                {
                    var a = ToString();
                    var oa = o.ToString();
                    return a == oa;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            public override string ToString()
            {
                return $"{{A:{A},B:{B},C:{C},D:{D},E:[{string.Join(',', E)}],F:[{string.Join(',', F)}],G:[{string.Join(',', G)}]}}";
            }
        }

        public class NestedComplexObjectChildB
        {
            public byte[] A = new byte[] { 0xfd, 0x24, 0x04, 0x64, 0xa2, 0x99, 0x94, 0xf3, 0x3f, 0xb5, 0xc0, 0xd3, 0x9a, 0x3a, 0xf7, 0xf1, 0xbc, 0x55,
                0x13, 0x87, 0x3c, 0x6c, 0x64, 0x1d, 0x7c, 0xd8, 0x25, 0x63, 0xd8, 0x7e, 0x9b, 0x6d, 0x90, 0x47, 0xc1, 0x5d, 0x95, 0x61,
                0xaf, 0x5c, 0x8b, 0x0f, 0x53, 0xd6, 0xc5, 0x5c, 0x08, 0xf6, 0xb1, 0xb4, 0xdb, 0xdc, 0x2a, 0x14, 0x63, 0xbc, 0x55, 0x25,
                0x53, 0x05, 0x89, 0x21, 0x7d, 0xa0 };

            public override bool Equals(object obj)
            {
                if (obj is NestedComplexObjectChildB o)
                {
                    var a = ToString();
                    var oa = o.ToString();
                    return a == oa;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            public override string ToString()
            {
                return $"{{A:[{string.Join("", A.Select(a => a.ToString("X2")))}]}}";
            }
        }

        public class NestedComplexObjectChildC
        {
            public int A = 31;
            public bool B { get; set; } = true;
            public float C { get; set; } = 1.3123f;
            public double D = 6.4341;
            public int[] E = new int[] { 13, 31, 213, 12321, 321, 3, 1, 23, 1, 32 };
            public List<int> F = new List<int> { 321, 3, 1, 23, 1, 32 };
            public int[] G { get; set; } = new int[] { 3, 1, 23, 1, 32 };
            public NestedComplexObjectChildB NestedComplexObjectB = new NestedComplexObjectChildB();

            public override bool Equals(object obj)
            {
                if (obj is NestedComplexObjectChildC o)
                {
                    var a = ToString();
                    var oa = o.ToString();
                    return a == oa;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            public override string ToString()
            {
                return $"{{A:{A},B:{B},C:{C},D:{D},E:[{string.Join(',', E)}],F:[{string.Join(',', F)}],G:[{string.Join(',', G)}],NestedComplexObjectB:{NestedComplexObjectB}}}";
            }
        }

        public class NestedComplexObject
        {
            public int A = 31;
            public bool B { get; set; } = true;
            public float C { get; set; } = 1.3123f;
            public double D = 6.4341;
            public int[] E = new int[] { 13, 31, 213, 12321, 321, 3, 1, 23, 1, 32 };
            public List<int> F = new List<int> { 321, 3, 1, 23, 1, 32 };
            public NestedComplexObjectChildA G { get; set; } = new NestedComplexObjectChildA();
            public List<NestedComplexObjectChildA> H { get; set; } = new List<NestedComplexObjectChildA> { new NestedComplexObjectChildA(), new NestedComplexObjectChildA() };
            public NestedComplexObjectChildA[] I = new NestedComplexObjectChildA[] { new NestedComplexObjectChildA(), new NestedComplexObjectChildA() };
            public NestedComplexObjectChildC J { get; set; } = new NestedComplexObjectChildC();

            public override bool Equals(object obj)
            {
                if (obj is NestedComplexObject o)
                {
                    var a = ToString();
                    var oa = o.ToString();
                    return a == oa;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            public override string ToString()
            {
                return $"{{A:{A},B:{B},C:{C},D:{D},E:[{string.Join(',', E)}],F:[{string.Join(',', F)}],G:{G}}},H:[{string.Join(',', H.Select(h => h.ToString()))}],I:[{string.Join(',', I.Select(i => i.ToString()))}],J:{J}";
            }
        }

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void SerializationIntegrationTests()
        {
            //Single value tests
            SerializeTest(2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest(true, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest(0.3123f, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest(2.0d, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((sbyte)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((byte)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((short)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((ushort)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((uint)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((long)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest((ulong)2, (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));
            SerializeTest("testing", (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));

            //Primitive array tests
            SerializeTest(new int[] { 2, 56, 1461, 1313213213 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new bool[] { true, false, false, true }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new float[] { 0.3123f, 0.51312f, 0.3131311f }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new double[] { 2.0d, 131.0d, 31231.213123123d }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new sbyte[] { 2, -123, 123, 12 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new byte[] { 23, 131, 141, 255 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new short[] { 1, 131, -1233, 1413 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new ushort[] { 123, 131, 1232, 12123 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new uint[] { 2, 312, 131, 12312 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new long[] { 2, 231, 123123, -131231, 5, 123 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new ulong[] { 2, 641, 23, 123151 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));

            //List test
            SerializeTest(new List<int> { 2, 56, 1461, 1313213213 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<bool> { true, false, false, true }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<float> { 0.3123f, 0.51312f, 0.3131311f }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<double> { 2.0d, 131.0d, 31231.213123123d }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<sbyte> { 2, -123, 123, 12 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<byte> { 23, 131, 141, 255 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<short> { 1, 131, -1233, 1413 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<ushort> { 123, 131, 1232, 12123 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<uint> { 2, 312, 131, 12312 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<long> { 2, 231, 123123, -131231, 5, 123 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new List<ulong> { 2, 641, 23, 123151 }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));

            //Complex object test
            SerializeTest(new NestedComplexObject(), (value, deserializedValue) => Assert.AreEqual(value, deserializedValue));

            //Complex object array test
            SerializeTest(new List<NestedComplexObject> { new NestedComplexObject(), new NestedComplexObject() }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));
            SerializeTest(new NestedComplexObject[] { new NestedComplexObject(), new NestedComplexObject() }, (value, deserializedValue) => CollectionAssert.AreEqual(value, deserializedValue));

            Assert.Pass();
        }

        private void SerializeTest<T>(T value, Action<T, T> equalAssertion)
        {
            var mockTypedValue = new MockType<T>(value);
            var expectedSerializedSize = MessageSerializer.GetSize(mockTypedValue);
            var serializedData = new byte[expectedSerializedSize];

            using (var stream = new System.IO.MemoryStream(serializedData))
            {
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    MessageSerializer.Serialize(writer, mockTypedValue);

                var positionAfterSerialization = stream.Position;
                Assert.AreEqual(positionAfterSerialization, expectedSerializedSize);

                stream.Position = 0;

                T deserializedValue;
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                    deserializedValue = MessageDeserializer.Deserialize<MockType<T>>(reader).Value;

                equalAssertion(value, deserializedValue);
            }
        }

        private class MockType<T>
        {
            public T Value { get; set; }

            public MockType(T value)
            {
                Value = value;
            }

            public MockType()
            {

            }
        }
    }
}