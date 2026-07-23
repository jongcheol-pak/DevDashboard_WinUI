# plan.md — 대시보드 카드: 빈 설명·태그 플레이스홀더 + 헤더/액션 버튼 테두리 제거

**기준 디자인**: `docs/design/DevDashboard Redesign.dc.html` — 카드 마크업 `:352~410`(설명·태그 5블록 조건 `:366~386`, 액션 버튼 `:357~401`, 슬롯 `:403~409`). 사용자가 시안을 갱신(플레이스홀더 추가)하고 새 렌더 이미지 1장 제공(2026-07-23).
**PRD**: `docs/prd.md` — 이번 변경은 active **FR-D1**(카드 시안 재구성)의 세부 보강에 닿으므로 연결한다(Phase G 재검증 활성). `## PRD Coverage` 참조.

## 요구 이해

> 원문(사용자, 2026-07-23): "디자인 시안을 확인해서 설명, 태그가 없는 경우 이미지처럼 표시 하고, 헤더에 있는 고정/삭제 버튼, 할 일, 테스트 목록, 더 보기 버튼은 이미지처럼 버튼의 사각테두리가 없도록 수정" (+ 시안 갱신 + 렌더 이미지 1장)

이해한 요구:
- **① 빈 설명·태그 플레이스홀더**: 카드에 설명/태그가 없을 때 빈 공간으로 두지 말고 시안대로 "추가 유도" 플레이스홀더를 표시한다. 시안은 상태를 3분기한다 — 둘 다 없으면 큰 점선 박스 "＋ 설명·태그 추가", 설명만 없으면(태그는 있음) 텍스트 링크 "＋ 설명 추가", 태그만 없으면(설명은 있음) 점선 pill "＋ 태그 추가".
- **② 액션 버튼 사각 테두리 제거**: 헤더의 핀·삭제 버튼, 하단 액션의 할 일·테스트·더 보기 버튼은 시안에서 전부 `border:none`인데, 현행 공용 `CardIconButtonStyle`이 `BorderThickness="1"`이라 사각 테두리가 보인다. 시안대로 테두리를 없앤다. **스크립트 슬롯 버튼은 시안이 `border:1px solid`라 테두리를 유지**한다(요구에 미포함).
- 직전 세션(2026-07-23)에서 넣은 "설명·태그 자리 고정 예약(Height 32/24)"은 이번 시안 방식(플레이스홀더가 자리를 채우고 액션 행을 바닥으로 밀어냄)으로 **대체**한다.

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-D1 카드 시안 재구성(빈 상태 플레이스홀더·버튼 시안 정합) | Must | T1, T2, T3 | ✅ 커버(세부 보강) |
| FR-D2 / FR-D3 / FR-D4 (대시보드 카드 기존 항목) | Must/Should | — | 이번 범위 외 (기구현 — 색상·액션 축약·그리드는 직전 plan에서 완료, 이번 diff 무관) |
| FR-C* / FR-S* / FR-T* / FR-E* / FR-H* / FR-N* | Must/Should | — | 이번 범위 외 (기구현) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(실측 baseline `CS0618` 1건 제외) |
| NFR-2 (계층 위반 0) | — | T1 | 검증 대상(VM은 `Microsoft.UI.Xaml` Brush 미참조 유지) |
| NFR-4 (다국어 ko/en 대칭) | — | T2 | 검증 대상(신규 resw 3키 전량 ko/en) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## Investigation Log (근거)

