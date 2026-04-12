using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade
{
    public readonly struct Vector3
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // Сложение векторов
        public static Vector3 operator +(Vector3 a, Vector3 b)
            => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        // Вычитание векторов
        public static Vector3 operator -(Vector3 a, Vector3 b)
            => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        // Умножение на число (скаляр)
        public static Vector3 operator *(Vector3 a, double scalar)
            => new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);

        // Длина вектора
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

}
