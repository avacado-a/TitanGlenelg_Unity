using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public override string ToString() => $"({x}, {y}, {z})";
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color red => new Color(1, 0, 0);
        public static Color green => new Color(0, 1, 0);
        public static Color yellow => new Color(1, 1, 0);
    }
    public struct Rect { public float x, y, width, height; public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } }
    public class Object { public static void DestroyImmediate(Object obj) {} }
    public class GameObject : Object
    {
        public GameObject(string name) {}
        public T AddComponent<T>() where T : Component, new() => new T();
        public Transform transform => new Transform();
    }
    public class Component : Object
    {
        public T GetComponent<T>() where T : Component => default(T);
    }
    public class MonoBehaviour : Component {}
    public class Transform : Component
    {
        public Vector3 position;
    }
    public static class Time { public static float deltaTime = 0.016f; }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Clamp(float v, float min, float max) => Math.Min(Math.Max(v, min), max);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0f, 1f);
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
    }
    public static class Screen { public static int width = 1920; public static int height = 1080; }
    public static class GUI { public static Color color; public static GUISkin skin = new GUISkin(); }
    public class GUISkin { public GUIStyle box = new GUIStyle(); public GUIStyle window = new GUIStyle(); }
    public class GUIStyle {}
    public static class GUILayout
    {
        public static void BeginArea(Rect r) {}
        public static void BeginArea(Rect r, GUIStyle s) {}
        public static void BeginArea(Rect r, string t, GUIStyle s) {}
        public static void EndArea() {}
        public static void BeginHorizontal() {}
        public static void EndHorizontal() {}
        public static void Label(string text, params GUILayoutOption[] options) {}
        public static bool Button(string text, params GUILayoutOption[] options) => false;
        public static void Space(float px) {}
        public static GUILayoutOption Width(float w) => new GUILayoutOption();
    }
    public class GUILayoutOption {}
    public class HeaderAttribute : Attribute { public HeaderAttribute(string h) {} }
}

namespace Unity.Burst { public class BurstCompileAttribute : Attribute {} }

namespace Unity.Collections { public struct NativeArray<T> where T : struct {} }

namespace Unity.Mathematics
{
    public struct float2
    {
        public float x, y;
        public float2(float x, float y) { this.x = x; this.y = y; }
        public static float2 operator /(float2 a, float b) => new float2(a.x / b, a.y / b);
    }
    public struct float3
    {
        public float x, y, z;
        public float3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static float3 zero => new float3(0, 0, 0);
        public static float3 operator +(float3 a, float3 b) => new float3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static float3 operator -(float3 a, float3 b) => new float3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static float3 operator *(float3 a, float b) => new float3(a.x * b, a.y * b, a.z * b);
        public static float3 operator *(float a, float3 b) => new float3(a * b.x, a * b.y, a * b.z);
        public static float3 operator /(float3 a, float b) => new float3(a.x / b, a.y / b, a.z / b);
        public static float3 operator -(float3 a) => new float3(-a.x, -a.y, -a.z);
    }
    public struct quaternion
    {
        public static quaternion LookRotationSafe(float3 f, float3 u) => new quaternion();
    }
    public static class math
    {
        public const float PI = (float)Math.PI;
        public static float min(float a, float b) => Math.Min(a, b);
        public static float max(float a, float b) => Math.Max(a, b);
        public static int max(int a, int b) => Math.Max(a, b);
        public static float clamp(float v, float min, float max) => Math.Min(Math.Max(v, min), max);
        public static int clamp(int v, int min, int max) => Math.Min(Math.Max(v, min), max);
        public static float saturate(float v) => clamp(v, 0f, 1f);
        public static float sin(float x) => (float)Math.Sin(x);
        public static float cos(float x) => (float)Math.Cos(x);
        public static float exp(float x) => (float)Math.Exp(x);
        public static float sqrt(float x) => (float)Math.Sqrt(x);
        public static float radians(float deg) => deg * (PI / 180f);
        public static float degrees(float rad) => rad * (180f / PI);
        public static float round(float x) => (float)Math.Round(x);
        public static float length(float2 v) => (float)Math.Sqrt(v.x * v.x + v.y * v.y);
        public static float length(float3 v) => (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        public static float lengthsq(float3 v) => v.x * v.x + v.y * v.y + v.z * v.z;
        public static float2 normalize(float2 v) { float l = length(v); return l > 0 ? v / l : v; }
        public static float3 normalize(float3 v) { float l = length(v); return l > 0 ? v / l : v; }
        public static float3 cross(float3 a, float3 b) => new float3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float dot(float3 a, float3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static float3 lerp(float3 a, float3 b, float t) => a + (b - a) * saturate(t);
        public static float lerp(float a, float b, float t) => a + (b - a) * saturate(t);
        public static float atan2(float y, float x) => (float)Math.Atan2(y, x);
    }
}

namespace Unity.Entities
{
    public struct Entity {}
    public interface IComponentData {}
    public struct SystemState
    {
        public void RequireForUpdate<T>() {}
        public int Dependency { get; set; }
    }
    public static class SystemAPI
    {
        public static class Time { public static float DeltaTime => 0.016f; }
    }
    public interface ISystem
    {
        void OnCreate(ref SystemState state);
        void OnUpdate(ref SystemState state);
        void OnDestroy(ref SystemState state);
    }
    public interface IJobEntity {}
    public static class JobEntityExtensions
    {
        public static int ScheduleParallel<T>(this T job, int dep = 0) where T : struct, IJobEntity => 0;
    }
    public class UpdateInGroupAttribute : Attribute { public UpdateInGroupAttribute(Type t) {} }
    public class UpdateBeforeAttribute : Attribute { public UpdateBeforeAttribute(Type t) {} }
    public class SimulationSystemGroup {}
    public class FixedStepSimulationSystemGroup {}
}

namespace Unity.Physics
{
    public struct PhysicsVelocity
    {
        public Unity.Mathematics.float3 Linear;
        public Unity.Mathematics.float3 Angular;
    }
}
namespace Unity.Physics.Systems
{
    public class PhysicsSystemGroup {}
}

namespace Unity.Transforms
{
    public struct LocalTransform
    {
        public Unity.Mathematics.float3 Position;
        public Unity.Mathematics.quaternion Rotation;
        public float Scale;
    }
}

namespace NUnit.Framework
{
    public class TestFixtureAttribute : Attribute {}
    public class SetUpAttribute : Attribute {}
    public class TearDownAttribute : Attribute {}
    public class TestAttribute : Attribute {}
    public static class Assert
    {
        public static void AreEqual(object a, object b, string m = "")
        {
            if (!object.Equals(a, b)) throw new Exception($"Assertion failed: Expected {a}, got {b}. {m}");
        }
        public static void AreEqual(float a, float b, float tol, string m = "")
        {
            if (Math.Abs(a - b) > tol) throw new Exception($"Assertion failed: Expected {a} ~ {b} (+/- {tol}). {m}");
        }
        public static void IsTrue(bool b, string m = "")
        {
            if (!b) throw new Exception($"Assertion failed: Expected True. {m}");
        }
        public static void IsFalse(bool b, string m = "")
        {
            if (b) throw new Exception($"Assertion failed: Expected False. {m}");
        }
        public static void Greater(float a, float b, string m = "")
        {
            if (a <= b) throw new Exception($"Assertion failed: Expected {a} > {b}. {m}");
        }
        public static void Less(float a, float b, string m = "")
        {
            if (a >= b) throw new Exception($"Assertion failed: Expected {a} < {b}. {m}");
        }
    }
}
