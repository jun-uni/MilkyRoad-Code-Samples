using System.Collections.Generic;
using System.Linq;

public static class EventEligibility
{
    public static bool Matches(
        RandomEvent randomEvent,
        int year,
        int coma,
        float fuel,
        ISet<CrewRace> availableRaces)
    {
        // 수치 조건과 필수 선원 종족 전체 충족 여부 검사
        if (randomEvent == null ||
            randomEvent.minimumYear > year ||
            randomEvent.minimumCOMA > coma ||
            randomEvent.minimumFuel > fuel)
            return false;

        return randomEvent.requiredCrewRace == null ||
               randomEvent.requiredCrewRace.Count == 0 ||
               availableRaces != null && randomEvent.requiredCrewRace.All(availableRaces.Contains);
    }
}
