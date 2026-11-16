using UnityEngine;
using UnityEngine.UI;

namespace CehennemSenfonisi
{
    [AddComponentMenu("")]
    public sealed class PortalEmbers : MaskableGraphic
    {
        private struct Ember
        {
            public float Angle;
            public float Offset;
            public float Rate;
            public float Brightness;
            public float Size;
        }

        private static readonly Ember[] Embers = CreateEmbers();
        private double elapsed;
        private float rotation;

        internal void SetMotion(double time, float angle)
        {
            elapsed = time;
            rotation = angle;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect viewport = rectTransform.rect;
            float edge = viewport.size.magnitude * 0.54f;
            float sizeScale = viewport.height / 420f;

            foreach (Ember ember in Embers)
            {
                float phase = (float)((ember.Offset + elapsed * ember.Rate) % 1.0);
                float distance = edge * Mathf.Pow(phase, 2.45f);
                float length = sizeScale * (2f + 15f * phase * phase);
                float heading = ember.Angle + rotation;
                Vector2 direction = new Vector2(Mathf.Cos(heading), -Mathf.Sin(heading));
                Vector2 tail = viewport.center + direction * Mathf.Max(0f, distance - length);
                Vector2 head = viewport.center + direction * distance;
                float halfWidth = sizeScale * ember.Size * (0.7f + phase) * 0.5f;
                Vector2 side = new Vector2(-direction.y, direction.x) * halfWidth;
                float alpha = Mathf.Sin(Mathf.PI * phase);
                Color32 tint = new Color32(244, 98, 55, (byte)Mathf.RoundToInt(255f * alpha * alpha * ember.Brightness));
                int index = mesh.currentVertCount;
                mesh.AddVert(tail + side, tint, Vector4.zero);
                mesh.AddVert(head + side, tint, Vector4.zero);
                mesh.AddVert(head - side, tint, Vector4.zero);
                mesh.AddVert(tail - side, tint, Vector4.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index, index + 2, index + 3);
            }
        }

        private static Ember[] CreateEmbers()
        {
            int seed = 37;
            var particles = new Ember[29];
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = new Ember
                {
                    Angle = Next(ref seed) * 2f * Mathf.PI,
                    Offset = Next(ref seed),
                    Rate = 0.13f + Next(ref seed) * 0.17f,
                    Brightness = 0.2f + Next(ref seed) * 0.35f,
                    Size = 0.45f + Next(ref seed) * 0.7f
                };
            }

            return particles;
        }

        private static float Next(ref int seed)
        {
            seed = (int)((long)seed * 16807 % int.MaxValue);
            return (float)((double)seed / int.MaxValue);
        }
    }
}
