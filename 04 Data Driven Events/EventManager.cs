using System.Collections.Generic;
using UnityEngine;

// 이벤트 순차 처리와 선택 결과 분배 담당
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    private EventPanelController eventPanelController;

    public EventPanelController EventPanelController
    {
        get => eventPanelController;
        set
        {
            eventPanelController = value;

            if (eventPanelController != null && !isProcessingEvent && pendingEvents.Count > 0)
                ProcessNextEvent();
        }
    }

    private readonly Queue<RandomEvent> pendingEvents = new();

    private EventResourceEffectHandler resourceEffectHandler;
    private EventCrewEffectHandler crewEffectHandler;
    private EventPlanetEffectHandler planetEffectHandler;
    private SpecialEffectHandlerFactory specialEffectHandlerFactory;

    private RandomEvent currentEvent;
    private bool isProcessingEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        resourceEffectHandler = new EventResourceEffectHandler();
        crewEffectHandler = new EventCrewEffectHandler();
        planetEffectHandler = new EventPlanetEffectHandler();
        specialEffectHandlerFactory = new SpecialEffectHandlerFactory();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void TriggerEvent(RandomEvent randomEvent)
    {
        if (randomEvent == null)
            return;

        pendingEvents.Enqueue(randomEvent);

        if (!isProcessingEvent)
            ProcessNextEvent();
    }

    private void ProcessNextEvent()
    {
        // UI 종료 신호마다 큐의 다음 이벤트 표시
        if (pendingEvents.Count == 0)
        {
            currentEvent = null;
            isProcessingEvent = false;
            return;
        }

        if (EventPanelController == null)
        {
            Debug.LogError("EventPanelController가 연결되지 않았습니다.");
            isProcessingEvent = false;
            return;
        }

        isProcessingEvent = true;
        currentEvent = pendingEvents.Dequeue();
        EventPanelController.ShowEvent(currentEvent);
    }

    public void ProcessChoice(RandomEvent randomEvent, int choiceIndex)
    {
        if (randomEvent == null ||
            randomEvent != currentEvent ||
            randomEvent.choices == null ||
            choiceIndex < 0 ||
            choiceIndex >= randomEvent.choices.Count)
            return;

        EventOutcome outcome = randomEvent.choices[choiceIndex].GetRandomOutcome();
        if (outcome == null)
            return;

        ApplyOutcomeEffects(outcome);
        EventPanelController?.ShowOutcome(outcome.outcomeText.Localize());
    }

    private void ApplyOutcomeEffects(EventOutcome outcome)
    {
        if (outcome.resourceEffects?.Count > 0)
            resourceEffectHandler.ApplyEffects(outcome.resourceEffects);

        if (outcome.crewEffects?.Count > 0)
            crewEffectHandler.ApplyEffects(outcome.crewEffects);

        if (outcome.planetEffects?.Count > 0)
            planetEffectHandler.ApplyEffects(outcome.planetEffects, currentEvent?.eventTitle ?? string.Empty);

        if (outcome.specialEffectType == SpecialEffectType.None)
            return;

        // 열거형에 대응하는 특수 효과 핸들러 선택
        ISpecialEffectHandler handler = specialEffectHandlerFactory.GetHandler(outcome.specialEffectType);
        handler?.HandleEffect(outcome);
    }

    public void EndEvent()
    {
        currentEvent = null;
        ProcessNextEvent();
    }

    public void ClearPendingEvents()
    {
        pendingEvents.Clear();
        currentEvent = null;
        isProcessingEvent = false;
    }
}
