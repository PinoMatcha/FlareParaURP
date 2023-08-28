using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class FlarePraRenderFeature : ScriptableRendererFeature {

    [SerializeField] private Shader shader;
    [SerializeField] private PostprocessTiming timing = PostprocessTiming.BeforePostprocess;
    [SerializeField] private bool applyToSceneView = true;

    private FlareParaRenderPass postProcessPass;

    // 初期化
    public override void Create() {
        postProcessPass = new FlareParaRenderPass(applyToSceneView, shader, timing);
    }

    // パス追加
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(postProcessPass);
    }
}