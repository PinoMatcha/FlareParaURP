using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[VolumeComponentMenu("Flare & Para")]
public class FlareParaVolume : VolumeComponent
{

    public BoolParameter isActive = new BoolParameter(true);

    public bool IsActive() => isActive.value;

    public ColorParameter flareColor = new ColorParameter(Color.black);
    public FloatParameter flarePower = new FloatParameter(1f);
    public FloatParameter flareIntensity = new FloatParameter(1f);
    public FloatParameter flareStrength = new FloatParameter(1f);

    public ColorParameter paraColor = new ColorParameter(Color.white);
    public FloatParameter paraPower = new FloatParameter(1f);
    public FloatParameter paraIntensity = new FloatParameter(1f);
    public FloatParameter paraStrength = new FloatParameter(1f);

    public FloatParameter degree = new FloatParameter(0f);

}
