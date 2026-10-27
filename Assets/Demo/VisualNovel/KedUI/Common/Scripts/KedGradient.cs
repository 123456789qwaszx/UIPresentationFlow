using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 그림 없이 그리는 세로 그라디언트(시안의 상단 그림자 TopGradient). 사각형 하나의 위·아래 꼭짓점 색만 바꾼다.
    /// 이미지 위에 얹는 메시 효과라 늘려도 계단이 생기지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KedGradient : BaseMeshEffect
    {
        [SerializeField] private Color top = new(0.075f, 0.149f, 0.231f, 0.333f);
        [SerializeField] private Color bottom = new(0.075f, 0.149f, 0.231f, 0f);

        private readonly List<UIVertex> _vertices = new();

        public void SetColors(Color topColor, Color bottomColor)
        {
            top = topColor;
            bottom = bottomColor;

            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            _vertices.Clear();
            vh.GetUIVertexStream(_vertices);

            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < _vertices.Count; i++)
            {
                float y = _vertices[i].position.y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            float height = Mathf.Max(0.0001f, maxY - minY);

            for (int i = 0; i < _vertices.Count; i++)
            {
                UIVertex vertex = _vertices[i];
                float t = (vertex.position.y - minY) / height;
                vertex.color = Color.Lerp(bottom, top, t) * (Color)vertex.color;
                _vertices[i] = vertex;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(_vertices);
        }
    }
}