- **위키 참조**: vault 미설정 — 코드·시안 1차 출처로 진행.
- **기존 plan**: 직전 카드 리디자인 plan은 `## Phase Ledger` **Phase G 통과(Must 100%)** = 완료 → 새 계획으로 교체(Deferred는 F-6.5에서 `docs/plans/deferred.md`로 이관 완료).
- **Deferred 대장 확인**: `[태그 pill 시안화 미적용]`(deferred.md:84)이 있으나 이번 작업과 **별개** — 그 항목은 태그 *표시*를 `MarqueeTagsControl`에서 시안 pill로 바꿀지의 문제이고, 이번은 태그 *표시는 유지*하고 "태그가 **없을 때** 플레이스홀더"만 추가한다. 대장에 그대로 유지.
- **시안 명세 (직접 Read, `:365~409`)**:
  - 본문 컨테이너(`:365`): `padding:14px 16px 16px; display:flex; flex-direction:column; gap:12px; flex:1`. 액션 행(`:387`)에 `margin-top:auto`(하단으로 밀어냄).
  - `c.noMeta`(둘 다 없음, `:366~370`): `button` `flex:1; min-height:52px; border:1px dashed #2e2e35; border-radius:9; background:transparent; color:#5f5d66; font-size:11.5px; gap:7; justify-content:center` → `＋`(13px) + "설명·태그 추가". hover `color:#8b7cf7; border-color:#4a4460`.
  - `c.hasDesc`(`:371~373`): `div font-size:12px; color:#98959d; nowrap; ellipsis` = 설명 1줄. **단 현행은 사용자 결정으로 2줄(`MaxLines="2"`) — 이번 요구는 "없을 때"라 설명 표시 방식은 무변경(2줄 유지)**.
  - `c.needDesc`(설명 없고 태그 있음, `:374~376`): `button border:none; background:transparent; padding:0; color:#5f5d66; font-size:12px; align-self:flex-start; gap:6` → "＋ 설명 추가".
  - `c.hasTags`(`:377~383`): 태그 pill wrap → **현행 `MarqueeTagsControl` 유지**.
  - `c.needTags`(태그 없고 설명 있음, `:384~386`): `button border:1px dashed #2e2e35; border-radius:6; padding:3px 8px; color:#5f5d66; font-size:11px; align-self:flex-start` → "＋ 태그 추가".
  - 버튼 테두리: 핀(`:357`)·삭제(`:360`) 28px `border:none`, 할일(`:392`)·테스트(`:395`)·더보기(`:399`) 34px `border:none`. 슬롯(`:404,405`) 28px **`border:1px solid #2b2b31`**(유지), 슬롯추가(`:408`) `border:1px dashed`(유지).
- **현행 구현 (직접 Read)**:
  - `Presentation/Views/DashboardView.xaml`: 직전 세션에서 본문을 `Grid`(Row0 상단 고정 / Row1 `*` / Row2 하단)로 바꾸고 설명 `Height="32"`·태그 `Height="24"` 고정 예약(`:192~207` 부근). 이번에 플레이스홀더 방식으로 재구성.
  - `CardIconButtonStyle`(`:33~90`) `BorderThickness="1"`, `PointerOver` VSM에서 `BorderBrush`를 `ControlStrokeColorSecondaryBrush`로. **소비처는 `DashboardView.xaml` 9곳뿐**(grep 전수 — 핀·삭제·할일·테스트·더보기 5 + 슬롯 4). `DashboardView.xaml.cs`의 1건은 주석. 다른 파일 0 → 스타일 수정은 카드 내부만 영향. (개발도구 실행 버튼은 이미 로컬 `BorderThickness="0"`이라 이 스타일 미소비.)
  - `ProjectCardViewModel`: `Description`(`:159` `_item.Description` 위임)·`Tags`(`:165` `IReadOnlyList<string>` 위임)·`Initial`·`EditCommand`(프로젝트 설정 다이얼로그) 실재. **`HasDescription`/`HasTags` 없음** → 신규. 이름 충돌 없음(`HistoryDialogViewModel`의 동명은 별개 클래스).
  - resw 카드 키: `CardMenu_*.Text`(x:Uid `.Text` 접미). `AddCardTemplate`의 `TextBlock x:Uid="AddNewProjectText"`(`:553`)로 **DataTemplate 안 TextBlock의 `x:Uid`가 동작함이 확인됨** → 플레이스홀더 문구도 같은 방식.
