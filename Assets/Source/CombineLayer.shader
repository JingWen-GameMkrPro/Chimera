Shader "CombineLayer"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       
       ZWrite Off Cull Off

       Pass
       {
           Name "CombineLayer"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #pragma vertex Vert
           #pragma fragment Frag


           TEXTURE2D_X(_CaptureTex);

           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord.xy;

               half4 uiColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

               half4 bgColor = SAMPLE_TEXTURE2D_X(_CaptureTex, sampler_LinearClamp, uv);

               half3 finalColor = lerp(bgColor.rgb, uiColor.rgb, uiColor.a);

               return half4(finalColor, 1.0f);
           }

           ENDHLSL
       }
   }
}