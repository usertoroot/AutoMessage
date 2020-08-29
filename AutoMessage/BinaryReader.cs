using System;
using System.IO;
using System.Text;

namespace AutoMessage
{
    public unsafe class BinaryReader : System.IO.BinaryReader
    {
        public BinaryReader(Stream input) : base(input)
        {
        }

        public BinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {

        }

        public bool[] ReadBooleans(int count) => ReadPrimitiveArray<bool>(count);
        public double[] ReadDoubles(int count) => ReadPrimitiveArray<double>(count);
        public short[] ReadInt16s(int count) => ReadPrimitiveArray<short>(count);
        public int[] ReadInt32s(int count) => ReadPrimitiveArray<int>(count);
        public long[] ReadInt64s(int count) => ReadPrimitiveArray<long>(count);
        public sbyte[] ReadSBytes(int count) => ReadPrimitiveArray<sbyte>(count);
        public float[] ReadSingles(int count) => ReadPrimitiveArray<float>(count);
        public ushort[] ReadUInt16s(int count) => ReadPrimitiveArray<ushort>(count);
        public uint[] ReadUInt32s(int count) => ReadPrimitiveArray<uint>(count);
        public ulong[] ReadUInt64s(int count) => ReadPrimitiveArray<ulong>(count);

        private unsafe T[] ReadPrimitiveArray<T>(int count) where T : unmanaged
        {
            var values = new T[count];

            fixed (T* valuePointer = values)
            {
                var byteSpan = new Span<byte>(valuePointer, values.Length * sizeof(T));
                Read(byteSpan);
            }

            return values;
        }
    }
}