- **AGENTS.md**: 실재. 빌드 `MSBuild x64`(메모리 정본). 이번 참조 경로 전부 실재 — stale 없음.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| `ProjectCardViewModel.HasDescription`/`HasTags`/`ShowMetaPlaceholder`/`ShowDescPlaceholder`/`ShowTagsPlaceholder` | 카드 VM에 빈 상태 판정 프로퍼티 없음. `StringNotEmptyToVisibility` 컨버터는 단일 값만 판정(조합 불가) | **신규(계산 프로퍼티)** — noMeta/needDesc/needTags는 설명·태그 **조합** 조건이라 단일 컨버터로 표현 불가. 파생 지점을 VM 한 곳으로 모은다 |
| 플레이스홀더 3버튼 마크업 | 카드에 점선 버튼 선례 `DashedAddButtonStyle`(슬롯 추가) | **마크업 신규 + 점선은 기법 재사용 검토** — noMeta/needTags의 점선 테두리는 `Rectangle StrokeDashArray` 기법(함정 6)을 쓰되, 크기·문구가 달라 인라인. needDesc는 테두리 없는 텍스트 버튼이라 스타일 불필요. 신규 C# 심볼 0 |
| resw 3키(`CardPlaceholder_*`) | — | **신규** — 신규 문구, ko/en 대칭 |

## 시각 요소 분해

