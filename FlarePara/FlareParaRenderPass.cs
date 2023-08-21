using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;

public enum PostprocessTiming {
    AfterOpaque,
    BeforePostprocess,
    AfterPostprocess
}

public class FlareParaRenderPass : ScriptableRenderPass {

    private const string RenderPassName = nameof(FlareParaRenderPass);

    private readonly bool _applyToSceneView;
    private readonly int _mainTexPropertyId = Shader.PropertyToID("_MainTex");
    private readonly Material _material;
    private readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");

    private RenderTargetHandle _afterPostProcessTexture;
    private RenderTargetIdentifier _cameraColorTarget;
    private RenderTargetHandle _tempRenderTargetHandle;
    private FlareParaVolume _volume;

    public FlareParaRenderPass(bool applyToSceneView, Shader shader, Material material) {
        if (shader == null) {
            return;
        }
        if (material == null) {
            return;
        }

        _applyToSceneView = applyToSceneView;
        _tempRenderTargetHandle.Init("_TempRT");

        // マテリアルを作成
        // _material = CoreUtils.CreateEngineMaterial(shader);
        _material = material;

        // RenderPassEvent.AfterRenderingではポストエフェクトを掛けた後のカラーテクスチャがこの名前で取得できる
        _afterPostProcessTexture.Init("_AfterPostProcessTexture");
    }

    public void Setup(RenderTargetIdentifier cameraColorTarget, PostprocessTiming timing) {
        _cameraColorTarget = cameraColorTarget;

        renderPassEvent = GetRenderPassEvent(timing);

        // Volumeコンポーネントを取得
        var volumeStack = VolumeManager.instance.stack;
        _volume = volumeStack.GetComponent<FlareParaVolume>();
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_material == null) {
            return;
        }

        // カメラのポストプロセス設定が無効になっていたら何もしない
        if (!renderingData.cameraData.postProcessEnabled) {
            return;
        }

        // カメラがシーンビューカメラかつシーンビューに適用しない場合には何もしない
        if (!_applyToSceneView && renderingData.cameraData.cameraType == CameraType.SceneView) {
            return;
        }

        if (!_volume.IsActive()) {
            return;
        }

        // renderPassEventがAfterRenderingの場合、カメラのカラーターゲットではなく_AfterPostProcessTextureを使う
        /*var source = renderPassEvent == RenderPassEvent.AfterRendering && renderingData.cameraData.resolveFinalTarget
            ? _afterPostProcessTexture.Identifier()
            : _cameraColorTarget;*/

        var renderer = renderingData.cameraData.renderer;
        var source = renderer.cameraColorTarget;

        // コマンドバッファを作成
        var cmd = CommandBufferPool.Get(RenderPassName);
        cmd.Clear();

        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        descriptor.colorFormat = renderingData.cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        cmd.GetTemporaryRT(_tempRenderTargetHandle.id, descriptor, FilterMode.Bilinear);

        _material.SetColor(Shader.PropertyToID("_FlareColor"), _volume.flareColor.value);
        _material.SetFloat(Shader.PropertyToID("_FlarePower"), _volume.flarePower.value);
        _material.SetFloat(Shader.PropertyToID("_FlareIntensity"), _volume.flareIntensity.value);
        _material.SetFloat(Shader.PropertyToID("_FlareStrength"), _volume.flareStrength.value);

        _material.SetColor(Shader.PropertyToID("_ParaColor"), _volume.paraColor.value);
        _material.SetFloat(Shader.PropertyToID("_ParaPower"), _volume.paraPower.value);
        _material.SetFloat(Shader.PropertyToID("_ParaIntensity"), _volume.paraIntensity.value);
        _material.SetFloat(Shader.PropertyToID("_ParaStrength"), _volume.paraStrength.value);

        _material.SetFloat(Shader.PropertyToID("_Degree"), _volume.degree.value);

        cmd.SetGlobalTexture(_mainTexPropertyId, source);

        Blit(cmd, ref renderingData, _material, 0);

        // 元のテクスチャから一時的なテクスチャにエフェクトを適用しつつ描画
        cmd.Blit(source, _tempRenderTargetHandle.Identifier(), _material);

        // 一時的なテクスチャから元のテクスチャに結果を書き戻す
        cmd.Blit(_tempRenderTargetHandle.Identifier(), source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        // Cameraのターゲットと同じDescription（Depthは無し）のRenderTextureを取得する
        /*var tempTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        tempTargetDescriptor.depthBufferBits = 0;
        tempTargetDescriptor.colorFormat = renderingData.cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        cmd.GetTemporaryRT(_tempRenderTargetHandle.id, tempTargetDescriptor);

        // VolumeからTintColorを取得して反映
        _material.SetColor(_tintColorPropertyId, _volume.tintColor.value);
        cmd.SetGlobalTexture(_mainTexPropertyId, source);

        //Debug.Log(RenderPassName);

        // 元のテクスチャから一時的なテクスチャにエフェクトを適用しつつ描画
        cmd.Blit(source, _tempRenderTargetHandle.Identifier(), _material);

        // 一時的なテクスチャから元のテクスチャに結果を書き戻す
        cmd.Blit(_tempRenderTargetHandle.Identifier(), source);

        // 一時的なRenderTextureを解放する
        cmd.ReleaseTemporaryRT(_tempRenderTargetHandle.id);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
        CommandBufferPool.Release(cmd);*/
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        // base.OnCameraCleanup(cmd);

        // 一時的なRenderTextureを解放する
        cmd.ReleaseTemporaryRT(_tempRenderTargetHandle.id);
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