using System;
using System.IO;
using System.Text;

namespace AutoMessage
{
    public class BinaryWriter : System.IO.BinaryWriter
    {
        public BinaryWriter(Stream output) : base(output)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
        {
        }

        public void Write(ulong[] values) => WritePrimitiveArray(values);
        public void Write(uint[] values) => WritePrimitiveArray(values);
        public void Write(ushort[] values) => WritePrimitiveArray(values);
        public void Write(float[] values) => WritePrimitiveArray(values);
        public void Write(sbyte[] values) => WritePrimitiveArray(values);
        public void Write(long[] values) => WritePrimitiveArray(values);
        public void Write(int[] values) => WritePrimitiveArray(values);
        public void Write(short[] values) => WritePrimitiveArray(values);
        public void Write(bool[] values) => WritePrimitiveArray(values);
        public void Write(double[] values) => WritePrimitiveArray(values);

        private unsafe void WritePrimitiveArray<T>(T[] values) where T : unmanaged
        {
            fixed (T* valuePointer = values)
            {
                var byteSpan = new ReadOnlySpan<byte>(valuePointer, values.Length * sizeof(T));
                Write(byteSpan);
            }
        }
    }
}
