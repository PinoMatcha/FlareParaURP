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

        // �}�e���A�����쐬
        // _material = CoreUtils.CreateEngineMaterial(shader);
        _material = material;

        // RenderPassEvent.AfterRendering�ł̓|�X�g�G�t�F�N�g���|������̃J���[�e�N�X�`�������̖��O�Ŏ擾�ł���
        _afterPostProcessTexture.Init("_AfterPostProcessTexture");
    }

    public void Setup(RenderTargetIdentifier cameraColorTarget, PostprocessTiming timing) {
        _cameraColorTarget = cameraColorTarget;

        renderPassEvent = GetRenderPassEvent(timing);

        // Volume�R���|�[�l���g���擾
        var volumeStack = VolumeManager.instance.stack;
        _volume = volumeStack.GetComponent<FlareParaVolume>();
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_material == null) {
            return;
        }

        // �J�����̃|�X�g�v���Z�X�ݒ肪�����ɂȂ��Ă����牽�����Ȃ�
        if (!renderingData.cameraData.postProcessEnabled) {
            return;
        }

        // �J�������V�[���r���[�J�������V�[���r���[�ɓK�p���Ȃ��ꍇ�ɂ͉������Ȃ�
        if (!_applyToSceneView && renderingData.cameraData.cameraType == CameraType.SceneView) {
            return;
        }

        if (!_volume.IsActive()) {
            return;
        }

        // renderPassEvent��AfterRendering�̏ꍇ�A�J�����̃J���[�^�[�Q�b�g�ł͂Ȃ�_AfterPostProcessTexture���g��
        /*var source = renderPassEvent == RenderPassEvent.AfterRendering && renderingData.cameraData.resolveFinalTarget
            ? _afterPostProcessTexture.Identifier()
            : _cameraColorTarget;*/

        var renderer = renderingData.cameraData.renderer;
        var source = renderer.cameraColorTarget;

        // �R�}���h�o�b�t�@���쐬
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

        // ���̃e�N�X�`������ꎞ�I�ȃe�N�X�`���ɃG�t�F�N�g��K�p���`��
        cmd.Blit(source, _tempRenderTargetHandle.Identifier(), _material);

        // �ꎞ�I�ȃe�N�X�`�����猳�̃e�N�X�`���Ɍ��ʂ������߂�
        cmd.Blit(_tempRenderTargetHandle.Identifier(), source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        // Camera�̃^�[�Q�b�g�Ɠ���Description�iDepth�͖����j��RenderTexture���擾����
        /*var tempTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        tempTargetDescriptor.depthBufferBits = 0;
        tempTargetDescriptor.colorFormat = renderingData.cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        cmd.GetTemporaryRT(_tempRenderTargetHandle.id, tempTargetDescriptor);

        // Volume����TintColor���擾���Ĕ��f
        _material.SetColor(_tintColorPropertyId, _volume.tintColor.value);
        cmd.SetGlobalTexture(_mainTexPropertyId, source);

        //Debug.Log(RenderPassName);

        // ���̃e�N�X�`������ꎞ�I�ȃe�N�X�`���ɃG�t�F�N�g��K�p���`��
        cmd.Blit(source, _tempRenderTargetHandle.Identifier(), _material);

        // �ꎞ�I�ȃe�N�X�`�����猳�̃e�N�X�`���Ɍ��ʂ������߂�
        cmd.Blit(_tempRenderTargetHandle.Identifier(), source);

        // �ꎞ�I��RenderTexture���������
        cmd.ReleaseTemporaryRT(_tempRenderTargetHandle.id);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
        CommandBufferPool.Release(cmd);*/
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        // base.OnCameraCleanup(cmd);

        // �ꎞ�I��RenderTexture���������
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