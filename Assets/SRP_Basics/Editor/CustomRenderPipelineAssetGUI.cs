using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// [CustomEditor(typeof(CustomRenderPipelineAsset))]
public class CustomRenderPipelineAssetGUI : Editor
{
    private CustomRenderPipelineAsset SRPAsset;

    private void OnEnable()
    {
        SRPAsset = (CustomRenderPipelineAsset)target;
    }
    public override void OnInspectorGUI()
    {

    }
}
