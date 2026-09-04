using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[Serializable]
public class EventChoice
{
    public string choiceText;
    public List<EventOutcome> possibleOutcomes = new();

    public EventOutcome GetRandomOutcome()
    {
        return GetOutcomeByNormalizedRoll(Random.value);
    }

    public EventOutcome GetOutcomeByNormalizedRoll(float normalizedRoll)
    {
        float totalWeight = 0f;

        foreach (EventOutcome outcome in possibleOutcomes)
        {
            if (outcome != null && outcome.probability > 0f)
                totalWeight += outcome.probability;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Mathf.Clamp01(normalizedRoll) * totalWeight;
        float accumulatedWeight = 0f;
        EventOutcome lastValidOutcome = null;

        foreach (EventOutcome outcome in possibleOutcomes)
        {
            if (outcome == null || outcome.probability <= 0f)
                continue;

            lastValidOutcome = outcome;
            accumulatedWeight += outcome.probability;

            if (roll <= accumulatedWeight)
                return outcome;
        }

        // 부동소수점 오차에 대한 마지막 유효 결과 반환
        return lastValidOutcome;
    }
}
