Shader "Meta/Debug/EnvironmentDepthHeat_BiRP"
{
    Properties
    {
        _MinDepth("Min Depth (meters)", Float) = 0.2
        _MaxDepth("Max Depth (meters)", Float) = 4.0
        _EyeOverride("Eye Override (-1=auto, 0=L, 1=R)", Float) = -1
        _ShowInvalidAsTransparent("Invalid=Transparent (0/1)", Float) = 1
        _Invert("Invert Gradient (0/1)", Float) = 0
        _Alpha("Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "DepthHeatPreview"
            Tags { "LightMode"="Always" }

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "UnityCG.cginc"

            // EnvironmentDepth from Meta XR（由 EnvironmentDepthManager 設定為全域）
            UNITY_DECLARE_TEX2DARRAY(_EnvironmentDepthTexture);

            // Controls
            float _MinDepth, _MaxDepth, _EyeOverride, _ShowInvalidAsTransparent;
            float _Invert, _Alpha;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v){
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // 0..1 映射到彩色熱度圖（紅→黃→綠→青→藍），可依喜好調色點
            float3 HeatColor(float t){
                // 可反轉
                t = saturate(_Invert > 0.5 ? (1.0 - t) : t);

                // 五段錨點
                const float3 C0 = float3(1.0, 0.0, 0.0); // Red
                const float3 C1 = float3(1.0, 1.0, 0.0); // Yellow
                const float3 C2 = float3(0.0, 1.0, 0.0); // Green
                const float3 C3 = float3(0.0, 1.0, 1.0); // Cyan
                const float3 C4 = float3(0.0, 0.0, 1.0); // Blue

                float seg = t * 4.0;
                if (seg < 1.0)      return lerp(C0, C1, seg);
                else if (seg < 2.0) return lerp(C1, C2, seg - 1.0);
                else if (seg < 3.0) return lerp(C2, C3, seg - 2.0);
                else                return lerp(C3, C4, seg - 3.0);
            }

            float PickEyeIndex(){
                if (_EyeOverride >= 0.0) return _EyeOverride;
                #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON)
                    #ifdef unity_StereoEyeIndex
                        return (float)unity_StereoEyeIndex;
                    #else
                        return 0.0;
                    #endif
                #else
                    return 0.0;
                #endif
            }

            fixed4 frag(v2f i) : SV_Target
            {
                #ifdef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #endif

                float eyeIdx = PickEyeIndex();

                // 線性深度（公尺）。0 表示無效像素
                float d = UNITY_SAMPLE_TEX2DARRAY(_EnvironmentDepthTexture, float3(i.uv, eyeIdx)).r;

                if (d <= 0.0){
                    if (_ShowInvalidAsTransparent > 0.5) return fixed4(0,0,0,0);
                    else                                  return fixed4(0,0,0,_Alpha);
                }

                // 拉到 0..1
                float t = saturate((d - _MinDepth) / max(1e-6, (_MaxDepth - _MinDepth)));
                float3 col = HeatColor(t);
                return fixed4(col, _Alpha);
            }
            ENDCG
        }
    }
}
