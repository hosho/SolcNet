﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SolCodeGen.AbiEncoding.Encoders
{
    public class ArrayEncoder<TItem> : AbiTypeEncoder<IEnumerable<TItem>>
    {

        IAbiTypeEncoder<TItem> _itemEncoder;

        public ArrayEncoder(IAbiTypeEncoder<TItem> itemEncoder)
        {
            _itemEncoder = itemEncoder;
        }

        public override int GetEncodedSize()
        {
            switch (_info.Category)
            {
                case SolidityTypeCategory.DynamicArray:
                    {
                        int len = _itemEncoder.GetEncodedSize() * _val.Count();
                        return 32 + len;
                    }
                case SolidityTypeCategory.FixedArray:
                    {
                        int len = _itemEncoder.GetEncodedSize() * _info.ArrayLength;
                        return len;
                    }
                default:
                    throw UnsupportedTypeException();
            }
        }

        public override Span<byte> Encode(Span<byte> buffer)
        {
            if (_info.Category == SolidityTypeCategory.DynamicArray)
            {
                // write length prefix
                buffer = UInt256Encoder.Encode(buffer, _val.Count());
            }
            else if (_info.Category == SolidityTypeCategory.FixedArray)
            {
                var itemCount = _val.Count();
                if (itemCount != _info.ArrayLength)
                {
                    throw new ArgumentOutOfRangeException($"Fixed size array type '{_info.SolidityName}' needs exactly {_info.ArrayLength} items, was given {itemCount}");
                }
            }
            else
            {
                throw UnsupportedTypeException();
            }

            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                buffer = _itemEncoder.Encode(buffer);
            }

            return buffer;
        }

        public override ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> buffer, out IEnumerable<TItem> val)
        {
            // TODO: ArrayEncoder.Decode
            throw new NotImplementedException();
        }

    }

}
