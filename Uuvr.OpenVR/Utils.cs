using System;
using UnityEngine;

namespace Uuvr.OpenVR;

internal static class Utils
{
    // this version does not clamp [0..1]
    private static Quaternion Slerp(Quaternion a, Quaternion b, float t)
    {
        var cosom = Mathf.Clamp(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w, -1.0f, 1.0f);
        if (cosom < 0.0f)
        {
            b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
            cosom = -cosom;
        }

        float sclp, sclq;
        if (1.0f - cosom > 0.0001f)
        {
            var omega = Mathf.Acos(cosom);
            var sinom = Mathf.Sin(omega);
            sclp = Mathf.Sin((1.0f - t) * omega) / sinom;
            sclq = Mathf.Sin(t * omega) / sinom;
        }
        else
        {
            // "from" and "to" very close, so do linear interp
            sclp = 1.0f - t;
            sclq = t;
        }

        return new Quaternion(
            sclp * a.x + sclq * b.x,
            sclp * a.y + sclq * b.y,
            sclp * a.z + sclq * b.z,
            sclp * a.w + sclq * b.w);
    }

    private static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            Lerp(a.x, b.x, t),
            Lerp(a.y, b.y, t),
            Lerp(a.z, b.z, t));
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private static float Saturate(float a)
    {
        return a < 0 ? 0 : a > 1 ? 1 : a;
    }

    public static Vector2 Saturate(Vector2 a)
    {
        return new Vector2(Saturate(a.x), Saturate(a.y));
    }

    private static float Abs(float a)
    {
        return a < 0 ? -a : a;
    }

    public static Vector2 Abs(Vector2 a)
    {
        return new Vector2(Abs(a.x), Abs(a.y));
    }

    private static float CopySign(float sizeVal, float signVal)
    {
        return Mathf.Sign(signVal) == 1 ? Mathf.Abs(sizeVal) : -Mathf.Abs(sizeVal);
    }

    private static Quaternion Rotation(this Matrix4x4 matrix)
    {
        var q = new Quaternion
        {
            w = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2,
            x = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2,
            y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2,
            z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2
        };
        q.x = CopySign(q.x, matrix.m21 - matrix.m12);
        q.y = CopySign(q.y, matrix.m02 - matrix.m20);
        q.z = CopySign(q.z, matrix.m10 - matrix.m01);
        return q;
    }

    private static Vector3 Position(this Matrix4x4 matrix)
    {
        return new Vector3(matrix.m03, matrix.m13, matrix.m23);
    }

    [Serializable]
    public struct RigidTransform
    {
        public Vector3 pos;
        public Quaternion rot;

        public static RigidTransform Identity => new(Vector3.zero, Quaternion.identity);

        public static RigidTransform FromLocal(Transform t)
        {
            return new RigidTransform(t.localPosition, t.localRotation);
        }

        public RigidTransform(Vector3 pos, Quaternion rot)
        {
            this.pos = pos;
            this.rot = rot;
        }

        public RigidTransform(Transform t)
        {
            pos = t.position;
            rot = t.rotation;
        }

        public RigidTransform(Transform from, Transform to)
        {
            var inv = Quaternion.Inverse(from.rotation);
            rot = inv * to.rotation;
            pos = inv * (to.position - from.position);
        }

        public RigidTransform(HmdMatrix34_t pose)
        {
            var m = Matrix4x4.identity;

            m[0, 0] = pose.m0;
            m[0, 1] = pose.m1;
            m[0, 2] = -pose.m2;
            m[0, 3] = pose.m3;

            m[1, 0] = pose.m4;
            m[1, 1] = pose.m5;
            m[1, 2] = -pose.m6;
            m[1, 3] = pose.m7;

            m[2, 0] = -pose.m8;
            m[2, 1] = -pose.m9;
            m[2, 2] = pose.m10;
            m[2, 3] = -pose.m11;

            pos = m.Position();
            rot = m.Rotation();
        }

        public RigidTransform(HmdMatrix44_t pose)
        {
            var m = Matrix4x4.identity;

            m[0, 0] = pose.m0;
            m[0, 1] = pose.m1;
            m[0, 2] = -pose.m2;
            m[0, 3] = pose.m3;

            m[1, 0] = pose.m4;
            m[1, 1] = pose.m5;
            m[1, 2] = -pose.m6;
            m[1, 3] = pose.m7;

            m[2, 0] = -pose.m8;
            m[2, 1] = -pose.m9;
            m[2, 2] = pose.m10;
            m[2, 3] = -pose.m11;

            m[3, 0] = pose.m12;
            m[3, 1] = pose.m13;
            m[3, 2] = -pose.m14;
            m[3, 3] = pose.m15;

            pos = m.Position();
            rot = m.Rotation();
        }

        public HmdMatrix44_t ToHmdMatrix44()
        {
            var m = Matrix4x4.TRS(pos, rot, Vector3.one);
            var pose = new HmdMatrix44_t
            {
                m0 = m[0, 0],
                m1 = m[0, 1],
                m2 = -m[0, 2],
                m3 = m[0, 3],
                m4 = m[1, 0],
                m5 = m[1, 1],
                m6 = -m[1, 2],
                m7 = m[1, 3],
                m8 = -m[2, 0],
                m9 = -m[2, 1],
                m10 = m[2, 2],
                m11 = -m[2, 3],
                m12 = m[3, 0],
                m13 = m[3, 1],
                m14 = -m[3, 2],
                m15 = m[3, 3]
            };

            return pose;
        }

        public HmdMatrix34_t ToHmdMatrix34()
        {
            var m = Matrix4x4.TRS(pos, rot, Vector3.one);
            var pose = new HmdMatrix34_t
            {
                m0 = m[0, 0],
                m1 = m[0, 1],
                m2 = -m[0, 2],
                m3 = m[0, 3],
                m4 = m[1, 0],
                m5 = m[1, 1],
                m6 = -m[1, 2],
                m7 = m[1, 3],
                m8 = -m[2, 0],
                m9 = -m[2, 1],
                m10 = m[2, 2],
                m11 = -m[2, 3]
            };

            return pose;
        }

        public override bool Equals(object? o)
        {
            if (o is not RigidTransform rigidTransform) return false;
            return pos == rigidTransform.pos && rot == rigidTransform.rot;
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() ^ rot.GetHashCode();
        }

        public static bool operator ==(RigidTransform a, RigidTransform b)
        {
            return a.pos == b.pos && a.rot == b.rot;
        }

        public static bool operator !=(RigidTransform a, RigidTransform b)
        {
            return a.pos != b.pos || a.rot != b.rot;
        }

        public static RigidTransform operator *(RigidTransform a, RigidTransform b)
        {
            return new RigidTransform
            {
                rot = a.rot * b.rot,
                pos = a.pos + a.rot * b.pos
            };
        }

        public void Inverse()
        {
            rot = Quaternion.Inverse(rot);
            pos = -(rot * pos);
        }

        public RigidTransform GetInverse()
        {
            var t = new RigidTransform(pos, rot);
            t.Inverse();
            return t;
        }

        public void Multiply(RigidTransform a, RigidTransform b)
        {
            rot = a.rot * b.rot;
            pos = a.pos + a.rot * b.pos;
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
            return Quaternion.Inverse(rot) * (point - pos);
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            return pos + rot * point;
        }

        public static Vector3 operator *(RigidTransform t, Vector3 v)
        {
            return t.TransformPoint(v);
        }

        public static RigidTransform Interpolate(RigidTransform a, RigidTransform b, float t)
        {
            return new RigidTransform(Vector3.Lerp(a.pos, b.pos, t), Quaternion.Slerp(a.rot, b.rot, t));
        }

        public void Interpolate(RigidTransform to, float t)
        {
            pos = Lerp(pos, to.pos, t);
            rot = Slerp(rot, to.rot, t);
        }
    }
}