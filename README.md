# Milky Road — Portfolio Code Samples

Unity로 개발한 2D 우주 무역·RTS 전략 시뮬레이션 **Milky Road**에서 제가 설계·구현하고 개선한 핵심 게임 시스템을 정리한 채용 포트폴리오용 저장소입니다.

- 개발 기간: 2025.03–2025.06
- 개발 결과: 완성, 데모 출시
- 개발 형태: 기획 1명, 프로그래밍 3명, 아트 1명
- 담당: 함선 시스템, 워프 맵, 화물·거래, 랜덤 이벤트, 함선 전투, 데이터 제작 파이프라인
- 주요 기술: C#, Unity 6, ScriptableObject, Object Pooling, GitHub Actions, Python
- STOVE 데모: [Milky Road](https://store.onstove.com/ko/games/101477)
- 플레이 영상: [YouTube](https://www.youtube.com/watch?v=WO4OZRX400M)

## 빠르게 살펴보기

| 영역 | 주요 코드 | 확인할 내용 |
|---|---|---|
| 함선 외갑판 | [`코드 설명`](01%20Procedural%20Outer%20Hull/README.md) · [`배치 계산`](01%20Procedural%20Outer%20Hull/OuterHullPlacementCalculator.cs) · [`테스트`](01%20Procedural%20Outer%20Hull/OuterHullPlacementCalculatorTests.cs) | 방향 규칙 분리, 중복 제거, 결정적 변형 선택 |
| 워프 경로 | [`코드 설명`](02%20Procedural%20Warp%20Graph/README.md) · [`그래프 생성`](02%20Procedural%20Warp%20Graph/WarpGraphGenerator.cs) · [`Unity UI 적용`](02%20Procedural%20Warp%20Graph/EventTreeMap.cs) · [`테스트`](02%20Procedural%20Warp%20Graph/WarpGraphGeneratorTests.cs) | 계산과 표시 분리, 구간 기반 연결, 경로 분기·병합, 생성 결과 재현 |
| 화물 인벤토리 | [`코드 설명`](03%20Grid%20Cargo%20Inventory/README.md) · [`형태·회전`](03%20Grid%20Cargo%20Inventory/ItemShape.cs) · [`배치 검사`](03%20Grid%20Cargo%20Inventory/StorageRoomBase.cs) · [`드래그 프리뷰`](03%20Grid%20Cargo%20Inventory/TradingItemDragHandler.cs) | 상대 좌표 기반 회전, 경계·충돌 검사, 좌표 변환과 격자 스냅 |
| 랜덤 이벤트 | [`코드 설명`](04%20Data%20Driven%20Events/README.md) · [`이벤트 데이터베이스`](04%20Data%20Driven%20Events/EventDatabase.cs) · [`실행 흐름`](04%20Data%20Driven%20Events/EventManager.cs) · [`테스트`](04%20Data%20Driven%20Events/EventSystemTests.cs) | 중복 없는 이벤트 인덱싱, 복합 조건 필터, 가중치 결과 선택, 큐 기반 순차 처리 |
| 투사체 풀 | [`코드 설명`](05%20Projectile%20Object%20Pool/README.md) · [`풀 관리자`](05%20Projectile%20Object%20Pool/ProjectileManager.cs) · [`투사체 수명주기`](05%20Projectile%20Object%20Pool/MyProjectile.cs) | ID별 풀 구성, 동적 확장, 활성·반환 수명주기 관리 |
| 데이터 제작 파이프라인 | [`코드 설명`](06%20Data%20Authoring%20Pipeline/README.md) · [`자동 변환`](06%20Data%20Authoring%20Pipeline/DataPipeline.yml) · [`ID 기반 Importer`](06%20Data%20Authoring%20Pipeline/TradingItemDataImporter.cs) · [`확률 검증`](06%20Data%20Authoring%20Pipeline/EventProbabilityValidator.cs) | CSV·JSON 자동 변환, 기존 에셋 참조 유지, 이벤트 편집 단계 검증 |

## 주요 구현 사례

### 1. 방 배치에 맞춰 생성되는 함선 외갑판

함선에 배치된 방이 점유하는 타일을 수집하고, 각 타일의 인접 관계를 직선·외부 모서리·내부 모서리로 분류해 외갑판을 자동 생성했습니다. 배치 계산은 순수 클래스로 분리하고 방향 조합은 룰 테이블로 구성해 경계·중복·결정성을 테스트할 수 있습니다.

관련 코드: [`01 Procedural Outer Hull`](01%20Procedural%20Outer%20Hull)

### 2. 연결선 교차를 줄인 워프 경로 그래프

시작점과 도착점 사이에 레이어별 노드를 생성하고, 인접 레이어의 노드 수 비율로 연결 가능한 구간을 계산했습니다. 각 노드의 진입·진출 경로를 보장하면서 구간 안에서 분기와 병합을 구성하고, 그래프 계산을 Unity UI 표시와 분리해 연결성과 동일 seed의 재현 여부를 테스트합니다.

관련 코드: [`02 Procedural Warp Graph`](02%20Procedural%20Warp%20Graph)

### 3. 회전 가능한 불규칙 화물 인벤토리

화물의 기본 형태를 5×5 좌표계의 상대 좌표로 표현하고, 좌표를 90도씩 회전해 네 방향의 점유 형태를 생성했습니다. 실제 점유 타일을 기준으로 창고 경계와 기존 화물 충돌을 검사하고, 월드 좌표와 창고 로컬 그리드 좌표를 변환합니다. 드래그 중에는 마우스 아래의 창고를 찾아 불규칙한 점유 영역의 중심에 프리뷰를 맞춥니다.

관련 코드: [`03 Grid Cargo Inventory`](03%20Grid%20Cargo%20Inventory)

### 4. 조건에 따라 선택되는 데이터 기반 랜덤 이벤트

ScriptableObject 이벤트를 ID·이벤트 타입·결과 타입으로 인덱싱하고, 연도·COMA·연료·선원 종족 조건에 따라 후보를 필터링했습니다. 선택지는 유효한 가중치의 합을 기준으로 결과를 추첨합니다. 여러 이벤트가 발생하면 큐에서 순서대로 처리하고, 결과는 자원·선원·행성·특수 효과 처리 흐름으로 분배합니다.

관련 코드: [`04 Data Driven Events`](04%20Data%20Driven%20Events)

### 5. 투사체 데이터별 오브젝트 풀

투사체 ID별 큐를 미리 구성하고, 풀이 비었을 때 필요한 타입만 추가로 생성하도록 구현했습니다. 활성 투사체의 갱신과 반환을 `ProjectileManager`에서 관리하고, 잘못된 데이터는 초기화 단계에서 제외합니다. 투사체는 목표 도달 시 타격 콜백을 처리하고, 목표 도달 또는 수명 만료 후 자신을 생성한 풀로 복귀합니다.

관련 코드: [`05 Projectile Object Pool`](05%20Projectile%20Object%20Pool)

### 6. 코드 수정 없이 조정하는 데이터 제작 파이프라인

화물·장비·함선 무기·현지화 데이터를 CSV로 관리하고, GitHub Actions와 Python으로 Unity가 읽는 JSON을 자동 생성했습니다. Unity Editor Importer는 ID를 기준으로 기존 `ScriptableObject`를 갱신해 다른 오브젝트가 가진 참조를 유지하고, 신규 데이터만 에셋으로 생성합니다. 이벤트 제작용 커스텀 인스펙터에는 선택지 결과의 확률 합계 검사와 균등 분배, JSON 가져오기·내보내기를 구성했습니다.

관련 코드: [`06 Data Authoring Pipeline`](06%20Data%20Authoring%20Pipeline)

## 디렉터리 구성

```text
01 Procedural Outer Hull/   # 방 배치 기반 함선 외갑판 자동 생성
02 Procedural Warp Graph/   # 레이어 기반 워프 경로 그래프
03 Grid Cargo Inventory/    # 회전 가능한 격자형 화물 인벤토리
04 Data Driven Events/      # 조건 필터와 큐 기반 랜덤 이벤트
05 Projectile Object Pool/  # 투사체 데이터별 오브젝트 풀
06 Data Authoring Pipeline/  # CSV·JSON·ScriptableObject 데이터 제작 흐름
```

## 공개 및 저작권

이 저장소는 팀 프로젝트 **Milky Road**의 채용 포트폴리오용 코드 모음입니다.

- 제가 직접 구현하거나 주요하게 기여한 C# 코드를 정리했습니다.
- 이 저장소에는 별도의 오픈소스 라이선스를 부여하지 않습니다.
