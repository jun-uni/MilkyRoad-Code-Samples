#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EventSystemTests
{
    [Test]
    public void EventWithRequiredCrew_IsRejectedWhenCrewIsMissing()
    {
        RandomEvent randomEvent = CreateEvent();
        randomEvent.requiredCrewRace.Add(CrewRace.Human);

        bool result = EventEligibility.Matches(
            randomEvent,
            year: 1,
            coma: 0,
            fuel: 0f,
            availableRaces: new HashSet<CrewRace>());

        Assert.That(result, Is.False);
        Object.DestroyImmediate(randomEvent);
    }

    [Test]
    public void EventWithMultipleRequiredCrew_RequiresEveryRace()
    {
        RandomEvent randomEvent = CreateEvent();
        randomEvent.requiredCrewRace.Add(CrewRace.Human);
        randomEvent.requiredCrewRace.Add(CrewRace.MechanicTank);

        bool result = EventEligibility.Matches(
            randomEvent,
            year: 1,
            coma: 0,
            fuel: 0f,
            availableRaces: new HashSet<CrewRace> { CrewRace.Human });

        Assert.That(result, Is.False);
        Object.DestroyImmediate(randomEvent);
    }

    [Test]
    public void WeightedOutcome_UsesTotalWeightInsteadOfFixedPercentage()
    {
        EventOutcome first = new() { probability = 1f };
        EventOutcome second = new() { probability = 3f };
        EventChoice choice = new()
        {
            possibleOutcomes = new List<EventOutcome> { first, second }
        };

        Assert.That(choice.GetOutcomeByNormalizedRoll(0.2f), Is.SameAs(first));
        Assert.That(choice.GetOutcomeByNormalizedRoll(0.8f), Is.SameAs(second));
    }

    private static RandomEvent CreateEvent()
    {
        return ScriptableObject.CreateInstance<RandomEvent>();
    }
}
#endif
