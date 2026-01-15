Shader "UI/CartoonWater_Editable"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {} // נשאר בשביל UI
        _ColorA ("Deep Color", Color) = (0.05, 0.55, 0.95, 1)
        _ColorB ("Shallow Color", Color) = (0.25, 0.85, 1.0, 1)
        _Alpha  ("Global Alpha", Float) = 1

        _WaveDir ("Wave Direction (XY)", Vector) = (0, 1, 0, 0)
        _WaveScale ("Wave Scale", Float) = 6
        _WaveSpeed ("Wave Speed", Float) = 0.6
        _WaveStrength ("Wave Strength", Float) = 0.02

        _DetailScale ("Detail Scale", Float) = 14
        _DetailSpeed ("Detail Speed", Float) = 0.9
        _DetailStrength ("Detail Strength", Float) = 0.015

        _LineColor ("Foam Line Color", Color) = (1,1,1,1)
        _LineDensity ("Foam Line Density", Float) = 10
        _LineSpeed ("Foam Line Speed", Float) = 0.8
        _LineSharpness ("Foam Line Sharpness", Float) = 6
        _LineAmount ("Foam Line Amount", Float) = 0.65

        // UI סטנדרטי
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_CLIP_RECT)] _UseUIClipRect ("Use UI Clip Rect", Float) = 1
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use UI Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TextureSampleAdd;

            fixed4 _ColorA;
            fixed4 _ColorB;
            fixed4 _LineColor;
            float _Alpha;

            float4 _WaveDir;
            float _WaveScale;
            float _WaveSpeed;
            float _WaveStrength;

            float _DetailScale;
            float _DetailSpeed;
            float _DetailStrength;

            float _LineDensity;
            float _LineSpeed;
            float _LineSharpness;
            float _LineAmount;

            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            float2 SafeNormalize(float2 v)
            {
                float len = max(1e-5, length(v));
                return v / len;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = Hash21(i);
                float b = Hash21(i + float2(1,0));
                float c = Hash21(i + float2(0,1));
                float d = Hash21(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UI base sample (כדי לשמור תאימות ל-Image tint / atlas)
                fixed4 baseCol = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                float2 dir = SafeNormalize(_WaveDir.xy);
                float t = _Time.y;

                // UV distortion (שני שכבות: wave + detail)
                float phase1 = dot(i.uv, dir) * _WaveScale + t * _WaveSpeed;
                float wave1 = sin(phase1) * _WaveStrength;

                float2 dir2 = SafeNormalize(float2(-dir.y, dir.x));
                float phase2 = dot(i.uv, dir2) * (_WaveScale * 0.75) + t * (_WaveSpeed * 1.35);
                float wave2 = sin(phase2) * (_WaveStrength * 0.7);

                float n = Noise(i.uv * _DetailScale + dir * (t * _DetailSpeed));
                float detail = (n - 0.5) * 2.0 * _DetailStrength;

                float2 duv = i.uv + (dir * wave1) + (dir2 * wave2) + (dir * detail);

                // Gradient water color (לפי Y, אפשר לשנות אם רוצים לפי X)
                float g = saturate(duv.y);
                fixed4 water = lerp(_ColorA, _ColorB, g);

                // Foam lines: “קווים” שנעים בכיוון הגלים
                float linePhase = dot(duv, dir) * _LineDensity + t * _LineSpeed;
                float s = sin(linePhase) * 0.5 + 0.5;        // 0..1
                float lines = pow(s, max(0.0001, _LineSharpness));
                lines *= _LineAmount;

                fixed4 col = water;
                col.rgb = lerp(col.rgb, _LineColor.rgb, saturate(lines) * _LineColor.a);

                // multiply by UI sprite alpha/tint (כדי שאם ה-Image שקוף זה יעבוד)
                col.a *= baseCol.a * _Alpha;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
