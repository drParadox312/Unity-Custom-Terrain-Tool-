// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.Experimental.TerrainAPI
{
    internal class CustomTerrainTools_PaintTexture : TerrainPaintTool<CustomTerrainTools_PaintTexture>
    {
        const string toolName = "Paint Texture : Custom Tool";

        Editor m_TemplateMaterialEditor = null;
        Editor m_SelectedTerrainLayerInspector = null;

        [SerializeField]
        TerrainLayer m_SelectedTerrainLayer = null;

        int selectedTerrainLayerIndex = -1;


        [SerializeField]
        public CustomTerrainTools_SplatPaintRules splatPaintRules;


        public override string GetName()
        {
            return toolName;
        }

        public override string GetDesc()
        {
            return "Paints the selected material layer onto the terrain texture";
        }


        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("CustomTerrainTools/PaintTexture"));
            return m_Material;
        }




        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {

            if(splatPaintRules == null)
                return false;


            Material mat = GetPaintMaterial();
            
            mat.SetTexture("_BrushTex", editContext.brushTexture);





            // gathering heightmap
            BrushTransform brushXformForGatheringHeightmap = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContextForGatheringHeightmap = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXformForGatheringHeightmap.GetBrushXYBounds(), 1);
            if (paintContextForGatheringHeightmap == null)
                return false;

            RenderTexture gatheredHeightmap = RenderTexture.GetTemporary(terrain.terrainData.heightmapTexture.descriptor);
            Graphics.Blit(paintContextForGatheringHeightmap.sourceRenderTexture, gatheredHeightmap); //, TerrainPaintUtility.GetBlitMaterial(), 0);
            TerrainPaintUtility.ReleaseContextResources(paintContextForGatheringHeightmap);


            // painting alphamap
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintTexture(terrain, brushXform.GetBrushXYBounds(), m_SelectedTerrainLayer);
            if (paintContext == null)
                return false;


            float targetAlpha = 1.0f;       // always 1.0 now -- no subtractive painting (we assume this in the ScatterAlphaMap)
            Vector4 brushParams = new Vector4(editContext.brushStrength, targetAlpha, splatPaintRules.useHeightTransition ? 1f : 0f, splatPaintRules.useAngleTransition ? 1f : 0f);
            Vector4 paintRulesParametersHeight = new Vector4(splatPaintRules.minHeightStart, splatPaintRules.minHeightEnd, splatPaintRules.maxHeightStart, splatPaintRules.maxHeightEnd);
            Vector4 paintRulesParametersAngle = new Vector4(splatPaintRules.minAngleStart, splatPaintRules.minAngleEnd, splatPaintRules.maxAngleStart, splatPaintRules.maxAngleEnd);
            Vector4 paintRulesInversionAndUsage = new Vector4(splatPaintRules.inverseHeightRule ? 1f : 0f , splatPaintRules.inverseAngleRule ? 1f : 0f , splatPaintRules.applyHeightRule ? 1f : 0f,  splatPaintRules.applyAngleRule ? 1f : 0f);
            mat.SetVector("_BrushParams", brushParams);
            mat.SetVector("_TerrainSize", (Vector4)terrain.terrainData.size);
            mat.SetVector("_PaintRulesParametersHeight", paintRulesParametersHeight);
            mat.SetVector("_PaintRulesParametersAngle", paintRulesParametersAngle);
            mat.SetVector("_PaintRulesInversionAndUsage", paintRulesInversionAndUsage);
            mat.SetTexture("_Heightmap", gatheredHeightmap);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);
            TerrainPaintUtility.EndPaintTexture(paintContext, "Terrain Paint - Texture");



            RenderTexture.ReleaseTemporary(gatheredHeightmap);
            return true;
        }







        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            // We're only doing painting operations, early out if it's not a repaint
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial(), 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }






        private const int kTemplateMaterialEditorControl = 67890;
        private const int kSelectedTerrainLayerEditorControl = 67891;

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            Editor.DrawFoldoutInspector(terrain.materialTemplate, ref m_TemplateMaterialEditor);

            EditorGUILayout.Space();

            int layerIndex = TerrainPaintUtility.FindTerrainLayerIndex(terrain, m_SelectedTerrainLayer);
            layerIndex = TerrainLayerUtility.ShowTerrainLayersSelectionHelper(terrain, layerIndex);
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedTerrainLayer = layerIndex != -1 ? terrain.terrainData.terrainLayers[layerIndex] : null;
                Save(true);
            }

            TerrainLayerUtility.ShowTerrainLayerGUI(terrain, m_SelectedTerrainLayer, ref m_SelectedTerrainLayerInspector,
                (m_TemplateMaterialEditor as MaterialEditor)?.customShaderGUI as ITerrainLayerCustomUI);
            EditorGUILayout.Space();

            editContext.ShowBrushesGUI(5);



            if(layerIndex < 0)
                return ;

            



        //  load SplatPaintRules (ScriptableObeject)  //
        {
            string name = "SplatPaintRules_"+ terrain.terrainData.terrainLayers[layerIndex].name + ".asset";
            string folder = "Assets/Terrain Tool Extender/SplatPaintRules";

            if(!AssetDatabase.IsValidFolder("Assets/Terrain Tool Extender"))
            {
                AssetDatabase.CreateFolder("Assets", "Terrain Tool Extender");
                AssetDatabase.CreateFolder("Assets/Terrain Tool Extender", "SplatPaintRules");
                splatPaintRules = (CustomTerrainTools_SplatPaintRules)ScriptableObject.CreateInstance("CustomTerrainTools_SplatPaintRules"); 
                AssetDatabase.CreateAsset(splatPaintRules, folder + "/" + name);
            }

            if(!AssetDatabase.IsValidFolder("Assets/Terrain Tool Extender/SplatPaintRules"))
            {
                AssetDatabase.CreateFolder("Assets/Terrain Tool Extender", "SplatPaintRules");
                splatPaintRules = (CustomTerrainTools_SplatPaintRules)ScriptableObject.CreateInstance("CustomTerrainTools_SplatPaintRules"); 
                AssetDatabase.CreateAsset(splatPaintRules, folder + "/" + name);
            }
            
            splatPaintRules = (CustomTerrainTools_SplatPaintRules)AssetDatabase.LoadAssetAtPath(folder + "/" + name, typeof(CustomTerrainTools_SplatPaintRules));

            if(splatPaintRules == null)
            {
                splatPaintRules = (CustomTerrainTools_SplatPaintRules)ScriptableObject.CreateInstance("CustomTerrainTools_SplatPaintRules"); 
                AssetDatabase.CreateAsset(splatPaintRules, folder + "/" + name);;
                splatPaintRules.SetMaxHeights(terrain.terrainData.size.y);
                splatPaintRules.SetDirty();
                MarkSceneDirty(terrain.gameObject);
            }
        }





            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            splatPaintRules.applyHeightRule = EditorGUILayout.Toggle("Apply Height Rule", splatPaintRules.applyHeightRule);
            EditorGUILayout.EndHorizontal();

            if(splatPaintRules.applyHeightRule)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Height : Min(start - end) -- Max(start - end)");
                splatPaintRules.minHeightStart = EditorGUILayout.DelayedFloatField(splatPaintRules.minHeightStart);
                if(splatPaintRules.useHeightTransition)
                {
                    splatPaintRules.minHeightEnd = EditorGUILayout.DelayedFloatField(splatPaintRules.minHeightEnd);
                    splatPaintRules.maxHeightStart = EditorGUILayout.DelayedFloatField(splatPaintRules.maxHeightStart);
                }
                splatPaintRules.maxHeightEnd = EditorGUILayout.DelayedFloatField(splatPaintRules.maxHeightEnd);
                EditorGUILayout.EndHorizontal();


                if(splatPaintRules.useHeightTransition)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.MinMaxSlider(ref splatPaintRules.minHeightEnd, ref splatPaintRules.maxHeightStart, 0f, terrain.terrainData.size.y);
                    EditorGUILayout.EndHorizontal();
                    splatPaintRules.minHeightEnd = Mathf.Max(splatPaintRules.minHeightEnd, splatPaintRules.minHeightStart);
                    splatPaintRules.maxHeightStart = Mathf.Min(splatPaintRules.maxHeightStart, splatPaintRules.maxHeightEnd);
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.MinMaxSlider(ref splatPaintRules.minHeightStart, ref splatPaintRules.maxHeightEnd, 0f, terrain.terrainData.size.y);
                EditorGUILayout.EndHorizontal();
                

                EditorGUILayout.BeginHorizontal();
                splatPaintRules.inverseHeightRule = EditorGUILayout.Toggle("Inverse Height", splatPaintRules.inverseHeightRule);
                splatPaintRules.useHeightTransition = EditorGUILayout.Toggle("Height Transition", splatPaintRules.useHeightTransition);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            


            EditorGUILayout.Space();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            splatPaintRules.applyAngleRule = EditorGUILayout.Toggle("Apply Angle Rule", splatPaintRules.applyAngleRule);
            EditorGUILayout.EndHorizontal();

            if(splatPaintRules.applyAngleRule)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Angle : Min(start - end) -- Max(start - end)");
                splatPaintRules.minAngleStart = EditorGUILayout.DelayedFloatField(splatPaintRules.minAngleStart);
                if(splatPaintRules.useAngleTransition)
                {
                    splatPaintRules.minAngleEnd = EditorGUILayout.DelayedFloatField(splatPaintRules.minAngleEnd);
                    splatPaintRules.maxAngleStart = EditorGUILayout.DelayedFloatField(splatPaintRules.maxAngleStart);
                }
                splatPaintRules.maxAngleEnd = EditorGUILayout.DelayedFloatField(splatPaintRules.maxAngleEnd);
                EditorGUILayout.EndHorizontal();

                if(splatPaintRules.useAngleTransition)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.MinMaxSlider(ref splatPaintRules.minAngleEnd, ref splatPaintRules.maxAngleStart, 0f, 90f);
                    EditorGUILayout.EndHorizontal();
                    splatPaintRules.minAngleEnd = Mathf.Max(splatPaintRules.minAngleEnd, splatPaintRules.minAngleStart);
                    splatPaintRules.maxAngleStart = Mathf.Min(splatPaintRules.maxAngleStart, splatPaintRules.maxAngleEnd);
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.MinMaxSlider(ref splatPaintRules.minAngleStart, ref splatPaintRules.maxAngleEnd, 0f, 90f);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                splatPaintRules.inverseAngleRule = EditorGUILayout.Toggle("Inverse Angle", splatPaintRules.inverseAngleRule);
                splatPaintRules.useAngleTransition = EditorGUILayout.Toggle("Angle Transition", splatPaintRules.useAngleTransition);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            if(EditorGUI.EndChangeCheck())
            {
                splatPaintRules.SetDirty();
                MarkSceneDirty(terrain.gameObject);
            }

            selectedTerrainLayerIndex = layerIndex;

            splatPaintRules = (CustomTerrainTools_SplatPaintRules)EditorGUILayout.ObjectField(splatPaintRules, typeof(CustomTerrainTools_SplatPaintRules), true);
        }


        void MarkSceneDirty(GameObject gO)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gO.scene);
        }

    }
}
