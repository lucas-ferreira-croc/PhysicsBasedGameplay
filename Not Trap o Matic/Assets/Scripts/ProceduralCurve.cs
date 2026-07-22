using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCurve : MonoBehaviour
{
    public AnimationCurve curve;
    public float length;

    private float positon = 0.0f;
    private float minimalChangeThreshold = 0.8f;


    private struct TargetConfig
    {
        public float min;
        public float max;
        public float defaultMin;
        public float snap;
    };

    private TargetConfig targets = new TargetConfig
    {
        min = 0.0f,
        max = 1.0f,
        defaultMin = 0.0f,
        snap = 0.0f
    };

    private bool stopped = true;
    private bool playBackwards = false;

    public void Initialize(float? min = null) 
    {
        if (min.HasValue) 
        {
            targets.min = min.Value;
        }
        stopped = false;
        playBackwards = false;
        positon = 1.0f;
    }

    public void InitializeBackwards(float? min)
    {
        if (min.HasValue)
        {
            targets.min = min.Value;
        }
        stopped = false;
        playBackwards = true;
        positon = 1.0f;
    }

    public bool IsRunning() 
    {
        return !stopped;
    }

    public void ForceStop() 
    {
        stopped = true;
        positon = 0.0f;
    }

    public void SetTargets(float min, float max, float? snap = null) 
    {
        targets.min = min;
        targets.defaultMin = min;
        targets.max = max;
        targets.snap = snap ?? max;
    }

    public float Step(float delta)
    {
        if (playBackwards)
        {
            positon -= Time.deltaTime / length;
        }
        else
        {
            positon += Time.deltaTime / length;
        }

        float curveSample = curve.Evaluate(positon);
        var lerpedValue = Mathf.Lerp(targets.min, targets.max, curveSample);

        if ((positon >= 1.0 && !playBackwards) || (positon <= 0.0 && playBackwards))
        {
            positon = 0.0f;
            stopped = true;
            return !playBackwards ? targets.snap : targets.defaultMin;
        }

        if (curveSample > minimalChangeThreshold && targets.min != targets.defaultMin)
        {
            targets.min = targets.defaultMin;
        }

        return lerpedValue;
    }


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }    
}
