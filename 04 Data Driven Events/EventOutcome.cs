using System;
using System.Collections.Generic;

[Serializable]
public class EventOutcome
{
    public string outcomeText;
    public float probability;

    public List<PlanetEffect> planetEffects = new();
    public List<ResourceEffect> resourceEffects = new();
    public List<CrewEffect> crewEffects = new();

    public SpecialEffectType specialEffectType = SpecialEffectType.None;

    // 특수 효과 핸들러가 해석하는 대상 식별자와 수치
    public string specialEffectValue;
    public float specialEffectAmount;

    public RandomEvent nextEvent;
    public RandomQuest questToAdd;
}
