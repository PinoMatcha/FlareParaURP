using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[VolumeComponentMenu("Post-processing/PM Presents/Flare & Para")]
public class FlareParaVolume : VolumeComponent // VolumeComponentを継承する
{

    public bool IsActive() => flareColor != Color.black || paraColor != Color.white;

    public ColorParameter flareColor = new ColorParameter(Color.black);
    public FloatParameter flarePower = new FloatParameter(5.0f);
    public FloatParameter flareIntensity = new FloatParameter(1.0f);
    public FloatParameter flareStrength = new FloatParameter(1.0f);

    public ColorParameter paraColor = new ColorParameter(Color.white);
    public FloatParameter paraPower = new FloatParameter(6.0f);
    public FloatParameter paraIntensity = new FloatParameter(1.0f);
    public FloatParameter paraStrength = new FloatParameter(1.0f);

    public FloatParameter degree = new FloatParameter(0.0f);

}