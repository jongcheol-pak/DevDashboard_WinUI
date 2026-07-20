# plan.md — 새 작업(TaskEditDialog) 다이얼로그 시안 정합 재구성

**PRD**: docs/prd.md (FR-T5 시각 재구성 — 기능 요구는 이미 충족, **시각 구조 + 기본값 1건만 변경**)
**이전 plan**: 작업(TaskPage) 칸반 화면 시안 정합 재구성 — Phase G 통과(Must 100%), master 병합 완료(HEAD b7f3a48).
**다음 plan**: 없음.

## 요구 이해

> 원문(사용자, 2026-07-20): "[이미지] 새 작업 화면 이미지처럼 수정" + 첨부 시안 이미지 1장(새 작업 다이얼로그).

이해한 요구:
- 첨부 이미지가 **새 작업 다이얼로그의 디자인 기준**이고, 현재 `TaskEditDialog`가 그 구조와 어긋나 있으니 **시안과 같은 구조로 재구성**한다.
- 어긋난 이유는 이전 plan과 동일 — Phase 2 구현 당시 목업이 레포에 없어 PRD 텍스트만으로 구현했다. **기능 결함이 아니라 시각 구조 불일치**다.
- 따라서 이번 작업도 **기능 추가가 아니라 시각 정합 재구성**이다 — 편집/신규 양쪽 동작, "테스트 목록에도 추가"(FR-T6), 카테고리·우선순위·날짜 입력은 전부 보존한다.
- 사용자 확정: 헤더 상태 pill은 **실제 상태**를 표시(생성자에 상태 전달), 제목 필수 강조는 **비어 있으면 상시**, 버튼은 **새 작업="등록"/편집="저장"**, 기본값 변경은 **시작일=오늘 1건만**(카테고리·테스트추가 토글 기본값은 현행 유지).

## Goal

`TaskEditDialog`를 시안과 동일한 구조로 재구성한다 — ① 헤더를 `제목 + 상태 pill` 구성으로 바꾸고 pill이 **실제 상태**를 표시 ② 입력 라벨을 굵은 본문체에서 **작고 저강도**(`InputLabelStyle`)로 바꾸고 제목 라벨에 빨간 필수표시 `*` ③ 제목 입력칸이 비어 있으면 상시 danger 테두리(하단 에러 문구는 제거) ④ placeholder 문구를 시안 문구로 교체 ⑤ "테스트 추가"를 `SettingsCard`(제목+부제+우측 토글)로 카드화 ⑥ 하단 버튼을 새 작업="등록"/편집="저장" ⑦ 새 작업의 시작일 기본값=오늘. 기능 동작은 무손실.

## Investigation Log (근거)

### 위키·시안
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- 시안: 사용자 제공 이미지 1장(새 작업 다이얼로그). 목업 HTML은 레포에 없어 **픽셀 값이 아닌 구조·구성 요소 기준**으로 대조한다(아래 `## 시각 요소 분해`).
- AGENTS.md 존재(2026-07-20 생성) — 빌드 명령·함정 11건·컨벤션 확인 완료. 신선도 점검: 이번 계획이 참조하는 경로(`Resources/Styles.xaml`·`Palette.xaml`·`Strings/{ko-KR,en-US}`·`Presentation/Views/Dialogs`) 전부 실재 확인. 어긋남 0건.

### Deferred 대장 확인 (docs/plans/deferred.md)
- **재수용**: `[미사용 심볼 정리]`의 잔존 항목 — `TaskEditDialog.xaml`의 `x:Name="TitleBox"` 미사용 건. 이번 **T3**가 정확히 그 요소(제목 TextBox)를 재작성하므로 **이번에 해소**한다(x:Bind 유효성 배선이 `x:Name` 참조를 대체하거나, 남으면 제거).
- **이번에도 유지**: `[FR-S5 프로젝트 설정 다이얼로그 restyle]`(다른 다이얼로그), `[Todo*/Test* resw 고아 정리]`, `[TestDateGroup·HistoryEntryDialogViewModel 고아]`, `[BOM 통일]`, `[NU1903]`, `[README/스크린샷]`, `[목록 뷰·TestPage·NotificationPage 시안 대조]` 등 — 이번 작업과 무관.
- **신규 등재 예정**: 아래 Deferred 절 참조(시작일 기본값 도입에 따른 "날짜 지우기 수단 부재").

### 영향 범위 — 전수 확인 (직접 grep + Read)
- **`TaskEditDialog` 생성자 호출부는 전 레포 2곳뿐**(grep 전수): `TaskPage.xaml.cs:184`(`ColumnAdd_Click` — 새 작업), `TaskPage.xaml.cs:199`(`EditTodoAsync` — 편집, 목록 뷰 버튼·칸반 카드 클릭·우클릭 메뉴 공용). 그 외 호출자·구현체·직렬화 대상 **0건**.
- `ColumnAdd_Click`은 **이미 `TodoStatus status`를 파싱해 갖고 있다**(`:182` `Enum.TryParse<TodoStatus>(statusTag, out var status)`) → 상태를 생성자에 넘기는 데 신규 조회가 불필요하다.
- `TaskEditDialogViewModel` 참조처는 `TaskEditDialog.xaml(.cs)` 1곳뿐(grep 확인).
- `TodoItem` **무변경** — 도메인·DB 스키마·직렬화 변경 없음. 마이그레이션 불요.
- `Dialog_Save`("저장")는 **다이얼로그 8종·코드 사이트 9곳이 공유**(grep: TestPage 2건·TestEditDialog·HistoryDialog·TaskEditDialog·GroupDialog·ProjectSettingsDialog·CommandScriptDialog·ProjectHistoryDialog) → **값을 "등록"으로 바꾸면 전 다이얼로그가 오염**된다. TaskEdit 전용 신규 키를 만든다(D5).

