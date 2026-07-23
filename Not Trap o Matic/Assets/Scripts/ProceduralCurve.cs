using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCurve<T> : MonoBehaviour
{
    public AnimationCurve curve;
    public float length;

    private float positon = 0.0f;
    private float minimalChangeThreshold = 0.8f;

    public ProceduralCurve(AnimationCurve curve)
    {
        this.curve = curve;
    }

    public struct TargetConfig<T>
    {
        public T min;
        public T max;
        public T defaultMin;
        public T snap;
    };

    public TargetConfig<T> targets = new TargetConfig<T>()
    {
        min = default,
        max = default,
        defaultMin = default,
        snap = default
    };

    private bool stopped = true;
    private bool playBackwards = false;

    public void Initialize(T min = default) 
    {
        if (min !=  null && min.Equals(default(T))) 
        {
            targets.min = min;
        }
        stopped = false;
        playBackwards = false;
        positon = 1.0f;
    }

    public void InitializeBackwards(T min = default)
    {
        if (min != null && min.Equals(default(T)))
        {
            targets.min = min;
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

    public void SetTargets(T min, T max, T snap = default)
    {
        targets.min = min;
        targets.defaultMin = min;
        targets.max = max;

        targets.snap = (snap == null || snap.Equals(default(T))) ? max : snap;
    }

    public T Step(float delta)
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
        T lerpedValue = default;

        if (typeof(T) == typeof(Vector3))
        {
            Vector3 minVec = (Vector3)(object)targets.min;
            Vector3 maxVec = (Vector3)(object)targets.max;

            Quaternion q1 = Quaternion.Euler(minVec);
            Quaternion q2 = Quaternion.Euler(maxVec);
            Vector3 rotationResult = Quaternion.Slerp(q1, q2, curveSample).eulerAngles;

            lerpedValue = (T)(object)rotationResult;
        }
        else if (typeof(T) == typeof(float))
        {
            float minFloat = (float)(object)targets.min;
            float maxFloat = (float)(object)targets.max;
            lerpedValue = (T)(object)Mathf.Lerp(minFloat, maxFloat, curveSample);
        }

        if ((positon >= 1.0 && !playBackwards) || (positon <= 0.0 && playBackwards))
        {
            positon = 0.0f;
            stopped = true;
            return !playBackwards ? targets.snap : targets.defaultMin;
        }

        if (curveSample > minimalChangeThreshold && targets.min.Equals(targets.defaultMin))
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
