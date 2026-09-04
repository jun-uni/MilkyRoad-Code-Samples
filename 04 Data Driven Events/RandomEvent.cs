using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum EventType
{
    Ship,
    Planet,
    Mystery
}

public enum EventOutcomeType
{
    Positive,
    Neutral,
    Negative
}

[CreateAssetMenu(fileName = "New Event", menuName = "Event/Random Event")]
public class RandomEvent : ScriptableObject
{
    public int eventId;
    public string debugName;
    public string eventTitle;
    public string eventDescription;
    public Sprite eventImage;

    public EventType eventType;
    public EventOutcomeType outcomeType;

    // 이벤트 발생 조건
    public int minimumYear;
    public int minimumCOMA;
    public float minimumFuel;
    public List<CrewRace> requiredCrewRace = new();

    public List<EventChoice> choices = new();
}
