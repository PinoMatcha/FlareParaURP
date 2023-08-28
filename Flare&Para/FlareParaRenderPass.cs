using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum PostprocessTiming {
    AfterOpaque,
    BeforePostprocess,
    AfterPostprocess
}

public class FlareParaRenderPass : ScriptableRenderPass {

    private const string RenderPassName = nameof(FlareParaRenderPass);
    private new readonly ProfilingSampler profilingSampler = new ProfilingSampler(RenderPassName);

    private readonly bool applyToSceneView;
    private readonly Material material;

    private FlareParaVolume volume;

    public FlareParaRenderPass(bool applyToSceneView, Shader shader, PostprocessTiming timing) {
        if (shader == null)
            return;

        this.applyToSceneView = applyToSceneView;

        // 描画タイミング
        renderPassEvent = GetRenderPassEvent(timing);

        // マテリアルを作成
        material = CoreUtils.CreateEngineMaterial(shader);

        // Volumeコンポーネントを取得
        var volumeStack = VolumeManager.instance.stack;
        volume = volumeStack.GetComponent<FlareParaVolume>();
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        // RenderTextureDescriptorの取得
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        // 深度は使わないので0に
        descriptor.depthBufferBits = 0;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        // マテリアルがなければ何もしない
        if (material == null)
            return;

        // カメラのポストプロセス設定が無効になっていたら何もしない
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        // カメラがシーンビューカメラかつシーンビューに適用しない場合には何もしない
        if (!applyToSceneView && renderingData.cameraData.cameraType == CameraType.SceneView)
            return;

        // コマンドバッファを作成
        var cmd = CommandBufferPool.Get(RenderPassName);
        cmd.Clear();

        using (new ProfilingScope(cmd, profilingSampler)) {
            if (volume.IsActive()) {
                // パラメータ適応
                {
                    material.SetColor(Shader.PropertyToID("_FlareColor"), volume.flareColor.value);
                    material.SetFloat(Shader.PropertyToID("_FlarePower"), volume.flarePower.value);
                    material.SetFloat(Shader.PropertyToID("_FlareIntensity"), volume.flareIntensity.value);
                    material.SetFloat(Shader.PropertyToID("_FlareStrength"), volume.flareStrength.value);

                    material.SetColor(Shader.PropertyToID("_ParaColor"), volume.paraColor.value);
                    material.SetFloat(Shader.PropertyToID("_ParaPower"), volume.paraPower.value);
                    material.SetFloat(Shader.PropertyToID("_ParaIntensity"), volume.paraIntensity.value);
                    material.SetFloat(Shader.PropertyToID("_ParaStrength"), volume.paraStrength.value);

                    material.SetFloat(Shader.PropertyToID("_Degree"), volume.degree.value);
                }

                // SwapBuffer（URP12）を使えば一度のBlitでカラーバッファにマテリアルが適応される
                // TempRTも必要ない
                Blit(cmd, ref renderingData, material);
            }

            // コマンドバッファの実行&リリース
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private static RenderPassEvent GetRenderPassEvent(PostprocessTiming postprocessTiming) {
        switch (postprocessTiming) {
            case PostprocessTiming.AfterOpaque:
                return RenderPassEvent.AfterRenderingSkybox;
            case PostprocessTiming.BeforePostprocess:
                return RenderPassEvent.BeforeRenderingPostProcessing;
            case PostprocessTiming.AfterPostprocess:
                return RenderPassEvent.AfterRendering;
            default:
                throw new ArgumentOutOfRangeException(nameof(postprocessTiming), postprocessTiming, null);
        }
    }
}