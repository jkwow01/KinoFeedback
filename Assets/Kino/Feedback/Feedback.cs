//
// Kino/Feedback - framebuffer feedback effect for Unity
//
// Copyright (C) 2016 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using UnityEngine;
using UnityEngine.Rendering;

namespace Kino
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class Feedback : MonoBehaviour
    {
        #region Editable properties

        /// Feedback color
        public Color color {
            get { return _color; }
            set { _color = value; }
        }

        [SerializeField]
        Color _color = Color.white;

        /// Horizontal offset for feedback
        public float offsetX {
            get { return _offsetX; }
            set { _offsetX = value; }
        }

        [SerializeField, Range(-1, 1)]
        float _offsetX = 0;

        /// Vertical offset for feedback
        public float offsetY {
            get { return _offsetY; }
            set { _offsetY = value; }
        }

        [SerializeField, Range(-1, 1)]
        float _offsetY = 0;

        /// Center-axis rotation for feedback
        public float rotation {
            get { return _rotation; }
            set { _rotation = value; }
        }

        [SerializeField, Range(-5, 5)]
        float _rotation = 0;

        /// Scale factor for feedback
        public float scale {
            get { return _scale; }
            set { _scale = value; }
        }

        [SerializeField, Range(0.95f, 1.05f)]
        float _scale = 1;

        /// Disables bilinear filter
        public bool jaggies {
            get { return _jaggies; }
            set { _jaggies = value; }
        }

        [SerializeField]
        bool _jaggies = false;

        #endregion

        #region Private members

        // feedback shader reference
        Shader feedbackShader {
            get {
                const string name = "Hidden/Kino/Feedback";
                return _feedbackShader ? _feedbackShader : Shader.Find(name);
            }
        }

        [SerializeField] Shader _feedbackShader;

        // material used for feedback
        Material feedbackMaterial {
            get {
                if (_feedbackMaterial == null) {
                    _feedbackMaterial = new Material(feedbackShader);
                    _feedbackMaterial.hideFlags = HideFlags.DontSave;
                }
                return _feedbackMaterial;
            }
        }

        Material _feedbackMaterial;

        // effect target camera
        Camera targetCamera {
            get { return GetComponent<Camera>(); }
        }

        // render texture for storing previous frame
        RenderTexture delayBuffer {
            get {
                if (_delayBuffer == null) {
                    var cam = targetCamera;
                    _delayBuffer = RenderTexture.GetTemporary(
                        cam.pixelWidth, cam.pixelHeight,
                        0, RenderTextureFormat.DefaultHDR
                    );
                }
                return _delayBuffer;
            }
        }

        RenderTexture _delayBuffer;

        // command buffer for feedback blitting
        CommandBuffer feedbackCommand {
            get {
                if (_feedbackCommand == null) {
                    _feedbackCommand = new CommandBuffer();
                    _feedbackCommand.name = "Kino.Feedback";
                    targetCamera.AddCommandBuffer(
                        CameraEvent.BeforeImageEffects, _feedbackCommand
                    );
                }
                return _feedbackCommand;
            }
        }

        CommandBuffer _feedbackCommand;

        // initialization flag
        bool _initialized;

        #endregion

        #region MonoBehaviour Functions

        void OnDisable()
        {
            if (_feedbackCommand != null) {
                targetCamera.RemoveCommandBuffer(
                    CameraEvent.BeforeImageEffects, _feedbackCommand
                );
            }

            _feedbackCommand = null;

            if (_delayBuffer != null) {
                RenderTexture.ReleaseTemporary(_delayBuffer);
            }

            _delayBuffer = null;

            if (_feedbackMaterial != null) {
                DestroyImmediate(_feedbackMaterial);
            }

            _feedbackMaterial = null;
        }

        void Update()
        {
            if (!_initialized)
            {
                feedbackCommand.Blit(
                    delayBuffer as Texture,
                    BuiltinRenderTextureType.CameraTarget,
                    feedbackMaterial, 0
                );
                _initialized = true;
            }

            var m = feedbackMaterial;
            m.SetColor("_Color", _color);

            var offset = new Vector2(_offsetX, _offsetY) * -0.05f;
            m.SetVector("_Offset", offset);

            var angle = -Mathf.Deg2Rad * _rotation;
            var sin = Mathf.Sin(angle);
            var cos = Mathf.Cos(angle);
            var rotation = new Vector4(cos, sin, -sin, cos);
            m.SetVector("_Rotation", rotation);

            m.SetFloat("_Scale", 2 - _scale);

            delayBuffer.filterMode =
                _jaggies ? FilterMode.Point : FilterMode.Bilinear;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, delayBuffer);
            Graphics.Blit(source, destination);
        }

        #endregion
    }
}
