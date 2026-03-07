Shader "Custom/PaintBrush"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _BrushPos ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Float) = 0.05
        _BrushColor ("Brush Color", Color) = (1, 0, 0, 1)
        _BrushHardness ("Brush Hardness", Float) = 0.9
        _AspectRatio ("Aspect Ratio (Width/Height)", Float) = 1.0
        _UseBrushTexture ("Use Brush Texture", Float) = 0
        _AlphaThreshold ("Alpha Threshold", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _BrushPos;
            float _BrushSize;
            float4 _BrushColor;
            float _BrushHardness;
            float _AspectRatio;
            float _UseBrushTexture;
            float _AlphaThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 取得原本的顏色
                fixed4 baseColor = tex2D(_MainTex, i.uv);

                // 計算與筆刷中心的距離（補償長寬比，確保圓形不變形）
                float2 diff = i.uv - _BrushPos.xy;
                diff.y *= _AspectRatio;  // Y 軸乘以長寬比補償
                float dist = length(diff);

                float brushStrength = 0.0;

                if (_UseBrushTexture > 0.5)
                {
                    // 使用筆刷貼圖
                    // 將差值轉換為筆刷貼圖的 UV（-size ~ +size 映射到 0 ~ 1）
                    float2 brushUV = diff / (_BrushSize * 2.0) + 0.5;

                    // 檢查是否在筆刷範圍內
                    if (brushUV.x >= 0.0 && brushUV.x <= 1.0 && brushUV.y >= 0.0 && brushUV.y <= 1.0)
                    {
                        float rawAlpha = tex2D(_BrushTex, brushUV).a;
                        // 使用閾值讓邊緣更銳利，避免灰色混合
                        brushStrength = rawAlpha > _AlphaThreshold ? 1.0 : 0.0;
                    }
                }
                else
                {
                    // 使用硬邊圓形（原本的方式）
                    brushStrength = dist < _BrushSize ? 1.0 : 0.0;
                }

                // 混合顏色（新顏色覆蓋舊顏色）
                fixed4 finalColor = lerp(baseColor, _BrushColor, brushStrength);

                return finalColor;
            }
            ENDCG
        }
    }
}
