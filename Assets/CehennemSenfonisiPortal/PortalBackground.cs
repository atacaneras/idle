using System;
using UnityEngine;
using UnityEngine.UI;

namespace CehennemSenfonisi
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PortalBackground : MonoBehaviour
    {
        private const float CycleSeconds = 4.4f;
        private const float InitialTiltWeight = 0.65f;
        private const float RadiansToDegrees = 180f / Mathf.PI;

        [SerializeField, Range(0.25f, 2.5f)] private float fallSpeed = 1f;
        [SerializeField] private bool gentleRotation = true;
        [SerializeField] private bool showEmbers = true;
        [SerializeField, HideInInspector] private RawImage backLayer;
        [SerializeField, HideInInspector] private RawImage frontLayer;
        [SerializeField, HideInInspector] private PortalEmbers embers;

        private RectTransform viewport;
        private double elapsed = 0.35;
        private float tiltWeight = InitialTiltWeight;

        private void OnEnable()
        {
            viewport = (RectTransform)transform;
            elapsed = 0.35;
            tiltWeight = gentleRotation ? InitialTiltWeight : 0f;
            ApplyFrame();
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject))
                return;

            float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += delta * fallSpeed;
            float targetTilt = gentleRotation ? 1f : 0f;
            tiltWeight = Mathf.Lerp(tiltWeight, targetTilt, 1f - Mathf.Exp(-4f * delta));
            ApplyFrame();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && viewport != null)
                ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (backLayer == null || frontLayer == null || backLayer.texture == null)
                return;

            Rect target = viewport.rect;
            Vector2 textureSize = new Vector2(backLayer.texture.width, backLayer.texture.height);
            // The painted opening sits 57% down the image, leaving 43% below it.
            float maxTilt = gentleRotation || tiltWeight > 0.001f ? 0.05f : 0f;
            float coverScale = Mathf.Max(
                (target.width + target.height * maxTilt) / textureSize.x,
                (target.height + target.width * maxTilt) / (textureSize.y * 0.86f));

            float phase = (float)((elapsed / CycleSeconds) % 1.0);
            float nextPhase = Mathf.Repeat(phase + 0.5f, 1f);
            float angle = (float)Math.Sin(elapsed * 0.36) * 0.05f * tiltWeight;
            // Each layer restarts while hidden by the other one.
            float frontOpacity = (1f + Mathf.Cos(phase * 2f * Mathf.PI)) * 0.5f;
            Vector2 baseSize = textureSize * coverScale;
            Quaternion rotation = Quaternion.Euler(0f, 0f, -angle * RadiansToDegrees);

            SetLayerPose(backLayer, baseSize, rotation, phase, 1f);
            SetLayerPose(frontLayer, baseSize, rotation, nextPhase, frontOpacity);

            if (embers != null)
            {
                embers.enabled = showEmbers;
                if (showEmbers)
                    embers.SetMotion(elapsed, angle);
            }
        }

        private static void SetLayerPose(RawImage layer, Vector2 dimensions, Quaternion rotation, float phase, float opacity)
        {
            RectTransform rect = layer.rectTransform;
            rect.sizeDelta = dimensions;
            rect.localScale = Vector3.one * Mathf.Exp(0.94f * phase);
            rect.localRotation = rotation;
            Color tint = layer.color;
            tint.a = opacity;
            layer.color = tint;
        }
    }
}
