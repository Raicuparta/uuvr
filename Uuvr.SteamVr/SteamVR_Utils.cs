//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Utilities for working with SteamVR
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Valve.VR;
using System.IO;

public static class SteamVR_Utils
{
	public class Event
	{
		public delegate void Handler(params object[] args);

		public static void Listen(string message, Handler action)
		{
			var actions = listeners[message] as Handler;
			if (actions != null)
			{
				listeners[message] = actions + action;
			}
			else
			{
				listeners[message] = action;
			}
		}

		public static void Remove(string message, Handler action)
		{
			var actions = listeners[message] as Handler;
			if (actions != null)
			{
				listeners[message] = actions - action;
			}
		}

		public static void Send(string message, params object[] args)
		{
			var actions = listeners[message] as Handler;
			if (actions != null)
			{
				actions(args);
			}
		}

		private static Hashtable listeners = new Hashtable();
	}


	public static bool IsValid(Vector3 vector)
	{
		return (float.IsNaN(vector.x) == false && float.IsNaN(vector.y) == false && float.IsNaN(vector.z) == false);
	}
	public static bool IsValid(Quaternion rotation)
	{
		return (float.IsNaN(rotation.x) == false && float.IsNaN(rotation.y) == false && float.IsNaN(rotation.z) == false && float.IsNaN(rotation.w) == false) &&
			(rotation.x != 0 || rotation.y != 0 || rotation.z != 0 || rotation.w != 0);
	}

	// this version does not clamp [0..1]
	public static Quaternion Slerp(Quaternion A, Quaternion B, float t)
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

	public static Vector3 Lerp(Vector3 A, Vector3 B, float t)
	{
		return new Vector3(
			Lerp(A.x, B.x, t),
			Lerp(A.y, B.y, t),
			Lerp(A.z, B.z, t));
	}

	public static float Lerp(float A, float B, float t)
	{
		return A + (B - A) * t;
	}

	public static double Lerp(double A, double B, double t)
	{
		return A + (B - A) * t;
	}

	public static float InverseLerp(Vector3 A, Vector3 B, Vector3 result)
	{
		return Vector3.Dot(result - A, B - A);
	}

	public static float InverseLerp(float A, float B, float result)
	{
		return (result - A) / (B - A);
	}

	public static double InverseLerp(double A, double B, double result)
	{
		return (result - A) / (B - A);
	}

	public static float Saturate(float A)
	{
		return (A < 0) ? 0 : (A > 1) ? 1 : A;
	}

	public static Vector2 Saturate(Vector2 A)
	{
		return new Vector2(Saturate(A.x), Saturate(A.y));
	}

	public static float Abs(float A)
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

	public static Quaternion GetRotation(this Matrix4x4 matrix)
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

	public static Vector3 GetPosition(this Matrix4x4 matrix)
	{
		var x = matrix.m03;
		var y = matrix.m13;
		var z = matrix.m23;

		return new Vector3(x, y, z);
	}

	public static Vector3 GetScale(this Matrix4x4 m)
	{
		var x = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
		var y = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
		var z = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);

		return new Vector3(x, y, z);
	}

	public static float GetLossyScale(Transform t)
	{
		return t.lossyScale.x;
	}

	private const string secretKey = "foobar";

	public static string GetBadMD5Hash(string usedString)
	{
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(usedString + secretKey);

		return GetBadMD5Hash(bytes);
	}
	public static string GetBadMD5Hash(byte[] bytes)
	{
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hash = md5.ComputeHash(bytes);

		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for (int i = 0; i < hash.Length; i++)
		{
			sb.Append(hash[i].ToString("x2"));
		}

		return sb.ToString();
	}
	public static string GetBadMD5HashFromFile(string filePath)
	{
		if (File.Exists(filePath) == false)
			return null;

		string data = File.ReadAllText(filePath);
		return GetBadMD5Hash(data + secretKey);
	}

	public static string SanitizePath(string path, bool allowLeadingSlash = true)
	{
		if (path.Contains("\\\\"))
			path = path.Replace("\\\\", "\\");
		if (path.Contains("//"))
			path = path.Replace("//", "/");

		if (allowLeadingSlash == false)
		{
			if (path[0] == '/' || path[0] == '\\')
				path = path.Substring(1);
		}

		return path;
	}

	public static System.Type FindType(string typeName)
	{
		var type = System.Type.GetType(typeName);
		if (type != null) return type;
		foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
		{
			type = a.GetType(typeName);
			if (type != null)
				return type;
		}
		return null;
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

			this.pos = m.GetPosition();
			this.rot = m.GetRotation();
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

			this.pos = m.GetPosition();
			this.rot = m.GetRotation();
		}

		public HmdMatrix44_t ToHmdMatrix44()
		{
			var m = Matrix4x4.TRS(pos, rot, Vector3.one);
			var pose = new HmdMatrix44_t();

			pose.m0 = m[0, 0];
			pose.m1 = m[0, 1];
			pose.m2 = -m[0, 2];
			pose.m3 = m[0, 3];

			pose.m4 = m[1, 0];
			pose.m5 = m[1, 1];
			pose.m6 = -m[1, 2];
			pose.m7 = m[1, 3];

			pose.m8 = -m[2, 0];
			pose.m9 = -m[2, 1];
			pose.m10 = m[2, 2];
			pose.m11 = -m[2, 3];

			pose.m12 = m[3, 0];
			pose.m13 = m[3, 1];
			pose.m14 = -m[3, 2];
			pose.m15 = m[3, 3];

			return pose;
		}

		public HmdMatrix34_t ToHmdMatrix34()
		{
			var m = Matrix4x4.TRS(pos, rot, Vector3.one);
			var pose = new HmdMatrix34_t();

			pose.m0 = m[0, 0];
			pose.m1 = m[0, 1];
			pose.m2 = -m[0, 2];
			pose.m3 = m[0, 3];

			pose.m4 = m[1, 0];
			pose.m5 = m[1, 1];
			pose.m6 = -m[1, 2];
			pose.m7 = m[1, 3];

			pose.m8 = -m[2, 0];
			pose.m9 = -m[2, 1];
			pose.m10 = m[2, 2];
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

	public delegate object SystemFn(CVRSystem system, params object[] args);

	public static object CallSystemFn(SystemFn fn, params object[] args)
	{
		var initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
		if (initOpenVR)
		{
			var error = EVRInitError.None;
			OpenVR.Init(ref error, EVRApplicationType.VRApplication_Utility);
		}

		var system = OpenVR.System;
		var result = (system != null) ? fn(system, args) : null;

		if (initOpenVR)
			OpenVR.Shutdown();

		return result;
	}
}