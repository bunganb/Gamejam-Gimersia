Shader "Custom/SlashFill"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        [Enum(Horizontal,0,Vertical,1,Radial,2)] _FillType ("Fill Type", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FillAmount;
            float _FillType;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // Calculate fill based on type
                float fillMask = 0;
                
                if (_FillType < 0.5) // Horizontal
                {
                    fillMask = step(i.uv.x, _FillAmount);
                }
                else if (_FillType < 1.5) // Vertical
                {
                    fillMask = step(i.uv.y, _FillAmount);
                }
                else // Radial
                {
                    float2 center = float2(0.5, 0.5);
                    float2 delta = i.uv - center;
                    float angle = atan2(delta.y, delta.x);
                    angle = (angle / 3.14159 + 1.0) * 0.5; // Normalize to 0-1
                    fillMask = step(angle, _FillAmount);
                }
                
                col.a *= fillMask;
                return col;
            }
            ENDCG
        }
    }
}