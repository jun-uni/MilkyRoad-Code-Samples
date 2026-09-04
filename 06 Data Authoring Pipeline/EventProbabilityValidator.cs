#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// RandomEventEditor의 선택지 결과 확률 검증 로직
public static class EventProbabilityValidator
{
    private const float TargetTotal = 100f;
    private const float Tolerance = 0.01f;

    public static void Draw(SerializedProperty outcomes)
    {
        if (outcomes == null || outcomes.arraySize == 0)
        {
            EditorGUILayout.HelpBox("결과가 설정되지 않았습니다.", MessageType.Warning);
            return;
        }

        float totalProbability = 0f;

        for (int index = 0; index < outcomes.arraySize; index++)
        {
            SerializedProperty probability = outcomes
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("probability");
            totalProbability += probability.floatValue;
        }

        if (Math.Abs(totalProbability - TargetTotal) <= Tolerance)
            return;

        EditorGUILayout.HelpBox(
            $"확률의 합이 100%가 아닙니다. 현재 합계: {totalProbability:F1}%",
            MessageType.Warning);

        if (!GUILayout.Button("확률 균등 분배"))
            return;

        // 모든 결과에 동일한 확률 배분
        float equalProbability = TargetTotal / outcomes.arraySize;

        for (int index = 0; index < outcomes.arraySize; index++)
        {
            SerializedProperty probability = outcomes
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("probability");
            probability.floatValue = equalProbability;
        }
    }
}
#endif
