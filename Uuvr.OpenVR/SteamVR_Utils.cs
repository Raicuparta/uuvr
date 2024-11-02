using UnityEngine;

namespace Uuvr.OpenVR;

public static class SteamVR_Utils
{
	// this version does not clamp [0..1]
	private static Quaternion Slerp(Quaternion A, Quaternion B, float t)
	{
		var cosom = Mathf.Clamp(A.x * B.x + A.y * B.y + A.z * B.z + A.w * B.w, -1.0f, 1.0f);
		if (cosom < 0.0f)
		{
			B = new Quaternion(-B.x, -B.y, -B.z, -B.w);
			cosom = -cosom;
		}

		float sclp, sclq;
		if ((1.0f - cosom) > 0.0001f)
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
			sclp * A.x + sclq * B.x,
			sclp * A.y + sclq * B.y,
			sclp * A.z + sclq * B.z,
			sclp * A.w + sclq * B.w);
	}

	private static Vector3 Lerp(Vector3 A, Vector3 B, float t)
	{
		return new Vector3(
			Lerp(A.x, B.x, t),
			Lerp(A.y, B.y, t),
			Lerp(A.z, B.z, t));
	}

	private static float Lerp(float A, float B, float t)
	{
		return A + (B - A) * t;
	}

	private static float Saturate(float A)
	{
		return (A < 0) ? 0 : (A > 1) ? 1 : A;
	}

	public static Vector2 Saturate(Vector2 A)
	{
		return new Vector2(Saturate(A.x), Saturate(A.y));
	}

	private static float Abs(float A)
	{
		return (A < 0) ? -A : A;
	}

	public static Vector2 Abs(Vector2 A)
	{
		return new Vector2(Abs(A.x), Abs(A.y));
	}

	private static float _copysign(float sizeval, float signval)
	{
		return Mathf.Sign(signval) == 1 ? Mathf.Abs(sizeval) : -Mathf.Abs(sizeval);
	}

	private static Quaternion Rotation(this Matrix4x4 matrix)
	{
		Quaternion q = new Quaternion();
		q.w = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2;
		q.x = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2;
		q.y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2;
		q.z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2;
		q.x = _copysign(q.x, matrix.m21 - matrix.m12);
		q.y = _copysign(q.y, matrix.m02 - matrix.m20);
		q.z = _copysign(q.z, matrix.m10 - matrix.m01);
		return q;
	}

	private static Vector3 Position(this Matrix4x4 matrix)
	{
		return new Vector3(matrix.m03, matrix.m13, matrix.m23);
	}

	[System.Serializable]
	public struct RigidTransform
	{
		public Vector3 pos;
		public Quaternion rot;

		public static RigidTransform identity
		{
			get { return new RigidTransform(Vector3.zero, Quaternion.identity); }
		}

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
			this.pos = t.position;
			this.rot = t.rotation;
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

			m[0, 0] =  pose.m0;
			m[0, 1] =  pose.m1;
			m[0, 2] = -pose.m2;
			m[0, 3] =  pose.m3;

			m[1, 0] =  pose.m4;
			m[1, 1] =  pose.m5;
			m[1, 2] = -pose.m6;
			m[1, 3] =  pose.m7;

			m[2, 0] = -pose.m8;
			m[2, 1] = -pose.m9;
			m[2, 2] =  pose.m10;
			m[2, 3] = -pose.m11;

			this.pos = m.Position();
			this.rot = m.Rotation();
		}

		public RigidTransform(HmdMatrix44_t pose)
		{
			var m = Matrix4x4.identity;

			m[0, 0] =  pose.m0;
			m[0, 1] =  pose.m1;
			m[0, 2] = -pose.m2;
			m[0, 3] =  pose.m3;

			m[1, 0] =  pose.m4;
			m[1, 1] =  pose.m5;
			m[1, 2] = -pose.m6;
			m[1, 3] =  pose.m7;

			m[2, 0] = -pose.m8;
			m[2, 1] = -pose.m9;
			m[2, 2] =  pose.m10;
			m[2, 3] = -pose.m11;

			m[3, 0] =  pose.m12;
			m[3, 1] =  pose.m13;
			m[3, 2] = -pose.m14;
			m[3, 3] =  pose.m15;

			this.pos = m.Position();
			this.rot = m.Rotation();
		}

		public HmdMatrix44_t ToHmdMatrix44()
		{
			var m = Matrix4x4.TRS(pos, rot, Vector3.one);
			var pose = new HmdMatrix44_t();

			pose.m0  =  m[0, 0];
            pose.m1  =  m[0, 1];
			pose.m2  = -m[0, 2];
			pose.m3  =  m[0, 3];

			pose.m4  =  m[1, 0];
			pose.m5  =  m[1, 1];
			pose.m6  = -m[1, 2];
			pose.m7  =  m[1, 3];

			pose.m8  = -m[2, 0];
			pose.m9  = -m[2, 1];
			pose.m10 =  m[2, 2];
			pose.m11 = -m[2, 3];

			pose.m12 =  m[3, 0];
			pose.m13 =  m[3, 1];
			pose.m14 = -m[3, 2];
			pose.m15 =  m[3, 3];

			return pose;
		}

		public HmdMatrix34_t ToHmdMatrix34()
		{
			var m = Matrix4x4.TRS(pos, rot, Vector3.one);
			var pose = new HmdMatrix34_t();

			pose.m0  =  m[0, 0];
            pose.m1  =  m[0, 1];
			pose.m2  = -m[0, 2];
			pose.m3  =  m[0, 3];

			pose.m4  =  m[1, 0];
			pose.m5  =  m[1, 1];
			pose.m6  = -m[1, 2];
			pose.m7  =  m[1, 3];

			pose.m8  = -m[2, 0];
			pose.m9  = -m[2, 1];
			pose.m10 =  m[2, 2];
			pose.m11 = -m[2, 3];

			return pose;
		}

		public override bool Equals(object o)
		{
			if (o is RigidTransform)
			{
				RigidTransform t = (RigidTransform)o;
				return pos == t.pos && rot == t.rot;
			}
			return false;
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
			return pos + (rot * point);
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
			pos = SteamVR_Utils.Lerp(pos, to.pos, t);
			rot = SteamVR_Utils.Slerp(rot, to.rot, t);
		}
	}
	
	public static void QueueEventOnRenderThread(int eventID)
	{
		GL.IssuePluginEvent(OpenVrApiExtra.GetRenderEventFunc(), eventID);
    }
}

