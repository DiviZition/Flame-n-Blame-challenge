using System.Diagnostics;
using UnityEngine;
using Flame_n_Blame_Challenge.Adapters;

#region If UNITY_EDITOR => using UnityEditor
#if UNITY_EDITOR
using UnityEditor;
#endif
#endregion

public class BenchMark : MonoBehaviour
{
    public BurnableShell _shell;
    public Transform _transform;
    public float value;

    private void TestAction()
    {
        Vector3 posStart = _transform.position;
        Vector3 posShell = _shell.transform.position;
        Ray ray = new Ray(posStart, posShell - posStart);
        Physics.Raycast(ray, out RaycastHit hitInfo);
        _shell.ApplyStream(value, hitInfo.point);
    }

    #region Benchmark implementation
    [SerializeField][Range(1, 1_000)] private int _iterations = 1;

    public void RunBenchmark()
    {
        Stopwatch stopWatch = Stopwatch.StartNew();
        stopWatch.Start();
        int i;

        for (i = 0; i < _iterations; i++)
        {
            TestAction();

            if (stopWatch.ElapsedMilliseconds > 10000)
            {
                UnityEngine.Debug.Log("Breaking benchmark*");
                UnityEngine.Debug.Log("Escaping possibility of too long waiting time");
                break;
            }
        }

        stopWatch.Stop();

        UnityEngine.Debug.Log($"Benchmark result is: {i} iterations in {stopWatch.ElapsedMilliseconds} ms");
    }
    #endregion
}
#region Editor Button

#if UNITY_EDITOR
[CustomEditor(typeof(BenchMark))]
public class BenchMarkCustomEditor : Editor
{
    private BenchMark _benchmark;

    private void OnEnable()
    {
        _benchmark = (BenchMark)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Start benchmark"))
        {
            _benchmark.RunBenchmark();
        }
    }
}
#endif
#endregion