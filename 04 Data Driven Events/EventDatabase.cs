using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "Event/Event Database")]
public class EventDatabase : ScriptableObject
{
    [SerializeField] private List<RandomEvent> allEvents = new();

    private Dictionary<int, RandomEvent> eventsById;
    private Dictionary<EventType, List<RandomEvent>> eventsByType;
    private Dictionary<EventOutcomeType, List<RandomEvent>> eventsByOutcomeType;
    private IReadOnlyList<RandomEvent> indexedEvents;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        // 중복 ID를 제외한 공통 이벤트 집합 구성
        eventsById = new Dictionary<int, RandomEvent>();
        eventsByType = CreateIndex<EventType>();
        eventsByOutcomeType = CreateIndex<EventOutcomeType>();
        List<RandomEvent> uniqueEvents = new();

        foreach (RandomEvent randomEvent in allEvents)
        {
            if (randomEvent == null)
                continue;

            if (!eventsById.TryAdd(randomEvent.eventId, randomEvent))
            {
                Debug.LogWarning($"중복된 이벤트 ID: {randomEvent.eventId}");
                continue;
            }

            uniqueEvents.Add(randomEvent);
            eventsByType[randomEvent.eventType].Add(randomEvent);
            eventsByOutcomeType[randomEvent.outcomeType].Add(randomEvent);
        }

        indexedEvents = uniqueEvents.AsReadOnly();
    }

    public IReadOnlyList<RandomEvent> GetAllEvents()
    {
        EnsureInitialized();
        return indexedEvents;
    }

    public RandomEvent GetEvent(int eventId)
    {
        EnsureInitialized();
        eventsById.TryGetValue(eventId, out RandomEvent randomEvent);
        return randomEvent;
    }

    public IReadOnlyList<RandomEvent> GetEventsByType(EventType type)
    {
        EnsureInitialized();
        return eventsByType.TryGetValue(type, out List<RandomEvent> events)
            ? events.AsReadOnly()
            : Array.Empty<RandomEvent>();
    }

    public IReadOnlyList<RandomEvent> GetEventsByOutcomeType(EventOutcomeType outcomeType)
    {
        EnsureInitialized();
        return eventsByOutcomeType.TryGetValue(outcomeType, out List<RandomEvent> events)
            ? events.AsReadOnly()
            : Array.Empty<RandomEvent>();
    }

    public List<RandomEvent> GetFilteredEvents(
        EventType type,
        int year,
        int coma,
        float fuel,
        IReadOnlyCollection<CrewRace> availableRaces)
    {
        EnsureInitialized();

        if (!eventsByType.TryGetValue(type, out List<RandomEvent> typedEvents))
            return new List<RandomEvent>();

        HashSet<CrewRace> raceLookup = availableRaces != null
            ? new HashSet<CrewRace>(availableRaces)
            : new HashSet<CrewRace>();

        // 이벤트 타입으로 후보를 좁힌 뒤 복합 조건 검사
        return typedEvents.Where(randomEvent =>
            EventEligibility.Matches(randomEvent, year, coma, fuel, raceLookup)
        ).ToList();
    }

    public RandomEvent GetRandomEvent(
        EventType type,
        int year,
        int coma,
        float fuel,
        IReadOnlyCollection<CrewRace> availableRaces)
    {
        List<RandomEvent> candidates = GetFilteredEvents(type, year, coma, fuel, availableRaces);
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private static Dictionary<T, List<RandomEvent>> CreateIndex<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(value => value, _ => new List<RandomEvent>());
    }

    private void EnsureInitialized()
    {
        if (eventsById == null ||
            eventsByType == null ||
            eventsByOutcomeType == null ||
            indexedEvents == null)
            Initialize();
    }
}
