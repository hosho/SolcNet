﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SolCodeGen.AbiEncoding.Encoders
{

    public abstract class NumberEncoder<TInt> : AbiTypeEncoder<TInt> where TInt : struct
    {
        protected static Dictionary<string, (int ByteSize, BigInteger MaxValue)> _unsignedTypeSizes
            = new Dictionary<string, (int ByteSize, BigInteger MaxValue)>(32);

        protected static Dictionary<string, (int ByteSize, BigInteger MaxValue, BigInteger MinValue)> _signedTypeSizes
            = new Dictionary<string, (int ByteSize, BigInteger MaxValue, BigInteger MinValue)>(32);

        static NumberEncoder()
        {
            for (var i = 1; i <= 32; i++)
            {
                var bitSize = i * 8;
                var maxIntValue = BigInteger.Pow(2, bitSize);
                _unsignedTypeSizes.Add("uint" + bitSize, (i, maxIntValue));
                _signedTypeSizes.Add("int" + bitSize, (i, maxIntValue / 2 + 1, -maxIntValue / 2));
            }
        }

        protected abstract bool Signed { get; }
        protected abstract BigInteger AsBigInteger { get; }

        public BigInteger MaxValue => Signed ? _signedTypeSizes[_info.SolidityName].MaxValue : _unsignedTypeSizes[_info.SolidityName].MaxValue;
        public BigInteger MinValue => Signed ? _signedTypeSizes[_info.SolidityName].MinValue : 0;

        public override void SetValue(in TInt val)
        {
            base.SetValue(val);
            var bigInt = AsBigInteger;
            if (bigInt > MaxValue)
            {
                throw IntOverflow();
            }
            if (Signed && bigInt < MinValue)
            {
                throw IntUnderflow();
            }
        }

        public override Span<byte> Encode(Span<byte> buffer)
        {
            var byteSize = _info.BaseTypeByteSize;
            Span<byte> valBytes = stackalloc byte[Unsafe.SizeOf<TInt>()];
            MemoryMarshal.Write(valBytes, ref _val);

            //Span<byte> valBytes = MemoryMarshal.Cast<TInt, byte>(new[] { _val });
            //Unsafe.WriteUnaligned(ref valBytes[0], _val);

            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < byteSize; i++)
                {
                    buffer[31 - i] = valBytes[i];
                }
            }
            else
            {
                for (var i = 0; i < byteSize; i++)
                {
                    buffer[31 - byteSize + i] = valBytes[i];
                }
            }
            return buffer.Slice(32);
        }

        protected Exception IntOverflow()
        {
            return new OverflowException($"Max value for type '{_info}' is {MaxValue}, was given {_val}");
        }

        protected Exception IntUnderflow()
        {
            return new OverflowException($"Min value for type '{_info}' is {MinValue}, was given {_val}");
        }

        public override ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> buffer, out TInt val)
        {
            Span<TInt> num = new TInt[1];
            Span<byte> byteView = MemoryMarshal.Cast<TInt, byte>(num);

            var byteSize = _info.BaseTypeByteSize;
            var padSize = 32 - byteSize;
            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < byteSize; i++)
                {
                    byteView[byteSize - i - 1] = buffer[i + padSize];
                }
            }
            else
            {
                for (var i = 0; i < byteSize; i++)
                {
                    byteView[i] = buffer[i + padSize];
                }
            }

            // data validity check: should be padded with zero-bytes
            // Disabled - ganache liters this padding with garbage bytes
            /*
            for (var i = 0; i < padSize; i++)
            {
                if (buffer[i] != 0)
                {
                    throw new ArgumentException($"Invalid {_info.SolidityName} input data; should be {byteSize} bytes, left-padded with {32 - byteSize} zero-bytes; received: " + buffer.Slice(0, 32).ToHexString());
                }
            }
            */

            val = num[0];

            return buffer.Slice(32);
        }
    }


    public class Int8Encoder : AbiTypeEncoder<sbyte>
    {
        static readonly byte[] ZEROx31 = Enumerable.Repeat((byte)0, 31).ToArray();

        public override Span<byte> Encode(Span<byte> buffer)
        {
            buffer[31] = unchecked((byte)_val);
            return buffer.Slice(32);
        }

        public override ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> buffer, out sbyte val)
        {
            if (!buffer.Slice(0, 31).SequenceEqual(ZEROx31))
            {
                throw new ArgumentException("Invalid int8 input data; should be 31 zeros followed by a int8/byte; received: " + buffer.Slice(0, 32).ToHexString());
            }
            val = unchecked((sbyte)buffer[31]);
            return buffer.Slice(32);
        }
    }

    public class UInt8Encoder : AbiTypeEncoder<byte>
    {
        static readonly byte[] ZEROx31 = Enumerable.Repeat((byte)0, 31).ToArray();

        public override Span<byte> Encode(Span<byte> buffer)
        {
            buffer[31] = _val;
            return buffer.Slice(32);
        }

        public override ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> buffer, out byte val)
        {
            if (!buffer.Slice(0, 31).SequenceEqual(ZEROx31))
            {
                throw new ArgumentException("Invalid uint8 input data; should be 31 zeros followed by a uint8/byte; received: " + buffer.Slice(0, 32).ToHexString());
            }
            val = buffer[31];
            return buffer.Slice(32);
        }
    }

    public class Int16Encoder : NumberEncoder<short>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
    }

    public class UInt16Encoder : NumberEncoder<ushort>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;
    }

    public class Int32Encoder : NumberEncoder<int>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
    }

    public class UInt32Encoder : NumberEncoder<uint>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;
    }

    public class Int64Encoder : NumberEncoder<long>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
    }

    public class UInt64Encoder : NumberEncoder<ulong>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;
    }

    public class Int256Encoder : NumberEncoder<BigInteger>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
        public override Span<byte> Encode(Span<byte> buffer)
        {
            Span<byte> arr = _val.ToByteArray();
            arr.CopyTo(buffer.Slice(32 - arr.Length));
            return buffer.Slice(32);
        }
    }

    public class UInt256Encoder : NumberEncoder<UInt256>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;

        static readonly Lazy<UInt256Encoder> UncheckedInstance = new Lazy<UInt256Encoder>(() => 
        {
            var inst = new UInt256Encoder();
            inst.SetTypeInfo(AbiTypeMap.GetSolidityTypeInfo("uint256"));
            return inst;
        });

        public override void SetValue(in UInt256 val)
        {
            // Skip unnecessary bounds check on max uint256 value.
            // An optimization only for this common type at the moment.
            if (_info.SolidityName == "uint256")
            {
                _val = val;
            }
            else
            {
                base.SetValue(val);
            }
        }

        /// <summary>
        /// Encodes a solidity 'uint256' (with no overflow checks since its the max value)
        /// </summary>
        public static Span<byte> Encode(Span<byte> buffer, in UInt256 val)
        {
            var encoder = UncheckedInstance.Value;
            encoder._val = val;
            return encoder.Encode(buffer);
        }

        public static new ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> buffer, out UInt256 val)
        {
            NumberEncoder<UInt256> encoder = UncheckedInstance.Value;
            return encoder.Decode(buffer, out val);
        }
    }


}
