// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

    Shader "CustomTerrainTools/PaintTexture" {

    Properties { 
        _MainTex ("Texture", any) = "" {} 
        _Heightmap ("_Heightmap", any) = "" {} 
        }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _Heightmap;

            sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH              (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT          (_BrushParams[1])
            #define USE_HEIGHT_TRANSITION       (_BrushParams[2])
            #define USE_ANGLE_TRANSITION        (_BrushParams[3])

            float4 _TerrainSize;

            float4 _PaintRulesParametersHeight;
            #define MIN_HEIGHT_START        (_PaintRulesParametersHeight[0])
            #define MIN_HEIGHT_END          (_PaintRulesParametersHeight[1])
            #define MAX_HEIGHT_START        (_PaintRulesParametersHeight[2])
            #define MAX_HEIGHT_END          (_PaintRulesParametersHeight[3])

            float4 _PaintRulesParametersAngle;
            #define MIN_ANGLE_START        (_PaintRulesParametersAngle[0])
            #define MIN_ANGLE_END          (_PaintRulesParametersAngle[1])
            #define MAX_ANGLE_START        (_PaintRulesParametersAngle[2])
            #define MAX_ANGLE_END          (_PaintRulesParametersAngle[3])

            float4 _PaintRulesInversionAndUsage;
            #define HEIGHT_INVERSION                    (_PaintRulesInversionAndUsage[0])
            #define ANGLE_INVERSION                     (_PaintRulesInversionAndUsage[1])
            #define APPLY_HEIGHT_RULE                   (_PaintRulesInversionAndUsage[2])
            #define APPLY_ANGLE_RULE                    (_PaintRulesInversionAndUsage[3])

            


            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
                return o;
            }

            float ApplyBrush(float height, float brushStrength, float weight)
            {
                float targetHeight = weight * BRUSH_TARGETHEIGHT;
                if (targetHeight > height)
                {
                    height += brushStrength;
                    height = height < targetHeight ? height : targetHeight;
                }
                else
                {
                    height -= brushStrength;
                    height = height > targetHeight ? height : targetHeight;
                }
                return height;
            }

        ENDCG

        Pass    // 0 paint splat alphamap
        {
            Name "Paint Texture"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment PaintSplatAlphamap

            float4 PaintSplatAlphamap(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
                float alphaMap = tex2D(_MainTex, i.pcUV).r;

                float weightTotal = 0;

                const float2 coords [4] = { {-1,0}, { 1,0}, {0, -1}, { 0, 1} };

				float hc = UnpackHeightmap(tex2D(_Heightmap, heightmapUV));
				float hl = UnpackHeightmap(tex2D(_Heightmap, heightmapUV + coords[0] * _MainTex_TexelSize.xy));
				float hr = UnpackHeightmap(tex2D(_Heightmap, heightmapUV + coords[1] * _MainTex_TexelSize.xy));
				float ht = UnpackHeightmap(tex2D(_Heightmap, heightmapUV + coords[2] * _MainTex_TexelSize.xy));
				float hb = UnpackHeightmap(tex2D(_Heightmap, heightmapUV + coords[3] * _MainTex_TexelSize.xy));


                float _left_to_right_height_difference	= hl  -  hr ;
                float _down_to_up_height_difference		= hb  -  ht ; 
                float4 terrainSize = float4(4000,  1500,  4000,  0);
                float3 _normal = float3( _left_to_right_height_difference * terrainSize.x ,    1.0 * ((terrainSize.x / terrainSize.y)  +  (terrainSize.z / terrainSize.y)),   _down_to_up_height_difference * terrainSize.z) ;
                _normal = normalize(_normal) ;
        
                float dotValue = 1 - dot(_normal , float3(0.0 , 1.0 , 0.0));
                float angle = dotValue * 90;
                
                float height = hc * _TerrainSize.y;



                float weightHeight = 0;
                float weightAngle = 0;

   

                float heightDifferenceMin = MIN_HEIGHT_END - MIN_HEIGHT_START;
                float heightDifferenceMax = MAX_HEIGHT_END - MAX_HEIGHT_START;
                if(height >= MIN_HEIGHT_START  &&  height <= MAX_HEIGHT_END)
                {
                    if(height < MIN_HEIGHT_END * USE_HEIGHT_TRANSITION)
                    {
                        weightHeight = 1 - (MIN_HEIGHT_END - height) / heightDifferenceMin;
                    }
                    else if(height * USE_HEIGHT_TRANSITION > MAX_HEIGHT_START)
                    {
                        weightHeight = 1 - (height - MAX_HEIGHT_START) / heightDifferenceMax;
                    }
                    else
                    {
                       weightHeight = 1; 
                    }
                }



                float angleDifferenceMin = MIN_ANGLE_END - MIN_ANGLE_START;
                float angleDifferenceMax = MAX_ANGLE_END - MAX_ANGLE_START;
                if(angle >= MIN_ANGLE_START  &&  angle <= MAX_ANGLE_END)
                {
                    if(angle < MIN_ANGLE_END * USE_ANGLE_TRANSITION)
                    {
                        weightAngle = 1 - (MIN_ANGLE_END - angle) / angleDifferenceMin;
                    }
                    else if(angle * USE_ANGLE_TRANSITION > MAX_ANGLE_START)
                    {
                        weightAngle = 1 - (angle - MAX_ANGLE_START) / angleDifferenceMax;
                    }
                    else
                    {
                       weightAngle = 1; 
                    }
                }

                
                weightHeight = ((1 - APPLY_HEIGHT_RULE)  +  ((APPLY_HEIGHT_RULE) * weightHeight));
                weightAngle = ((1 - APPLY_ANGLE_RULE)  +  ((APPLY_ANGLE_RULE) * weightAngle));
                weightTotal = ((HEIGHT_INVERSION * (1 - weightHeight)  +  (1 - HEIGHT_INVERSION) * weightHeight))    *     ((ANGLE_INVERSION * (1 - weightAngle)  +  (1 - ANGLE_INVERSION) * weightAngle));
                

                return ApplyBrush(alphaMap, brushStrength, weightTotal);
            }

            ENDCG
        }

    }
    Fallback Off
}