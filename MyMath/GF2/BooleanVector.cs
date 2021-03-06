﻿using System;
using System.Collections.Generic;
using System.Linq;
using Boolean = MyLibrary.Types.Boolean;

namespace MyMath.GF2
{
    public class BooleanVector : Vector<bool>
    {
        public BooleanVector(IEnumerable<bool> bools) : base(bools)
        {
        }

        public BooleanVector()
        {
        }

        public BooleanVector(bool value)
            : base(value)
        {
        }

        public override string ToString()
        {
            return string.Join("", this.Select(b => b ? "1" : "0"));
        }

        public static BooleanVector And(BooleanVector vector1, BooleanVector vector2)
        {
            return
                new BooleanVector(
                    Enumerable.Range(0, Math.Min(vector1.Count, vector2.Count))
                        .Select(index => Boolean.And(vector1[index], vector2[index])));
        }

        public static BooleanVector Xor(BooleanVector vector1, BooleanVector vector2)
        {
            int count = Math.Min(vector1.Count, vector2.Count);
            return
                new BooleanVector(Enumerable.Range(0, count)
                    .Select(index => Boolean.Xor(vector1[index], vector2[index])))
                {
                    vector1.GetRange(count, vector1.Count - count),
                    vector2.GetRange(count, vector2.Count - count),
                };
        }

        public static int Module(BooleanVector booleanVector, BooleanVector vector2)
        {
            return Module(And(booleanVector, vector2));
        }

        public static int Module(BooleanVector booleanVector)
        {
            return booleanVector.Count(b => b);
        }

        public static bool IsZero(BooleanVector booleanVector)
        {
            return !booleanVector.Any(b => b);
        }

        public static bool NotZero(BooleanVector booleanVector)
        {
            return booleanVector.Any(b => b);
        }
    }
}