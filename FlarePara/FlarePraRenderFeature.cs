using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class FlarePraRenderFeature : ScriptableRendererFeature {

    [SerializeField] private Shader _shader;
    [SerializeField] private Material _material;
    [SerializeField] private PostprocessTiming _timing = PostprocessTiming.AfterOpaque;
    [SerializeField] private bool _applyToSceneView = true;

    private FlareParaRenderPass _postProcessPass;

    public override void Create() {
        _postProcessPass = new FlareParaRenderPass(_applyToSceneView, _shader, _material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        _postProcessPass.Setup(renderer.cameraColorTarget, _timing);
        renderer.EnqueuePass(_postProcessPass);
    }
}