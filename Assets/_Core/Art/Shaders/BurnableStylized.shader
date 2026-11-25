// Stylized burnable surface: the burnt area, the flame and the cartoon flame shell, all computed from the
// object's own geometry. The prop's albedo comes from `_BaseMap` (tinted, tiled), the burnt area and the flame
// are procedural — no per-object mask texture, no per-object material instance, no VFX Graph, and the WebGL
// budget in TODO.md. The shell writes seven scalars through a MaterialPropertyBlock: `_Burn` (the burn axis
// 0..1), `_Fire` (0..1), `_BurnOrigin` (the point the heat landed at, in object space), `_BurnExtent` (the
// mesh's own size), `_OutlineWidth`, `_ShellHeight` and `_EmissionStrength`, so the burnt look grows over the
// whole surface from the strike.
//
// The burnt area is ONE BLOB of burnt paint, not a disc: `BurnCoverage` measures how far the front has
// travelled past a point, from `_BurnOrigin`, with the front's radius modulated by a **quantized 3D value
// noise** sampled on the strike direction, and unburnt islands cut by a second noise field at double the
// frequency. Every mask is a `step` — the edge is a hard ragged line, never a smoothstep wash, and the tone
// rings inside the blob are quantized levels, so the outline is an organic blot with corners, not a circle
// and not a square grid. `Burn` 0 = no burnt paint at all; `Burn` 1 = the blob's front is past the farthest
// vertex, so the whole mesh is burnt.
//
// The frame is the same coverage field: a hard char band just outside the burnt core (`_BurnRim`), lit by a
// bright front line while there is fire. It is shaped by the same noise, so the frame is as ragged as the blob.
//
// The flame lattice cuts every cell with a **random convex polygon**: the intersection of `_FlamePolySides`
// half-planes at random angles and distances, so a flame hole is a cornered N-gon — never a square, never a
// circle.
Shader "FnB/Burnable Stylized"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (0.62, 0.55, 0.45, 1)
        _CharColor("Char Colour", Color) = (0.32, 0.30, 0.28, 1)
        _AshColor("Ash Colour", Color) = (0.06, 0.06, 0.06, 1)

        [HDR] _GlowColor("Burn Front", Color) = (1, 0.35, 0.08, 1)
        [HDR] _FireColor("Fire Colour", Color) = (1, 0.45, 0.10, 1)
        [HDR] _FlareColor("Flare Colour", Color) = (1, 0.90, 0.40, 1)

        // The burn state, written by the shell.
        _Burn("Burn (0..1)", Range(0, 1)) = 0
        _BurnOrigin("Burn Origin (object space)", Vector) = (0, 0, 0, 0)
        _BurnExtent("Burn Extent", Float) = 1
        _BurnReach("Burn Reach (extent units)", Float) = 1

        // The blob: its raggedness, the size of its lobes, how many tone levels it cuts into, the width of
        // the frame band, and the islands it leaves unburnt.
        _BurnWobble("Blob Raggedness", Float) = 0.35
        _BurnNoiseScale("Blob Noise Scale", Float) = 3.0
        _BurnLevels("Blob Levels", Range(1, 16)) = 4
        _BurnRim("Frame Width", Float) = 0.12
        _BurnPerforation("Unburnt Islands", Float) = 0.30

        // Hard graphic lines inside the burnt area, at every level boundary.
        _EdgeWidth("Edge Width", Range(0, 0.4)) = 0.07
        _EdgeDarken("Edge Darken", Range(0, 1)) = 0.45

        // The flame: its own lattice, its own level count, its own random polygon per cell.
        _Fire("Fire (0..1)", Range(0, 1)) = 0
        _FlameFacetSize("Flame Facet Size", Float) = 0.05
        _FlameLevels("Flame Levels", Range(1, 16)) = 5
        _FlamePolySides("Flame Polygon Sides", Range(3, 8)) = 5
        _FlamePolyRadius("Flame Polygon Radius", Float) = 0.45
        _FireLick("Flame Lick", Float) = 0.35
        _FlickerSpeed("Flicker Speed", Float) = 8
        _EmissionStrength("Emission Strength", Float) = 2.5

        // The silhouette passes.
        _OutlineColor("Outline Colour", Color) = (0.08, 0.08, 0.08, 1)
        _OutlineWidth("Outline Width", Float) = 0.012
        _ShellBase("Shell Base", Float) = 0.02
        _ShellHeight("Shell Height", Float) = 0.35
        _ShellLevels("Shell Levels", Range(1, 16)) = 3
        [HDR] _ShellColor("Shell Colour", Color) = (1, 0.42, 0.08, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 100

        // The properties and the shape functions are shared by every pass. The pass structs are declared
        // per pass, because URP's own ShadowCaster and DepthOnly includes bring their own.
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _CharColor;
            float4 _AshColor;
            float4 _GlowColor;
            float4 _FireColor;
            float4 _FlareColor;
            float4 _OutlineColor;
            float4 _ShellColor;
            float4 _BurnOrigin;
            float4 _BaseMap_ST;   // the base map's tiling and offset
            float _Burn;
            float _BurnExtent;
            float _BurnReach;
            float _BurnWobble;
            float _BurnNoiseScale;
            float _BurnLevels;
            float _BurnRim;
            float _BurnPerforation;
            float _EdgeWidth;
            float _EdgeDarken;
            float _Fire;
            float _FlameFacetSize;
            float _FlameLevels;
            float _FlamePolySides;
            float _FlamePolyRadius;
            float _FireLick;
            float _FlickerSpeed;
            float _EmissionStrength;
            float _OutlineWidth;
            float _ShellBase;
            float _ShellHeight;
            float _ShellLevels;
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
        CBUFFER_END

        float Hash3(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.34);
            return frac((p.x + p.y) * p.z * 101.1);
        }

        // One octave of 3D value noise, 0..1: the eight corners of the lattice cell around `p` are hashed,
        // and the result is interpolated inside the cell. Quantized by the caller, so what reaches the pixel
        // is a hard-edged band, not a gradient — the interpolation is only the shape of the field.
        float Noise3(float3 p)
        {
            float3 cell = floor(p);
            float3 f = p - cell;
            float3 u = f * f * (3.0 - 2.0 * f);

            float n000 = Hash3(cell);
            float n100 = Hash3(cell + float3(1, 0, 0));
            float n010 = Hash3(cell + float3(0, 1, 0));
            float n110 = Hash3(cell + float3(1, 1, 0));
            float n001 = Hash3(cell + float3(0, 0, 1));
            float n101 = Hash3(cell + float3(1, 0, 1));
            float n011 = Hash3(cell + float3(0, 1, 1));
            float n111 = Hash3(cell + float3(1, 1, 1));

            float a = lerp(n000, n100, u.x);
            float b = lerp(n010, n110, u.x);
            float c = lerp(n001, n101, u.x);
            float d = lerp(n011, n111, u.x);
            return lerp(lerp(a, b, u.y), lerp(c, d, u.y), u.z);
        }

        // How far the burn front is past a point, in `_BurnExtent` units: positive = burnt, negative = the
        // prop's own surface. The front's radius is `Burn` × the reach, modulated per direction by a
        // quantized noise lobe, so the outline is an organic blot of ragged lobes — never a disc, never a
        // square staircase. Unburnt islands are cut inside the blob by a second field at double the
        // frequency, gated by `1 - Burn` so they close as the blob finishes growing.
        //
        // `reach` is the radius the front has to reach to cover the mesh, in extent units, written by the
        // shell as `_BurnReach`: the farthest mesh corner from `_BurnOrigin`. A small margin makes sure the
        // last pixel is covered at `Burn` = 1.
        //
        // At `Burn` 0 the coverage is `-u - islands` ≤ 0 everywhere: no burnt paint. At `Burn` 1 the islands
        // term is zero and `reach ≥ u`, so the coverage is positive over the whole mesh. The core is cut by a
        // `step` just above zero, so the strike point itself stays unburnt while the front has not moved.
        float BurnCoverage(float3 positionOS)
        {
            float extent = max(_BurnExtent, 0.0001f);
            float levels = max(_BurnLevels, 1.0);

            float3 offset = positionOS - _BurnOrigin.xyz;
            float dist = length(offset);
            float u = dist / extent;
            float3 direction = offset / max(dist, 0.0001f);

            float lobe = floor(saturate(Noise3(direction * _BurnNoiseScale)) * levels) / levels;
            float islands = floor(saturate(Noise3(positionOS * _BurnNoiseScale * 2.0)) * levels) / levels;

            float reach = max(_BurnReach, 1.0) + 0.05;
            float front = _Burn * (reach + _BurnWobble * lobe);
            return front - u - _BurnPerforation * islands * (1.0 - _Burn);
        }

        // A hard line at the boundary of every level: the band position inside a level, cut at
        // `_EdgeWidth`. No smoothing — a `step` is the corner, a lerp would be the soap again.
        float EdgeLine(float coverage, float levels, float width)
        {
            return step(frac(coverage * max(levels, 1.0)), width);
        }

        // The grouping: the square flame lattice, in object space. One call gives the cell index (the
        // identity the value is hashed from) and the cell-local position in [-0.5, 0.5] — the flame
        // polygon is drawn in it.
        void FacetOf(float3 p, float size, out float3 cell, out float3 local)
        {
            size = max(size, 0.0001f);
            cell = floor(p / size);
            local = p / size - cell - 0.5;
        }

        // A flame shape: a random convex polygon drawn as the intersection of `_FlamePolySides` half-planes
        // through the cell centre, each at a random angle and a random distance from it. Flat faces, straight
        // edges, sharp corners; 1 inside, 0 outside — a `step`, so the border is a hard line, never a wash.
        float FlamePolygon(float3 local, float3 cell)
        {
            int sides = (int)clamp(round(_FlamePolySides), 3.0, 8.0);
            float minDist = 2.0;
            for (int i = 0; i < sides; ++i)
            {
                float h1 = Hash3(cell + float3(1.7 * i, 3.1 * i, 4.9 * i + 2.3));
                float h2 = Hash3(cell + float3(5.3 * i + 0.7, 2.9 * i, 1.3 * i + 5.1));

                // A plane's direction on the sphere: azimuth from h1, elevation from h2.
                float elevation = h2 * 2.0 - 1.0;
                float spread = sqrt(saturate(1.0 - elevation * elevation));
                float azimuth = h1 * 6.2831853;
                float3 dir = float3(cos(azimuth) * spread, elevation, sin(azimuth) * spread);

                // Each plane sits at `_FlamePolyRadius` ± 25% from the centre: the polygon is irregular.
                float radius = _FlamePolyRadius * (1.0 + (h2 - 0.5) * 0.5);
                minDist = min(minDist, radius - dot(dir, local));
            }
            return step(0.0, minDist);
        }

        // The flame for a point: the cell's flat presence level (0..1, quantized), the raw noise the shell
        // needs, and the point's polygon inside the cell (1/0). The lattice scrolls downward with
        // `_FlickerSpeed`, so the polygons rise by popping from one cell to the next — the flicker is
        // discrete, never a fade; flames are strongest on top.
        void FlameAt(float3 positionOS, float vertical, out float noise, out float presence, out float shape)
        {
            float3 cell, local;
            FacetOf(positionOS + float3(0, -_Time.y * _FlickerSpeed, 0), _FlameFacetSize, cell, local);

            float levels = max(_FlameLevels, 1.0);
            noise = floor(Hash3(cell) * levels) / levels;
            presence = floor(saturate((_Fire * (1.0 + _FireLick) - noise) * 1.4
                + vertical * _FireLick * _Fire) * levels) / levels;
            shape = FlamePolygon(local, cell);
        }
        ENDHLSL

        // ---- The cartoon outline: an inverted hull, drawn before the surface. ----
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 30
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // View-space extrusion: the line keeps its authored width whatever the object's scale is.
                // A swizzle cannot be the target of `+=` in HLSL, so the expanded position is built whole.
                float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz));
                float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_V,
                    TransformObjectToWorldNormal(input.normalOS)));
                float3 expanded = positionVS + normalVS * max(_OutlineWidth, 0.0);
                output.positionCS = mul(UNITY_MATRIX_P, float4(expanded, 1.0));
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        // ---- Flame tongues around the silhouette: the extruded hull, additive. The extrusion is quantized
        // and cut by the same per-cell polygon as the surface flame, so the shell has flat plateaus with
        // polygonal corners, not a soft bloom. ----
        Pass
        {
            Name "FlameShell"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend One One

            HLSLPROGRAM
            #pragma target 30
            #pragma vertex ShellVertex
            #pragma fragment ShellFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float3 positionOS : TEXCOORD0;
                float tongue : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShellVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float extent = max(_BurnExtent, 0.0001f);
                float vertical = saturate(input.positionOS.y / extent + 0.5);
                float noise, presence, shape;
                FlameAt(input.positionOS.xyz, vertical, noise, presence, shape);

                float levels = max(_ShellLevels, 1.0);
                float tongue = floor(saturate(_Fire * 1.2 - noise * 0.9) * levels) / levels * shape;

                output.positionOS = input.positionOS.xyz + input.normalOS * (_ShellBase + _ShellHeight * tongue * vertical);
                output.tongue = tongue;

                float3 positionWS = TransformObjectToWorld(output.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 ShellFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float tongue = saturate(input.tongue * 2.0 - 0.5);
                float3 colour = lerp(_ShellColor.rgb, _FlareColor.rgb,
                    saturate(input.tongue * 1.4 - 0.5));
                float3 flame = colour * tongue * _EmissionStrength * 0.4;
                flame = MixFog(flame, input.fogFactor);
                return half4(flame, 1.0);
            }
            ENDHLSL
        }

        // ---- The surface: the burnt blob, the frame at its edge, the flame on the geometry. ----
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 30
            #pragma vertex BurnableVertex
            #pragma fragment BurnableFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float2 baseUV : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings BurnableVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.baseUV = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;   // the authored tiling
                return output;
            }

            half4 BurnableFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = input.positionWS;
                float3 normalWS = normalize(input.normalWS);
                float extent = max(_BurnExtent, 0.0001f);
                float burnLevels = max(_BurnLevels, 1.0);
                float flameLevels = max(_FlameLevels, 1.0);

                // The blob: `coverage` is how far the front is past this point. The burnt core is a `step`
                // at zero — a hard ragged edge, no wash. Its tone is quantized by depth, so the black is cut
                // into flat rings: char near the edge, ash in the core.
                float coverage = BurnCoverage(input.positionOS);
                float burnt = step(0.0001f, coverage);
                float tone = floor(saturate(coverage / max(_BurnRim, 0.0001f) * 2.0) * burnLevels) / burnLevels;

                // The frame: the band just outside the core, `_BurnRim` wide, on the same noise field, so its
                // outline is as ragged as the blob's. It exists only once the burn has started.
                float started = step(0.02, _Burn);
                float frame = step(-_BurnRim, coverage) * (1.0 - burnt) * started;

                // The prop's own surface, tinted; the burnt paint is flat, so the texture stays visible on the
                // part the fire has not reached and the rings stay graphic.
                float3 surface = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV).rgb;
                float3 albedo = lerp(surface, _CharColor.rgb, frame);
                albedo = lerp(albedo, lerp(_CharColor.rgb, _AshColor.rgb, tone), burnt);

                // A hard graphic line at every ring boundary inside the burnt area only.
                albedo *= 1.0 - EdgeLine(coverage, burnLevels, _EdgeWidth) * _EdgeDarken * saturate(burnt + frame);

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 diffuse = albedo * mainLight.color
                    * saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation;
                float3 ambient = SampleSH(normalWS) * albedo;

                // Flame over the surface: the lattice cuts a flat random polygon per cell, hotter on top,
                // flickering by popping between levels.
                float vertical = saturate(input.positionOS.y / extent + 0.5);
                float noise, presence, shape;
                FlameAt(input.positionOS, vertical, noise, presence, shape);
                float fireMask = presence * shape;
                float flameT = floor(saturate(fireMask * 2.0 - 0.6) * flameLevels) / flameLevels;

                float3 flame = fireMask * lerp(_FireColor.rgb, _FlareColor.rgb, flameT);

                // The burn front: the bright line inside the frame, alive only while there is fire. A hard
                // band at the inner edge of the frame, cut at a third of its width.
                float front = step(-_BurnRim * 0.3, coverage) * (1.0 - burnt) * started * step(0.02, _Fire);

                float3 colour = diffuse + ambient + (flame + front * _GlowColor.rgb) * _EmissionStrength;
                colour = MixFog(colour, input.fogFactor);
                return half4(colour, 1.0);
            }
            ENDHLSL
        }

        // ---- URP bookkeeping: the object casts shadows and writes depth. ----
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 30
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 30
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Simple Lit"
}