> 기준: 시안 `:365~409` 인라인 스타일 + 렌더 이미지(2026-07-23). CSS px는 WinUI 논리 단위로 옮긴다. 최종 판정은 빌드 후 육안 대조(⏳ HUMAN-VERIFY). hover 보라(`#8b7cf7`)는 앱 액센트로 대체(D3 — 직전 plan D7 선례).

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 본문 컨테이너 | 레이아웃 | flex column, gap 12, flex:1, 액션은 margin-top:auto | `Grid`(상단 Auto / `*` 여백 / 하단 Auto), 상단 `StackPanel Spacing="12"` | HTML `:365,387` |
| noMeta 박스 | 크기·테두리 | flex:1, min-height:52, 1px dashed #2e2e35, radius 9, bg transparent | `Button` `MinHeight="52"` + `Rectangle StrokeDashArray`(함정 6) 또는 점선 스타일, `CornerRadius="9"` | HTML `:367` |
| noMeta 박스 | 내용·정렬 | ＋(13px) + "설명·태그 추가"(11.5px, #5f5d66), 중앙 정렬, gap 7 | `StackPanel Orientation="Horizontal" Spacing="7"` 중앙, `FontIcon`/글리프 13 + `TextBlock x:Uid` 11.5 | HTML `:368` |
| noMeta 박스 | hover | color #8b7cf7, border #4a4460 | 앱 액센트(D3) | HTML `:367` |
| 설명 | 글자·줄수 | 12px, #98959d | `FontSize="12"`, `TextFillColorSecondaryBrush`, **`MaxLines="2"` 유지(현행)** | HTML `:372` + 사용자 결정 |
| needDesc | 형태 | border:none, bg transparent, padding 0, #5f5d66, 12px, 좌측 정렬 | `Button` 투명·테두리 0, `HorizontalAlignment="Left"`, `＋`+`TextBlock x:Uid` | HTML `:375` |
| needTags | 형태 | 1px dashed #2e2e35, radius 6, padding 3/8, #5f5d66, 11px, 좌측 정렬 | 점선 pill(`Rectangle StrokeDashArray` 또는 점선 스타일), `CornerRadius="6"`, `Padding="8,3"`, 좌측 | HTML `:385` |
| 태그 | — | pill wrap | **현행 `MarqueeTagsControl` 유지**(없을 때만 needTags로 대체) | 사용자 결정 |
| 핀·삭제 버튼 | 테두리 | border:none | `CardIconButtonStyle` `BorderThickness` 1→0(스타일 정의 수정) | HTML `:357,360` |
| 할일·테스트·더보기 버튼 | 테두리 | border:none | 위와 동일 스타일 공유 → 함께 해소 | HTML `:392,395,399` |
| 슬롯 버튼 | 테두리 | 1px solid #2b2b31 (**유지**) | 스타일 기본이 0이 되므로 슬롯 4곳에 `BorderThickness="1"` 로컬 지정 | HTML `:404` |

## Decisions

- **D1 (플레이스홀더 클릭 → 프로젝트 설정 다이얼로그)**: 3개 플레이스홀더를 누르면 `EditCommand`(프로젝트 설정 다이얼로그)를 실행한다. *Source*: 설명·태그를 추가하려면 프로젝트를 편집해야 하고, 그 진입점은 기존 `EditCommand`(`…` 메뉴의 "프로젝트 설정"과 동일 커맨드)다. 시안 `onMore` 바인딩은 목업의 임시 핸들러이며, 실제 앱에서 설명/태그 입력 수단은 설정 다이얼로그 하나뿐(자명 확정, 신규 커맨드 0).
- **D2 (버튼 테두리는 공용 스타일에서 제거, 슬롯만 로컬 복원)**: `CardIconButtonStyle`의 `BorderThickness` Setter를 `1`→`0`으로 바꿔 핀·삭제·할일·테스트·더보기 5버튼의 테두리를 한 번에 없앤다. 슬롯 버튼 4곳은 시안이 `border:1px solid`라 로컬 `BorderThickness="1"`을 지정해 유지한다. *Source*: 소비처 grep 전수 — 스타일은 `DashboardView.xaml`에서만 소비(카드 내부). 스타일에서 없애고 슬롯만 복원하는 것이, 5곳에 로컬 0을 다는 것보다 적은 변경이며 의도가 스타일에 모인다. `PointerOver` VSM의 `BorderBrush` 변경은 `BorderThickness="0"`이면 보이지 않으므로 그대로 둬도 무해(제거는 선택).
- **D3 (hover 보라 → 앱 액센트)**: 플레이스홀더 hover 강조 `#8b7cf7`(보라)은 이 앱 팔레트에 없으므로 앱 액센트(`AppAccentBrush`)를 쓴다. *Source*: 직전 plan D7과 동일 — 팔레트 다크 단일·전역 정책(함정 4), 카드만 보라를 쓰면 강조색이 둘로 갈린다.
- **D4 (플레이스홀더 문구는 resw x:Uid `.Text`)**: 3개 문구를 resw ko/en에 `.Text` 접미로 등록하고 DataTemplate 안 `TextBlock x:Uid`로 소비한다. *Source*: `AddNewProjectText`(`:553`)가 DataTemplate 안 `TextBlock x:Uid`로 이미 동작함이 확인됨(함정 3의 `.Text` 접미 준수). 글리프 `＋`는 문구에 넣지 않고 별도 `FontIcon`/`TextBlock`으로 분리(문구 재사용·정렬 독립).
- **D5 (직전 세션 Height 고정 예약 대체)**: 2026-07-23의 설명 `Height="32"`·태그 `Height="24"` 고정 예약은 제거하고, 시안 방식(빈 상태에 플레이스홀더가 자리를 채우고 액션 행을 `*` 여백으로 바닥에 밀어냄)으로 대체한다. *Source*: 시안 `:365,387`. 플레이스홀더가 항상 무언가를 표시하므로 자리 예약이 불필요해지고, 하단 위치는 셀 균일 높이(`MinItemHeight="256"`) + 하단 바닥 배치로 유지된다.

## 작업 단계

### T1 — VM: 빈 설명·태그 상태 파생 프로퍼티 `Type C`
- [x] 구현
- **Files**: `Presentation/ViewModels/ProjectCardViewModel.cs`
- **Design**: ① 배치 — 파생 값은 `ProjectCardViewModel`(Presentation VM), 브러시·표시는 XAML. ② 신규 심볼 — `HasDescription`/`HasTags`(원자 판정) + `ShowMetaPlaceholder`/`ShowDescPlaceholder`/`ShowTagsPlaceholder`(조합 파생, 상호 배타). ③ 의존 방향 — VM은 `string`/`bool`만 계산(`Microsoft.UI.Xaml` Brush 미참조 — NFR-2 유지). XAML이 `BoolToVisibility`로 소비. ④ 비추상화 — 빈 상태를 담는 값 객체·enum을 만들지 않는다(bool 5개로 충분, 소비처 1곳).
- **구성**:
  - `HasDescription => !string.IsNullOrEmpty(Description)`, `HasTags => Tags is { Count: > 0 }`.
  - `ShowMetaPlaceholder => !HasDescription && !HasTags`(noMeta), `ShowDescPlaceholder => !HasDescription && HasTags`(needDesc), `ShowTagsPlaceholder => HasTags == false && HasDescription`(needTags). 세 값은 상호 배타 + hasDesc/hasTags와 합쳐 5블록이 겹치지 않는다.
  - 한글 주석으로 각 프로퍼티의 시안 대응(noMeta/needDesc/needTags)을 1줄씩 명시.
  - **편집 후 갱신**: 편집 저장 시 `MainViewModel`(`:477` `new ProjectCardViewModel(item, ...)`)이 카드 VM을 **재생성**하므로 파생값은 자동 갱신된다 → **`OnPropertyChanged` 배선 불요**(plan-reviewer m2 확정).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(실측 baseline `CS0618` 1건 제외).
  2. 5개 프로퍼티가 정의되고, `ShowMeta`/`ShowDesc`/`ShowTags`Placeholder가 상호 배타(동시에 둘 이상 true 불가)임이 로직으로 성립한다.
  3. `ProjectCardViewModel.cs`에 `Brush`/`SolidColorBrush` 타입이 신규로 추가되지 않는다(NFR-2 — 값은 bool).
- **Edge Cases**: `Description`이 공백만("   ") → `IsNullOrEmpty`는 false지만 시안상 "빈"에 가까움 — 현행 `StringNotEmptyToVisibility`도 `IsNullOrEmpty` 기준이므로 **일관되게 `IsNullOrEmpty` 사용**(공백만은 "있음"으로 처리, 기존 동작과 동일) / `Tags`가 null → `is { Count: > 0 }`가 false로 안전 / 태그 0개 → HasTags false.
- **Halt Forecast**: 없음 — 단일 파일, 신규 계산 프로퍼티, 파괴적·외부 작업 없음.

### T2 — 카드 템플릿: 빈 상태 플레이스홀더 5블록 재구성 + resw `Type C`
- [x] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 본문 상단 영역(현행 `Grid.Row=0` StackPanel)에 조건부 5블록. ② 신규 심볼 — 마크업만(신규 C# 0) + resw 3키. ③ 의존 방향 — XAML → VM(T1 값)·`EditCommand`·`BoolToVisibility`. ④ 비추상화 — 플레이스홀더 전용 UserControl·공용 스타일을 만들지 않는다(소비처 1곳, 인라인).
- **구성**:
  - 직전 세션의 설명 `Height="32"`·태그 `Height="24"` 고정 제거(D5). 상단 `StackPanel Spacing="12"` 안에 5블록을 `Visibility` 바인딩으로 조건부 배치:
    - noMeta 박스(`ShowMetaPlaceholder`): `Button` `MinHeight="52"` + 점선 테두리(`Rectangle StrokeDashArray` 기법 또는 `DashedAddButtonStyle` 응용) + `CornerRadius="9"`, 내부 중앙 정렬 `＋`+`TextBlock x:Uid="CardPlaceholder_DescAndTags"`, `Command="{Binding EditCommand}"`.
    - 설명(`HasDescription`): 현행 설명 `TextBlock`(2줄 유지) + `Visibility="{Binding HasDescription, Converter={StaticResource BoolToVisibility}}"`.
    - needDesc 버튼(`ShowDescPlaceholder`): 테두리·배경 없는 `Button` 좌측 정렬, `＋`+`x:Uid="CardPlaceholder_Desc"`, `EditCommand`.
    - 태그(`HasTags`): 현행 `MarqueeTagsControl` + `Visibility="{Binding HasTags, ...}"`.
    - needTags pill(`ShowTagsPlaceholder`): 점선 pill `CornerRadius="6" Padding="8,3"` 좌측 정렬, `＋`+`x:Uid="CardPlaceholder_Tags"`, `EditCommand`.
  - resw ko/en 3키(`.Text` 접미, 함정 3): `CardPlaceholder_DescAndTags`="설명·태그 추가"/"Add description · tags", `CardPlaceholder_Desc`="설명 추가"/"Add description", `CardPlaceholder_Tags`="태그 추가"/"Add tags"(en 문구는 자연스러운 관용 표현으로 확정).
  - 드래그 6속성·헤더 밴드·하단 액션/슬롯은 무변경(승계).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 4가지 조합이 각각 시안대로 렌더된다(코드 경로 존재): 둘 다 있음(설명+태그) / 설명만(설명+태그추가) / 태그만(설명추가+태그) / 둘 다 없음(설명·태그추가 박스). Visibility 바인딩이 T1의 상호 배타 값과 짝을 이룬다.
  3. 3개 플레이스홀더가 `EditCommand`에 바인딩된다(D1).
  4. 신규 resw 3키가 ko/en **양쪽**에 있고 `x:Uid` 소비 형식(`.Text`)을 따른다.
  5. 직전 세션의 설명 `Height="32"`·태그 `Height="24"` 고정이 제거된다.
  6. 액션 행이 셀 하단에 유지되고(하단 위치 회귀 없음), 드래그 6속성이 카드 `Border`에 남아 있다.
- **Edge Cases**: 설명 매우 김 → 2줄 말줄임(현행 유지) / 태그 많음 → `MarqueeTagsControl` 기존 처리 / noMeta 박스가 상단을 크게 차지 → `MinHeight="52"`로 시안 근사, 남는 공간은 `*` 여백 / 플레이스홀더 클릭 후 편집 저장 → T1 파생값 갱신 경로에 의존(T1 Edge 참조).
- **Halt Forecast**: (i) 사전 해소 — 소비처 `DashboardView.xaml` 1곳 grep 전수(함정 11). 파괴적·외부 작업 없음.

### T3 — 액션 버튼 테두리 제거(슬롯 유지) `Type C`
- [x] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`
- **Design**: ① 배치 — `CardIconButtonStyle` 정의(`:33~90`) + 슬롯 버튼 4곳. ② 신규 심볼 — 없음(스타일 값 변경 + 로컬 속성). ③ 의존 방향 — 스타일 소비처는 카드 내부 10곳(grep 전수). ④ 비추상화 — 슬롯 전용 스타일을 새로 파생하지 않는다(로컬 `BorderThickness`로 충분).
- **구성**:
  - `CardIconButtonStyle`의 `<Setter Property="BorderThickness" Value="1" />`을 `Value="0"`으로 변경 → 핀·삭제·할일·테스트·더보기 5버튼 테두리 제거(D2).
  - 슬롯 버튼 4곳(`IsCmd0~3Configured` 버튼)은 이미 `BorderBrush` 로컬 지정 상태 → `BorderThickness="1"`을 로컬로 추가해 시안(`1px solid #2b2b31`) 유지. 슬롯 추가(`DashedAddButtonStyle`)는 별도 스타일이라 무영향.
  - `PointerOver` VSM의 `BorderBrush` 애니메이션은 두께 0이면 비가시라 무해 — 제거하지 않는다(동작 보존, 최소 변경).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `CardIconButtonStyle`의 `BorderThickness` 기본값이 `0`이다.
  3. 슬롯 버튼 4곳에 로컬 `BorderThickness="1"`이 있어 테두리가 유지된다(시안 슬롯 정합).
  4. 핀·삭제·할일·테스트·더보기 5버튼에 사각 테두리가 렌더되지 않는다(⏳ HUMAN-VERIFY — hover 배경 강조는 유지).
- **Edge Cases**: 다른 파일이 `CardIconButtonStyle`을 소비 → grep 전수로 0 확인(카드 내부만) / hover 시 테두리 잔상 → `BorderThickness="0"`이면 `BorderBrush` 변경이 그려지지 않음 / 슬롯 로컬 `BorderThickness` 누락 → 슬롯 테두리가 사라져 시안과 어긋남(acceptance 3이 방어).
- **Halt Forecast**: 없음 — 단일 파일, 값·로컬 속성 변경, 파괴적·외부 작업 없음.

## Deferred / Follow-up
- `[태그 pill 시안화 미적용]`(deferred.md:84) — 태그 *표시*를 `MarqueeTagsControl` → 시안 pill로 바꿀지는 이번에도 미결(사용자 판단 대기). 이번 작업(빈 태그 플레이스홀더)과 별개로 대장에 유지.
- **[README/스크린샷 갱신]** — 대장의 기존 항목. 이번 카드 빈 상태 표시 변경으로 메인 화면 스크린샷이 또 낡는다(계속 Deferred).

## Out of Scope
- 설명 표시 줄 수 변경(시안은 1줄이나 사용자 결정으로 2줄 유지) — 이번 diff 대상 아님.
- 스크립트 슬롯 버튼 테두리 제거 — 시안이 테두리 유지라 대상 아님.
- 태그 표시 방식을 `MarqueeTagsControl` → 시안 pill로 교체 — Deferred(별개 논의).

## 사전 승인 항목 (일괄 승인 대상)
- 없음 (구조·공개 API·스키마·의존성 변경 없음. 전부 카드 템플릿·VM 파생값·resw 3키).
- 로컬 작업 브랜치(`task/dashboard-card-redesign`)에서 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·PR — 별도 승인.
- **시안 대조 최종 시각 판정** — 플레이스홀더 4조합 렌더·버튼 테두리 제거·하단 위치는 사용자만 판정(⏳ HUMAN-VERIFY).

## Progress Log
- T1-T2 완료 (커밋 5b06d0f, 508ac0f): VM 빈 상태 파생 5개 → 카드 본문 플레이스홀더 5블록 + resw 3키. 빌드 OK, spec·quality 리뷰 전부 OK.
  - 결정: 점선 플레이스홀더는 기존 `DashedAddButtonStyle`(Styles.xaml) 재사용(hover 액센트가 D3와 일치) — 신규 스타일 0. 클릭은 `EditCommand` 재사용.
  - 편집 후 갱신: 재생성 경로(MainViewModel:477) 확정 → OnPropertyChanged 배선 불요(plan-reviewer m2).

## Phase Ledger
- (진행 중 — T1·T2 완료)

## Open Questions
- (없음 — 근거로 전부 결정. D1 클릭 동작·D2 테두리·D3 hover색·D4 문구 방식·D5 레이아웃 전부 시안+코드+기존 선례로 확정)

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + 실측 baseline(`CS0618` 1건) 외 신규 0
- 회귀 방지 grep(소스 `*.cs`/`*.xaml`/`*.resw`, `obj/`·`bin/` 제외):
  - **T1**: `ProjectCardViewModel.cs`에 `HasDescription`·`HasTags`·`ShowMetaPlaceholder`·`ShowDescPlaceholder`·`ShowTagsPlaceholder` 정의 각 1 + **`Brush` 계열 타입 신규 0건**(NFR-2)
  - **T2**: `DashboardView.xaml`에 `CardPlaceholder_DescAndTags`·`CardPlaceholder_Desc`·`CardPlaceholder_Tags` x:Uid 각 1 + `EditCommand` 바인딩(플레이스홀더 3곳) / resw ko·en 3키 대칭 / 설명 `Height="32"`·태그 `Height="24"` 잔존 0(D5) / 드래그 6속성(`CanDrag`·`AllowDrop`·`DragStarting`·`DropCompleted`·`DragOver`·`Drop`) 잔존
  - **T3**: `CardIconButtonStyle`의 `BorderThickness` Setter가 `0` / 슬롯 버튼 로컬 `BorderThickness="1"` 4건 / `CardIconButtonStyle` 다른 파일 소비 0건(카드 내부만)
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - 설명만/태그만/둘 다 없음/둘 다 있음 4조합 카드가 시안대로 렌더되는지(플레이스홀더 문구·점선·정렬)
  - 플레이스홀더 클릭 시 프로젝트 설정 다이얼로그가 열리는지
  - 핀·삭제·할일·테스트·더보기 버튼의 사각 테두리가 사라졌는지(hover 배경 강조는 유지) / 슬롯 버튼 테두리는 유지되는지
  - 액션 행·슬롯이 카드마다 같은 하단 위치인지(직전 위치 고정 회귀 없음)

## 통과 체크리스트
- [x] 근거 없는 단정 0 (시안·코드 직접 Read)
- [x] `## 요구 이해` 작성됨
- [x] Impact Analysis 4-A~4-D (스타일 소비처 grep 전수, 신규 심볼 4-D 기록)
- [x] plan-reviewer 이슈 0 (BLOCKER 0/MAJOR 0, MINOR 2 반영 완료)
- [x] 각 task 검증 가능 acceptance + 동시 만족 가능
- [x] Open Questions 없음(근거로 전부 결정)
- [x] 코드 중 결정 분기 0
- [x] Type 분류 명시(T1/T2/T3 = C)
- [x] Design 필드(신규 심볼 도입 T1/T2)
- [x] Edge Cases 명시
- [x] Halt Forecast 명시
- [x] `## 시각 요소 분해` 작성됨(시각 충실도 요청 + 기준 디자인 존재)