### 재사용 자산 (직접 Read)
- **`InputLabelStyle`(Styles.xaml:210)** — `FontSize 12` + `TextFillColorSecondaryBrush` + `Margin 0,0,0,4`. **시안의 작고 저강도 라벨과 정확히 일치**하는데 **현재 소비처가 0**이다(grep: Styles.xaml 정의부에만 존재). 신규 스타일 없이 이 스타일을 소비하면 된다.
- **`toolkit:SettingsCard`** — `AppSettingsDialog.xaml:87-96` 등 4곳이 이미 `x:Uid`(`.Header`/`.Description`) + 우측 `ToggleSwitch(OnContent="" OffContent="" MinWidth="0")` 패턴으로 사용 중. **시안의 "테스트 추가" 카드(제목+부제+우측 토글)와 구조가 동일** → 신규 카드 스타일 불필요, `HeaderIcon`만 생략하면 시안과 일치.
- `TagBadgeStyle`(Styles.xaml:189, CornerRadius 4 / Padding 8,3) — 헤더 상태 pill의 기반으로 재사용.
- `AppMutedSoftBrush`(#286F6D75) + `AppTextTertiaryBrush` — 시안의 저강도 회색 pill 배색. 칸반 우선순위 배지 "낮음"에서 쓴 조합과 동일(이전 plan Progress Log).
- 상태 라벨: `TaskPage.xaml.cs:50-53`이 `LocalizationService.Get("TaskStatus_{Waiting|Active|Completed|Hold}")`를 static으로 캐시하는 선례. resw 키 4종 **전부 기존 존재**(ko: 예정/진행 중/완료/보류, en: To Do/…).
- `LocalizationService.Get` + `x:Bind` 정적/VM 프로퍼티 소비가 이 화면군의 관례(`TaskPage.xaml.cs:23-26`).

### 기술 제약 (확인 완료)
- **`AppDangerColor`(#FFF0716A) == `AppAccentColor`(#FFF0716A)** — 이 레포에서 danger와 accent는 **같은 살몬색**이다(`Palette.xaml:31,39` 직접 Read). 시안의 "빨간 테두리"는 이 살몬 danger 색이다.
  - 이것이 제목 TextBox 강조 구현의 성립 근거다: WinUI `TextBox`의 로컬 `BorderBrush`는 **Normal 상태에서만** 유효하고 `PointerOver`/`Focused`에서는 `TextControlBorderBrush*` ThemeResource가 덮어쓴다. 그런데 `Focused` 기본값은 accent(=`AccentFillColorDefaultBrush` `Palette.xaml:82`, `AccentControlElevationBorderBrush` `:85` — **둘 다** #F0716A) → **포커스 시에도 같은 살몬**이라 시각이 어긋나지 않는다. 따라서 템플릿 복사 없이 로컬 `BorderBrush` 바인딩으로 충분하다(D3).
  - `Palette.xaml`에 `TextControlBorderBrushFocused` 오버라이드는 **없다**(`:149-150`에 `TextControlBorderBrush`/`PointerOver`만) → WinUI 기본(accent)이 적용된다.
  - ⚠️ **단 `PointerOver`는 예외다**(plan-reviewer M1): `TextControlBorderBrushPointerOver`가 `AppBorderStrongerColor`(**#FF35353C 회색**)로 오버라이드돼 있다(`:150`). 즉 빈 제목 위에 **마우스를 올리는 동안만** 살몬 테두리가 회색으로 바뀌고, 떼면 즉시 돌아온다. accent==danger 논리는 `Focused`에만 적용되고 `PointerOver`에는 적용되지 않는다 → D3에서 별도 결정으로 처리.
- **`LocalizationService.Get`은 키를 찾지 못하면 키 문자열 자체를 반환한다**(`LocalizationService.cs:56,60` — plan-reviewer 확인). 따라서 소비 중인 resw 키를 먼저 지우면 화면에 `TaskEdit_TitleRequired` 같은 **raw 키가 그대로 표시**된다. "미소비 키는 무해"가 성립하려면 **참조를 끊은 뒤에 지워야** 한다(T3에서 제거하는 이유).
- **`x:Bind` 함수 바인딩은 Converter·ThemeResource를 받지 못한다**(AGENTS.md 함정 5) → 유효성 테두리는 코드비하인드 정적 헬퍼가 `Brush`를 **직접 반환**한다(`TaskPage.PriorityBrush`·`TestPage.StatusBrush` 선례).
- **`ContentDialog`는 `FrameworkElement`다**(ContentControl 파생) → AGENTS.md 함정 9(`Window`에서 `x:Bind`+Converter가 CS1503)는 **이 파일에 해당하지 않는다**. 현행 `TaskEditDialog.xaml:75`가 이미 `{x:Bind Vm.ShowTestToggle, Converter=...}`로 정상 빌드되는 것이 증거.
- **`ContentDialog.Title`은 `object`** → XAML `<ContentDialog.Title>`에 커스텀 콘텐츠(StackPanel)를 넣을 수 있다. 단 **코드비하인드의 `Title = LocalizationService.Get(...)`(TaskEditDialog.xaml.cs:24-26)이 XAML Title을 런타임에 덮어쓰므로 반드시 함께 제거**한다(D2).
- **`CalendarDatePicker`는 선택한 날짜를 UI로 지울 수단이 없다** — 시작일 기본값을 오늘로 채우면 "시작일 없는 작업"을 만들 방법이 사라진다(D6 Edge Case → Deferred 등재).

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| 입력 라벨 스타일 | `InputLabelStyle`(Styles.xaml:210) | **재사용** — 시안 값(12px·저강도)과 정확히 일치. 현재 소비처 0이라 신규로 착각하기 쉬우나 이미 존재한다. |
| "테스트 추가" 카드 | `toolkit:SettingsCard`(AppSettingsDialog.xaml:87 등 4곳) | **재사용** — 제목+부제+우측 컨트롤 구조가 시안과 동일. 카드 Border 스타일 신규 작성 불필요. |
| 헤더 상태 pill | `TagBadgeStyle`(Styles.xaml:189) + `AppMutedSoftBrush` | **재사용** — 칸반 우선순위 배지와 같은 조합. |
| `TitleBorderBrush(string)` 정적 헬퍼 | `TaskPage.PriorityBrush`·`TestPage.StatusBrush` | **신규(패턴 재사용)** — 대상 화면이 달라 공용화 불가. x:Bind 함수 바인딩이 Brush를 직접 반환하는 기존 패턴을 따른다. |
| `HeaderTitle`/`StatusLabel`/`PrimaryButtonLabel` VM 프로퍼티 | 없음 | **신규** — 다이얼로그 모드·상태에 따른 표시 문자열. 기존 VM에 상태 개념 자체가 없었다. |
| 상태→라벨 매핑 | `TaskPage.xaml.cs:50-53`·`StatusOptions`(:56-62) | **신규(계산식 재사용)** — 같은 resw 키를 쓰되 `TaskPage`의 static 캐시는 그 파일 전용이라 참조하지 않는다(다이얼로그가 페이지에 의존하면 방향이 역전). 2회 미만 중복이라 공용 추출 안 함. |

## 시각 요소 분해

> 출처: 사용자 제공 시안 이미지. 목업 HTML이 없어 **px 값은 확정 불가** — 구조·구성 요소·상대 관계를 명세하고, 수치는 기존 관례(`InputLabelStyle`·`SettingsCard`·`TagBadgeStyle` 현행 값)를 따른다. 확인은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY).

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|---|---|---|---|
| 헤더 | 구성 | `제목(굵게)` + **제목 오른쪽에 인접한** 상태 pill, 한 줄 가로 배치(다이얼로그 우측 끝 정렬 아님) | 시안 상단 |
| 헤더 제목 | 문구 | 새 작업="새 작업" / 편집="작업 편집"(기존 키 재사용) | 시안 + resw 기존값 |
| 상태 pill | 형태·색 | 라운드 배지, 저채도 회색 배경 + 저강도 회색 텍스트, 작은 글자 | 시안 헤더 |
| 상태 pill | 내용 | 실제 상태 문구("예정"/"진행 중"/"완료"/"보류") | 사용자 결정 2026-07-20 |
| 입력 라벨 | 스타일 | **작고 저강도**(굵은 본문체 아님), 입력칸 바로 위 | 시안 전 라벨 |
| 제목 라벨 | 구성 | `제목` + 빨간 `*` 필수표시 | 시안 |
| 제목 입력칸 | 테두리 | 비어 있으면 danger(살몬) 테두리 | 시안(빈 상태에서 빨간 테두리) |
| 제목 입력칸 | placeholder | `무엇을 해야 하나요?` | 시안 |
| 설명 입력칸 | placeholder | `작업 내용 설명 (선택)` | 시안 |
| 설명 입력칸 | 형태 | 여러 줄, 높이가 제목칸보다 큼 | 시안 |
| 카테고리·우선순위 | 배치 | 2열 등폭, 각 라벨 + 콤보 | 시안 (현행 유지) |
| 시작일·종료일 | 배치 | 2열 등폭, 각 라벨 + 날짜 입력 | 시안 (현행 유지) |
| 시작일 | 초기값 | 오늘 날짜 | 시안 + 사용자 결정 |
| 종료일 | placeholder | `연도-월-일` | 시안 |
| 테스트 추가 | 컨테이너 | 배경+1px 테두리+라운드 **카드**로 감쌈 | 시안 |
| 테스트 추가 | 구성 | 제목("테스트 추가") + 부제("이 작업을 테스트 목록에도 추가합니다.") + **우측** 토글 | 시안 |
| 테스트 추가 토글 | 색·라벨 | On=살몬(accent), 켜짐/꺼짐 문자 라벨 없음 | 시안 |
| 하단 버튼 | 문구 | 좌="등록"(새 작업)/"저장"(편집), 우="취소" | 시안 + 사용자 결정 |
| 하단 에러 문구 | 존재 | **없음**(테두리 강조로 대체) | 시안에 부재 + 사용자 결정 |

### V-9 결과 (T3 수행) — 구조 ✅ / 렌더 ⏳ F-8 인계

- **구조 대조 ✅ (❌ 0건)**: 위 표의 **전 행이 diff에 실재**함을 T3 spec 리뷰가 행별로 지목해 확인했다(헤더 `<ContentDialog.Title>` StackPanel, `TagBadgeStyle`+`AppMutedSoftBrush` pill, `InputLabelStyle` 7회, 빨간 `*`, `TitleBorderBrush` ARGB가 Palette 값과 일치, `SettingsCard`+`x:Uid` 제목/부제, `OnContent=""` 라벨 없는 토글, `PrimaryButtonLabel` 등록/저장, `ErrorText` 제거).
- **⏳ 미확인 — F-8 인계 목록 (렌더 육안 확인 필요)**: 빌드는 마크업 존재만 보증하고 "시안과 같아 보이는가"는 판정할 수 없다. 데스크톱 UI라 캡처 도구가 없어 아래는 **사용자 육안 대조로만 확정**된다.
  1. 헤더에서 pill이 제목 오른쪽에 자연스럽게 인접하는가(제목이 길 때 밀리지 않는가)
  2. 상태 pill의 배색·크기·**글자 굵기**가 시안과 같은 인상인가 — ⚠️ `ContentDialog` 기본 title presenter가 `FontWeight=SemiBold`를 걸고 폰트 속성은 시각 트리로 **상속**된다. 헤더 제목이 굵게 나오는 근거가 그 상속인데 **같은 상속이 pill 텍스트에도 걸려** 저강도 배지가 굵게 나올 수 있다(제목이 굵으면 pill도 굵은 배타 관계). 어긋나면 pill `TextBlock`에 `FontWeight="Normal"` 1줄 추가. (F-7 m1)
  3. `InputLabelStyle` 라벨 크기·색·입력칸과의 간격이 시안과 맞는가
  4. 빈 제목의 danger 테두리가 시안처럼 보이는가(+ hover 시 회색 전환이 D3-a 수용 범위로 느껴지는가)
  5. `SettingsCard` 배경이 다이얼로그 배경과 충분히 대비되는가(약하면 `Background`를 `AppCardAltBrush`로)
  6. 토글 On 색이 실제로 살몬으로 렌더되는가(전역 accent 오버라이드 경유라 코드상으론 확실하나 렌더 확인 필요)
  7. 전체 여백·다이얼로그 폭이 시안과 같은 밀도인가

## Decisions

| # | 항목 | 카테고리 | 결정 | Source |
|---|---|---|---|---|
| D1 | 헤더 상태 pill | UX·API | **실제 상태 표시** — `TaskEditDialog` 생성자에 `TodoStatus`를 추가해 새 작업은 눌린 열의 상태를, 편집은 기존 작업의 상태를 표시. ⚠️ 시그니처 변경(사전 승인 항목) | 사용자 결정 2026-07-20 |
| D2 | 헤더 구현 | 기술 | XAML `<ContentDialog.Title>`에 커스텀 콘텐츠. **코드비하인드의 `Title = ...` 할당(`:24-26`)을 반드시 제거** — 남기면 XAML Title을 런타임에 덮어써 pill이 사라진다 | `ContentDialog.Title`은 object + 현행 코드 Read |
| D3 | 제목 필수 강조 | UX | **비어 있으면 상시 danger 테두리**, 입력 시 즉시 정상 복귀. 하단 에러 문구(`ErrorText`)는 **제거**(중복). 구현은 로컬 `BorderBrush`를 x:Bind 함수 바인딩 — accent==danger 동색이라 포커스 상태에서도 어긋나지 않음 | 사용자 결정 2026-07-20 + Palette.xaml:31,39 |
| D3-a | hover 시 테두리 회색 전환 | UX | **허용한다(수용)** — 빈 제목 위에 마우스를 올리는 동안만 회색으로 바뀌고 떼면 즉시 살몬 복귀. 근거: ① **라벨의 빨간 `*`가 상시 표시**되어 hover 중에도 필수 신호가 사라지지 않는다(정보 손실 0) ② 시안은 정적 이미지라 hover 상태를 정의하지 않는다 ③ 회색 hover는 다른 입력칸과 동일한 WinUI 표준 피드백이라 오히려 일관적이다. **기각한 대안**: TextBox를 `Border`로 감싸면(테두리 완전 제어) focus 표시가 사라져 다른 입력칸과 불일치하고, 로컬 `Resources` 오버라이드는 정적이라 **제목이 채워진 뒤에도** hover가 빨개지는 부작용이 있다 | plan-reviewer M1 + Palette.xaml:150 |
| D4 | 저장 시 검증 | 로직 | `OnSave`의 **빈 제목 차단(`args.Cancel = true`)은 유지**한다 — 테두리는 시각 표시일 뿐 저장을 막지 못하므로, 제거하면 빈 제목 작업이 생성된다(회귀) | 현행 `TaskEditDialog.xaml.cs:39-45` |
| D5 | 버튼 문구 | 위치 | 새 작업="등록"(신규 키 `TaskEdit_Submit`), 편집="저장"(기존 공용 `Dialog_Save` 재사용). **`Dialog_Save` 값 자체는 건드리지 않는다** — 8개 다이얼로그 공용 | 사용자 결정 + grep 8건 |
| D6 | 기본값 변경 | 로직 | **시작일=오늘 1건만.** 카테고리 기본(미분류)·테스트추가 토글 기본(꺼짐)은 **현행 유지** | 사용자 결정 2026-07-20 |
| D7 | 라벨 스타일 | 재사용 | 기존 `InputLabelStyle` 소비(신규 스타일 0). `BodyStrongTextBlockStyle`에서 교체 | Styles.xaml:210 + 소비처 0 grep |
| D8 | 테스트 추가 카드 | 재사용 | `toolkit:SettingsCard` 사용(`HeaderIcon` 없음). 신규 카드 스타일 만들지 않음 | AppSettingsDialog.xaml:87-96 선례 |
| D9 | 필수표시 `*` | 구현 | 라벨 `TextBlock` 옆에 별도 `TextBlock`(`*`, `AppDangerBrush`)을 가로 배치. **`*`는 번역 대상 문구가 아닌 기호라 resw 미등록**(하드코딩 금지 규칙의 대상 아님 — 주석으로 명시) | AGENTS.md "문구 하드코딩 금지"의 취지 해석 |
| D10 | 편집 모드 카드 | 범위 | "테스트 추가" 카드는 **새 작업일 때만 표시**(현행 `ShowTestToggle` 유지) — 편집 시 숨김 | 현행 VM `:36` 무변경 |
| D11 | 상태→라벨 매핑 위치 | 위치 | `TaskEditDialogViewModel`에 둔다. `TaskPage`의 static 캐시를 참조하지 **않는다**(다이얼로그가 페이지에 의존하면 방향 역전) | 4-D 표 |

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-T5 (편집 다이얼로그 + 삭제 확인) | Must | T1, T2, T3 | ✅ 커버(시각 재구성 + 시작일 기본값, 기능 기구현) |
| FR-T6 (테스트 추가 토글) | Should | T3 | ✅ 커버(**회귀 방지** — 카드화하되 `AddTestRequested` 경로 보존) |
| FR-T1 (작업 데이터 모델) | Must | — | 이번 범위 외 (`TodoItem` 무변경) |
| FR-T7 (담당자(who) 필드) | Could | — | **의도적 미구현** — 사용자 제외 결정 유지(`deferred.md:9`). 미충족이 아니라 범위 제외 |
| FR-T2·T3·T4·T8 | Must/Should | — | 이번 범위 외 (직전 plan에서 기구현, 이번 변경이 닿지 않음) |
| FR-C1·C2·C3·C4·FR-S1~S5·FR-E1~E5·FR-H*·FR-N* | Must/Should | — | 이번 범위 외 (Phase 0~5에서 기구현, 이번 diff가 닿지 않음). 단 **FR-S5**(프로젝트 설정 다이얼로그 restyle, Should)는 기구현이 아니라 **`deferred.md` 대기 중** — 이번에도 유지 |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | T1·T2·T3 | ✅ 충족 (클린 리빌드 오류 0, 경고는 AGENTS.md 기존 5건뿐) |
| NFR-2 (계층 위반 0) | — | T2 | ✅ 충족 (VM은 `Presentation/ViewModels`, 도메인 무변경, D11로 페이지 역참조 금지) |
| NFR-3 (DB 스키마 하위호환) | — | — | ✅ 무영향 (`TodoItem`·스키마·직렬화 무변경) |
| NFR-4 (다국어 ko/en 대칭) | — | T1·T3 | ✅ 충족 (`TaskEdit*` 키 집합 ko/en 완전 대칭 — F-7 재확인) |
| NFR-5 (테스트) | — | — | 조건 미발동 (테스트 프로젝트 부재 — AGENTS.md 명시) |

## 작업 단계

### T1 — resw 문구 정비 (ko/en) `Type B`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **값 변경(기존 키 유지)**:
  - `TaskEditBox_Title.PlaceholderText` → ko `무엇을 해야 하나요?` / en `What needs to be done?`
  - `TaskEditBox_Description.PlaceholderText` → ko `작업 내용 설명 (선택)` / en `Task description (optional)`
  - `TaskEditStartDate.PlaceholderText`·`TaskEditEndDate.PlaceholderText` → ko `연도-월-일` / en `yyyy-mm-dd`
- **신규 키**: `TaskEdit_AddTestCard.Header`(ko `테스트 추가` / en `Add test`), `TaskEdit_AddTestCard.Description`(ko `이 작업을 테스트 목록에도 추가합니다.` / en `Also add this task to the test list.`) — **`x:Uid` 소비이므로 속성 접미 필수**(AGENTS.md 함정 3). `TaskEdit_Submit`(ko `등록` / en `Add`) — **코드 `LocalizationService.Get` 소비이므로 베어네임**.
- **제거 키는 이 task에서 다루지 않는다** — `TaskEdit_AddTest.Header`·`TaskEdit_TitleRequired`는 T1 시점에 **아직 실제로 소비 중**이다(`TaskEditDialog.xaml:73` `x:Uid`, `TaskEditDialog.xaml.cs:41` `Get()`). 여기서 지우면 `LocalizationService.Get`이 **키 문자열을 그대로 화면에 표시**한다. 참조를 끊는 **T3에서 함께 제거**한다(plan-reviewer M2).
- **Design**: 해당 없음 — 신규 심볼 없음(리소스 문구만).
- **Acceptance**: 빌드 성공(신규 경고 0) + 신규 3키·변경 4키가 **ko/en 양쪽에 동일 name으로 존재**(grep 대조) + **기존 파일의 BOM 유무 보존**(AGENTS.md 함정 1) + 이 시점 앱 동작은 **placeholder 문구만 바뀌고 나머지는 현행과 동일**
- **Edge Cases**: 한쪽 언어만 추가하면 다른 언어에서 빈 문자열이 되고 **빌드는 통과**한다(함정 2) → 양쪽 grep 대조가 acceptance. `x:Uid` 키에 접미를 빠뜨리면 빌드 오류 없이 빈 라벨(함정 3).
- **Halt Forecast**: 없음 — 리소스 추가·값 변경만. **키 제거는 이 task에 없다(T3로 이관)**.

### T2 — VM 확장: 상태 라벨·버튼 문구·시작일 기본값 + 생성자 시그니처 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/ViewModels/TaskEditDialogViewModel.cs`, `DevDashboard_WinUI/Presentation/Views/Dialogs/TaskEditDialog.xaml.cs`, `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`
- **Design**: ① 배치 — 표시 문자열은 전부 `TaskEditDialogViewModel`(Presentation/ViewModels), 다이얼로그는 VM을 읽기만 한다. ② 신규 심볼 — `HeaderTitle`(모드별 제목), `StatusLabel`(상태 문구), `PrimaryButtonLabel`(등록/저장) 3개 계산 프로퍼티 + VM 생성자에 `TodoStatus status` 파라미터. ③ 의존 방향 — VM은 `LocalizationService`만 참조하고 **`TaskPage`를 참조하지 않는다**(D11 — 페이지 역참조 금지). 호출부(`TaskPage.xaml.cs`) → 다이얼로그 → VM 단방향. ④ 비추상화 — 상태→라벨을 Converter·공용 매퍼로 빼지 않고 VM 안 `switch` 식으로 직접 반환(사용처 1곳, `TestPage.StatusBrush` 선례와 같은 결).
- **생성자 시그니처(사전 승인 항목)**:
  - `TaskEditDialog(TodoItem? existing, AppSettings settings, TodoStatus status = TodoStatus.Waiting)`
  - `TaskEditDialogViewModel(TodoItem? existing, AppSettings settings, TodoStatus status = TodoStatus.Waiting)`
  - **편집 모드는 `existing.Status`가 우선**한다(파라미터 무시) — 편집 호출부가 상태를 중복 전달하지 않아도 정확하다.
  - 기본값을 두어 **`EditTodoAsync`(`:199`) 호출부는 무변경**, `ColumnAdd_Click`(`:184`)만 `status`를 넘기도록 1줄 수정한다.
- **시작일 기본값(D6)**: VM 생성자의 신규 작업 분기에 `StartDate = DateTimeOffset.Now.Date;` — **`.Date`로 시각을 버린다**(`BuildResult()`가 `StartDate?.DateTime`을 그대로 넣으므로 시각이 섞이면 저장값에 잔여 시각이 남는다).
- **Acceptance**: 빌드 성공(신규 경고 0) + `TaskEditDialog` 생성자 호출부 2곳이 모두 컴파일됨 + 새 작업 시 `StatusLabel`이 **넘긴 상태**의 문구, 편집 시 **기존 작업 상태**의 문구를 반환(코드 경로 대조) + `PrimaryButtonLabel`이 새 작업="등록"/편집="저장" + **VM이 `TaskPage`를 참조하지 않음**(grep 대조) + 이 시점 XAML은 신규 VM 프로퍼티를 아직 소비하지 않으므로 **화면상 변화는 시작일 기본값(오늘)과 T1의 placeholder 문구뿐**(헤더·버튼 문구는 T3에서 배선되기 전까지 현행 유지)
- **Edge Cases**: 정의되지 않은 `TodoStatus` 값이 들어오면 → `switch` 식의 `_` 분기가 "예정" 문구로 폴백(예외 대신). 편집 대상의 상태가 나중에 바뀌어도 다이얼로그는 **열린 시점 스냅샷**을 표시(다이얼로그 수명이 짧아 무해 — 주석 명시). `DateTimeOffset.Now.Date`는 로컬 자정 기준 → 자정 직전 생성 시 날짜가 하루 넘어갈 수 있으나 사용자 로컬 시각 기준이라 의도된 동작.
- **Halt Forecast**: 없음 — 시그니처 변경이 사전 승인 항목에 등재됐고, 호출부 2곳이 전수 확인됐다. 파괴적·외부 작업 없음.

### T3 — XAML 시각 재구성 (헤더 pill·라벨·필수 강조·카드·버튼) `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/Dialogs/TaskEditDialog.xaml`, `DevDashboard_WinUI/Presentation/Views/Dialogs/TaskEditDialog.xaml.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 마크업은 `TaskEditDialog.xaml`, 유효성 Brush 헬퍼는 같은 화면의 코드비하인드(`x:Bind` 함수 바인딩이 Converter·ThemeResource를 못 받으므로 — AGENTS.md 함정 5). ② 신규 심볼 — `TitleBorderBrush(string title)` **static** 헬퍼(빈 제목이면 danger, 아니면 기본 테두리 Brush 반환) 하나뿐. **반환 Brush는 ARGB 리터럴로 만든 `SolidColorBrush` static 필드 2개**(danger `#FFF0716A` / 기본 `#FF2E2E34`) — `TaskPage.PriorityBrush`(`:126-131`)의 선례를 그대로 따른다. ⚠️ 값이 `Palette.xaml`과 **이중 정의**되므로 "팔레트 변경 시 동기화 필요" 주석을 붙인다(m1). ③ 의존 방향 — XAML이 T2의 VM 프로퍼티와 T1의 resw 키를 소비. 역참조 없음. ④ 비추상화 — 유효성 표시를 재사용 컴포넌트·Behavior로 빼지 않고 이 화면에 직접 둔다(사용처 1곳). 카드·라벨·pill 모두 **기존 자산 재사용**이라 신규 스타일 0.
- **구성**:
  - `<ContentDialog.Title>` → `StackPanel`(가로, `Spacing`) = 제목 `TextBlock`(`{x:Bind Vm.HeaderTitle}`) + `Border`(`TagBadgeStyle` + `AppMutedSoftBrush`) 안에 `{x:Bind Vm.StatusLabel}`. **pill은 제목 오른쪽에 인접**시킨다(다이얼로그 우측 끝 정렬이 아니므로 `Grid` 2열·`Stretch` 불필요 — m2).
  - **코드비하인드의 `Title = LocalizationService.Get(...)` 3줄 제거**(D2) + `PrimaryButtonText = Vm.PrimaryButtonLabel`로 교체.
  - 라벨 6개: `BodyStrongTextBlockStyle` → `InputLabelStyle`(D7). 제목 라벨만 가로 `StackPanel` + 빨간 `*`(D9).
  - 제목 `TextBox`: `BorderBrush="{x:Bind TitleBorderBrush(Vm.Title), Mode=OneWay}"` — `Vm.Title`이 `UpdateSourceTrigger=PropertyChanged`라 입력 즉시 재평가된다.
  - `ToggleSwitch` → `toolkit:SettingsCard x:Uid="TaskEdit_AddTestCard"` + 내부 `ToggleSwitch(OnContent="" OffContent="" MinWidth="0")`(D8). `Visibility`는 기존 `ShowTestToggle` 바인딩 유지(D10). **`toolkit` xmlns 선언 추가 필요**(현재 이 파일에 없음 — `AppSettingsDialog.xaml`의 선언을 그대로 따른다).
  - `ErrorText` `TextBlock` **제거**(D3). 단 `OnSave`의 `args.Cancel = true` 차단 로직은 **유지**(D4) — 문구 표시 2줄만 걷어낸다.
  - Deferred 재수용: 미사용 `x:Name="TitleBox"` 정리(유효성 배선이 대체).
  - **고아 resw 키 제거(T1에서 이관 — M2)**: 위 마크업·코드 변경으로 참조가 끊긴 뒤 `TaskEdit_AddTest.Header`·`TaskEdit_TitleRequired`를 **ko/en 양쪽에서 제거**한다. 제거 직전 잔존 참조가 실제로 0건인지 grep으로 확인한다(이 시점에는 정말 0이다).
- **Acceptance**: 빌드 성공(신규 경고 0) + 헤더에 제목과 상태 pill이 함께 렌더 + **코드비하인드에 `Title =` 할당이 남아 있지 않음**(grep 대조 — 남으면 pill이 런타임에 사라진다) + 라벨 6개가 모두 `InputLabelStyle` 소비(grep: `BodyStrongTextBlockStyle` 잔존 0) + 제목이 빈 상태에서 danger 테두리, 입력 시 기본 테두리 + `SettingsCard`가 제목·부제·우측 토글로 렌더 + **빈 제목으로 "등록"을 눌러도 저장되지 않음**(D4 회귀 방지) + **"테스트 추가" ON으로 만든 작업이 여전히 테스트를 생성**(FR-T6 회귀 방지 — `AddTestRequested` 경로 무변경) + `ErrorText`·`TitleBox` 심볼 잔존 0(grep) + **제거 2키가 ko/en 양쪽에서 사라지고 잔존 참조 0건**(grep 대조)
- **Edge Cases**: 코드비하인드 `Title` 할당 잔존 → pill 소실(위 acceptance가 grep으로 차단). `x:Uid` 접미 누락 → 카드 제목·부제가 **빈 문자열로 조용히 렌더**(함정 3). `toolkit` xmlns 누락 → 빌드 오류(즉시 발견). 제목이 매우 길 때 → 헤더 `StackPanel`에서 pill이 밀릴 수 있어 제목 `TextBlock`에 `TextTrimming` 적용 검토(⏳ HUMAN-VERIFY). 편집 모드에서는 카드가 숨겨져 다이얼로그 높이가 줄어듦(기존 동작과 동일). `SettingsCard` 기본 배경이 다이얼로그 배경과 대비가 약할 수 있음(⏳ HUMAN-VERIFY — 어긋나면 `Background`를 `AppCardAltBrush`로 지정). **빈 제목 위 hover 시 테두리가 회색으로 전환**되는 것은 D3-a로 수용된 동작이라 결함이 아니다(빨간 `*`가 상시 보조).
- **Halt Forecast**: 없음 — 결정이 D1~D11에 사전 확정됐고 파괴적 작업이 없다. 기존 키 2개 제거는 사전 승인 항목 등재.

## 사전 승인 항목 (일괄 승인 대상)
- **`TaskEditDialog`·`TaskEditDialogViewModel` 생성자에 `TodoStatus status` 파라미터 추가**(D1) — 기본값을 둬 호출부 2곳 중 1곳만 수정. 사용처 전수 확인 완료.
- resw(ko/en)에 신규 키 3개 추가 + 기존 키 4개 값 변경(T1) + 고아 키 2개(`TaskEdit_AddTest.Header`·`TaskEdit_TitleRequired`) 제거(**T3** — 참조를 끊은 뒤).
- 코드비하인드의 `Title = LocalizationService.Get(...)` 할당 제거(D2) 및 `ErrorText` `TextBlock` 제거(D3).
- 새 작업의 **시작일 기본값을 오늘로 설정**(D6) — 동작 변경이며 사용자가 명시 선택.
- 로컬 작업 브랜치 `task/taskedit-dialog-design-align` 생성 및 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR.
- 시안 대조의 **최종 시각 판정** — 빌드는 구조 구현만 보증하고 "시안과 같아 보이는가"는 사용자만 판정 가능(⏳ HUMAN-VERIFY로 보고).

## Deferred / Follow-up
- **[시작일을 지울 수단 부재]** — `CalendarDatePicker`는 선택된 날짜를 UI로 비울 수 없다. 시작일 기본값이 오늘로 채워지면서 "시작일 없는 작업"을 새로 만들 방법이 사라진다(편집 모드의 기존 빈 값은 그대로 유지된다). 지우기 버튼 또는 "날짜 없음" 옵션 추가는 별도 진행. (이번 D6의 귀결)
- **[다른 다이얼로그의 라벨 스타일 불일치]** — `InputLabelStyle`이 정의만 되고 소비처가 0이었다는 것은, 다른 다이얼로그들도 굵은 본문체 라벨을 쓰고 있어 이 화면만 시안 정합 후 **화면 간 라벨 스타일이 갈린다**는 뜻이다. 다른 다이얼로그 시안 확보 후 일괄 정합 검토. (T1 조사에서 발견)
- **[`TaskEdit_TitleRequired` 제거에 따른 검증 피드백 약화]** — 빈 제목으로 등록을 누르면 이제 **아무 문구 없이 닫히지 않기만** 한다(테두리는 이미 빨간 상태). 접근성(스크린 리더) 관점에서 시각 외 피드백이 없어진다. 필요 시 `TextBox.Description` 또는 자동화 속성으로 보완 검토.
- **[미사용 심볼 정리]** 대장 항목 중 `TaskEditDialog.xaml x:Name="TitleBox"` 건은 **이번 T3에서 해소** — 완료 후 대장에서 해당 문구를 제거한다.
- **[SUGGEST] `TaskEditDialog`의 선택적 `status` 파라미터가 미래의 함정** — C# 선택적 매개변수는 누락해도 컴파일 경고가 없어, **제3의 호출부가 "새 작업"을 만들며 `status`를 빠뜨리면 조용히 `Waiting`으로 생성**된다. 현재 호출부 2곳은 정확하고(편집 경로는 `existing?.Status ?? status`로 기본값을 항상 무시) plan이 근거를 문서화했으므로 지금은 결함이 아니다. 3번째 호출부가 생기면 명시적 오버로드 분리를 검토. (T2 quality 리뷰 SUGGEST)
- 기타 대장(`docs/plans/deferred.md`) 기등재 항목은 이번 작업과 무관 — 그대로 유지.

## Out of Scope
- `TodoItem` 도메인·SQLite 스키마·직렬화 변경.
- **다른 다이얼로그**(테스트/작업 기록/설정/프로젝트 설정 등)의 restyle — 이번은 `TaskEditDialog` 한 화면.
- `Dialog_Save` 공용 키의 값 변경(8개 다이얼로그 오염).
- 카테고리 기본값·테스트추가 토글 기본값 변경(사용자가 명시적으로 제외 — D6).
- 픽셀 단위 수치 일치 — 목업 HTML이 없어 대조 불가. 구조·구성 요소 일치까지가 이번 목표.

## Open Questions
- [x] 헤더 상태 pill 처리 → **실제 상태 표시(생성자에 상태 전달)**(사용자, 2026-07-20)
- [x] 제목 필수 강조 시점 → **비어 있으면 상시 빨간 테두리**(사용자, 2026-07-20)
- [x] 편집 모드 버튼 문구 → **새 작업="등록" / 편집="저장"**(사용자, 2026-07-20)
- [x] 시안의 기본값 3종 중 반영 범위 → **시작일=오늘 1건만**(사용자, 2026-07-20)

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + **AGENTS.md 기존 경고 5건(NU1903 1 + CS0612 4) 외 신규 경고 0**
- resw 대조: 신규·변경 키의 name 집합이 ko/en 일치 grep + 제거 2키의 잔존 참조 0건 grep
- 회귀 방지 grep(⚠️ **판정 대상을 정확히 한정**):
  - `TaskEditDialog.xaml.cs`에 `Title =` 할당 **0건**(D2 — 남으면 헤더 pill이 런타임 소실, 빌드로는 못 잡는다)
  - `TaskEditDialog.xaml`에 `BodyStrongTextBlockStyle` **0건**(D7 라벨 교체 완료)
  - `TaskEditDialog.xaml.cs`에 `args.Cancel = true` **1건 이상**(D4 — 빈 제목 저장 차단 생존)
  - `AddTestRequested` 대입부 **1건 이상**(FR-T6 생존)
  - `ErrorText`·`TitleBox` 심볼 **0건**
- 회귀(빌드로 검증 불가 → ⏳ HUMAN-VERIFY): 칸반 각 열의 `+ 새 작업`에서 헤더 pill이 그 열 상태와 일치 / 카드 클릭·우클릭 편집에서 pill이 해당 작업 상태와 일치 / 빈 제목으로 등록 시 닫히지 않음 / 제목 입력 시 테두리 즉시 복귀 / **빈 제목 위 hover 시 테두리 회색 전환은 D3-a로 수용된 동작**(결함 아님) / "테스트 추가" ON → 테스트 생성 / 시작일에 오늘이 채워짐 / 편집 모드에 카드 숨김·버튼 "저장" / 목록 뷰 편집 경로 정상 / 시안 육안 대조

## Phase Ledger
- 전 task(T1~T3) 완료.
- Phase F 통과 (HEAD 934fae6) — F-7 plan-completion-reviewer BLOCKER 0/MAJOR 0/MINOR 4(m1 pill 글자 굵기→F-8 목록 2번에 반영, m2 NFR 행 누락→PRD Coverage에 추가, m3 FR-C1/C2/C4 표기 누락→추가, m4 문서 갱신 미커밋→최종 커밋에 포함). 클린 리빌드(-t:Rebuild) 오류 0·신규 경고 0.
- Phase G 통과 (Must 100%) — 커버 대상 Must FR(T5) 충족, Should(T6)도 충족. NFR-1~5 전건 확인. F-7 전수 대조 재사용(incomplete·Sonnet 대체 아님). 갭 0건이라 재루프 없음.
- **F-8 미통과 — 시각 확인 대기**: `## 시각 요소 분해`의 렌더 일치 7항목이 `⏳ 미확인`으로 남아 **완료 선언 보류**(사용자 육안 대조 필요).

## Progress Log
- T1-T2 완료 (커밋 52d94f0, 8066961): resw 문구 정비(placeholder 4키 값 + 신규 3키 ko/en) / VM에 `HeaderTitle`·`StatusLabel`·`PrimaryButtonLabel` 추가 + 생성자에 `TodoStatus status` 선택적 파라미터 + 새 작업 시작일 기본값=오늘. 빌드 OK(기존 경고 5건만).
  - **리뷰 지적 반영(T2, spec BLOCKER + quality MAJOR 동시 지적)**: 코드비하인드의 `Title =` 제거와 `PrimaryButtonText = Vm.PrimaryButtonLabel` 교체는 **T3 몫인데 T2에서 선반영**해, 그 커밋 단독으로는 XAML `<ContentDialog.Title>`이 아직 없어 **헤더 제목이 빈 상태로 렌더되는 회귀**가 났다. `Title = Vm.HeaderTitle`(문구 결과는 기존과 동치)과 `PrimaryButtonText = Get("Dialog_Save")`(현행)로 되돌려 해소.
  - 결정: `StatusLabel`은 생성자에서 1회 계산해 `{ get; }`으로 고정 — 다이얼로그 수명이 짧아 상태 변경 추적이 불필요(주석 명시).
  - 결정: 상태→라벨 매핑을 `TaskPage`의 static 캐시와 공유하지 않고 VM에 `StatusLabelFor` switch를 별도로 둠 — D11(다이얼로그가 페이지를 역참조하지 않음). 사용처 1곳이라 공통화 기준(3회) 미달.
  - 구현 차이(무해): plan은 `DateTimeOffset.Now.Date`를 제시했으나 그 식은 `DateTime`을 반환해 `DateTimeOffset?`에 대입 불가 → 같은 파일 선례를 따라 `new DateTimeOffset(DateTime.Today)` 사용. 결과 동일(로컬 자정).
- T3 완료 (커밋 예정): XAML 헤더를 `<ContentDialog.Title>`(제목+상태 pill)로 재구성, 라벨 6개를 `InputLabelStyle`로 교체 + 제목에 빨간 `*`, 제목 빈 상태 danger 테두리(`TitleBorderBrush`), "테스트 추가"를 `toolkit:SettingsCard`로 카드화, `ErrorText` 제거(차단 로직은 유지), 버튼 문구 등록/저장 배선, 고아 resw 키 2개 제거. 빌드 OK. spec·quality 리뷰 지적 0건.
  - **빌드가 잡은 함정**: `{x:Bind TitleBorderBrush(...)}`로 쓰면 static 메서드를 인스턴스로 접근해 **CS0176**이 난다. `TaskPage.xaml:82` 선례대로 `xmlns:local` 선언 + `{x:Bind local:TaskEditDialog.TitleBorderBrush(...)}` 타입 한정으로 해결.
  - 결정: 라벨과 입력칸을 묶는 내부 `StackPanel`은 `Spacing`을 주지 않고 `InputLabelStyle`의 `Margin 0,0,0,4`에 간격을 위임했다(바깥 `Spacing="12"`는 필드 그룹 간 간격 담당 — 책임 분리).
  - 확인: 토글 On 살몬색은 `Palette.xaml:81-82`의 전역 accent 오버라이드(#F0716A)로 **이미 충족**돼 신규 스타일이 필요 없었다.
