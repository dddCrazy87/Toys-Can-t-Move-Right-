Shader "Custom/PaintBrush"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BrushPos ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Float) = 0.05
        _BrushColor ("Brush Color", Color) = (1, 0, 0, 1)
        _BrushHardness ("Brush Hardness", Float) = 0.9
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
            float4 _BrushPos;
            float _BrushSize;
            float4 _BrushColor;
            float _BrushHardness;

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

                // 計算與筆刷中心的距離
                float dist = distance(i.uv, _BrushPos.xy);

                // 計算筆刷強度（硬邊緣，避免灰色暈染）
                float brushStrength = dist < _BrushSize ? 1.0 : 0.0;

                // 混合顏色（新顏色覆蓋舊顏色）
                fixed4 finalColor = lerp(baseColor, _BrushColor, brushStrength);

                return finalColor;
            }
            ENDCG
        }
    }
}
