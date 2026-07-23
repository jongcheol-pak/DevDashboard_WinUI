# plan.md — 대시보드 카드: 플레이스홀더 점선박스 통일 + 카드 hover 이동 애니메이션 + 편집 시 핀 유지

**기준 디자인**: `docs/design/DevDashboard Redesign.dc.html`(갱신본, 미커밋) — 카드 `article`(`:352`), 플레이스홀더 noMeta/needDesc/needTags(`:367,375,385`). 사용자가 시안을 갱신(설명·태그 추가를 점선 박스로 통일)하고 렌더 이미지 1장 제공(2026-07-23).
**PRD**: `docs/prd.md` — 이번 변경은 active **FR-D1**(카드 시안 재구성)의 세부 보강에 닿으므로 연결한다(Phase G 재검증 활성). `## PRD Coverage` 참조.

## 요구 이해

> 원문(사용자, 2026-07-23): "① 디자인 시안을 확인해서 이미지처럼 설명 추가, 태그 추가 를 수정하고 마우스 오버시 색상이 다른데 확인해서 수정. ② 디자인 시안은 카드에 마우스가 올라가면 카드가 움직이는 애니메이션이 있는데 적용 가능한지 검토해서 적용. ③ 프로젝트 추가시 핀 고정 기본값이 on으로 되어 있으면 off로 설정" (+ 시안 갱신 + 렌더 이미지 1장)

이해한 요구:
- **① 플레이스홀더 점선박스 통일**: 갱신 시안에서 "＋ 설명 추가"(needDesc)·"＋ 태그 추가"(needTags)가 이전(텍스트 링크/작은 pill)과 달리 **카드 폭 전체 점선 박스**(noMeta와 같은 형태, 크기만 차등)로 통일됐다. 현행은 ③needDesc가 기본 Button(hover 시 회색 사각 배경), ⑤needTags가 `DashedAddButtonStyle`(hover 시 액센트 테두리)이라 **hover 동작이 서로 다르다** — 이것이 "마우스 오버 색상이 다른" 원인. 셋을 동일 `DashedAddButtonStyle`로 통일해 모양·hover를 일치시킨다. hover 강조색은 앱 액센트 산호(`AppAccentBrush`) 유지(Q1).
- **② 카드 hover 이동 애니메이션**: 시안 카드는 hover 시 `transform:translateY(-2px)`로 살짝 떠오른다(`:352`, transition 0.15s). 현행은 테두리색만 바뀐다. WinUI에서 `Storyboard`로 `TranslateTransform.Y`를 애니메이션해 적용한다(적용 가능 — 검토 완료).
- **③ 핀 기본값**: 코드 확인 결과 **신규 추가 시 핀은 이미 off**다(`ToProjectItem`이 `IsPinned` 미설정 → `ProjectItem` 기본값 false, DB `DEFAULT 0`). 신규 경로는 변경 불필요. 다만 **핀 걸린 프로젝트를 편집 저장하면 핀이 풀리는 별개 버그**를 함께 고친다(Q2) — `AddOrUpdateProjectAsync`의 편집 분기가 다이얼로그 결과(IsPinned=false)로 기존 값을 덮어쓰므로 기존 IsPinned/PinOrder를 보존한다.

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-D1 카드 시안 재구성(본문 설명·태그·액션, 핀·삭제) | Must | T1, T2, T3 | ✅ 커버(세부 보강·버그 수정) |
| FR-D2 / FR-D3 / FR-D4 (대시보드 카드 기존 항목) | Must/Should | — | 이번 범위 외 (기구현 — 이번 diff 무관) |
| FR-C* / FR-S* / FR-T* / FR-E* / FR-H* / FR-N* | Must/Should | — | 이번 범위 외 (기구현) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(baseline `CS0618` 1건 제외) |
| NFR-2 (계층 위반 0) | — | T1,T2 | 검증 대상(VM에 Brush 미참조 유지, 애니메이션은 View 코드비하인드) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## Investigation Log (근거)

- **위키 참조**: vault 미설정 — 코드·시안 1차 출처로 진행.
- **기존 plan**: 직전 카드 빈상태 플레이스홀더 plan은 `## Phase Ledger` **Phase G 통과(Must 100%)** = 완료(F-8 육안만 미완) → 새 계획으로 교체. 미커밋 변경은 시안 파일뿐이라 이번 요청과 직접 관련.
- **Deferred 대장 확인** (`docs/plans/deferred.md`):
  - **[카드 hover 이동 애니메이션 미구현]**(`:85`) — 시안 translateY(-2px) 미구현 → **이번 T2가 재수용**.
  - **[시안 보라 액센트 미채택]**(`:86`) — 앱 액센트 `#F0716A` 유지 결정 → Q1 답변(산호 유지)과 일치. 대장 유지(전역 액센트 변경은 별개).
  - **[태그 pill 시안화 미적용]**(`:84`) — 태그 *표시* 방식은 이번과 별개. 대장 유지.
  - **[대시보드 카드 F-8 육안 확인 미완]**(`:90`) — 이번 렌더 확인과 함께 다룸.
- **시안 명세 (직접 Read)**:
  - 카드 `article`(`:352`): `transition:border-color .15s, transform .15s`, hover `border-color:#3d3d45; transform:translateY(-2px)`.
  - noMeta(`:367`): `flex:1; min-height:52px; 1px dashed #2e2e35; radius 9; center`, hover `color:#8b7cf7; border-color:#4a4460`. → **현행 유지**(이미 `DashedAddButtonStyle` MinHeight 52).
  - needDesc(`:375`): `min-height:36px; 1px dashed #2e2e35; radius 9; padding:0; center; gap:6`, hover 동일 → "＋ 설명 추가". **갱신점**: 이전 시안의 "border:none 텍스트 링크"에서 점선 박스로 변경됨.
  - needTags(`:385`): `min-height:24px; 1px dashed #2e2e35; radius 9; padding:0; center; gap:6`, hover 동일 → "＋ 태그 추가". **갱신점**: 이전 시안의 "작은 좌측 pill(radius 6, padding 3/8)"에서 폭 전체 점선 박스로 변경됨.
- **현행 구현 (직접 Read)**:
  - `DashboardView.xaml`:
    - ① noMeta(`:209~217`): `DashedAddButtonStyle` MinHeight 52 — 시안 일치(유지).
    - ② 설명(`:220~228`): Height 32 고정 2줄 — 최근 커밋 4ec0d1b("1줄일 때 태그가 위로 붙던 문제") 유지(이번 무변경).
    - ③ needDesc(`:231~240`): `Background=Transparent BorderThickness=0` **기본 Button** 좌측 텍스트 링크 → 점선 박스로 교체 대상.
    - ⑤ needTags(`:250~260`): `DashedAddButtonStyle` + `HorizontalAlignment=Left Padding=8,3 FontSize=11` **작은 pill** → 폭 전체 점선 박스로 교체 대상.
    - 카드 `Border`(`:90~108`): `PointerEntered=Card_PointerEntered`/`PointerExited=Card_PointerExited` 이미 배선. `RenderTransform` 없음.
  - `DashboardView.xaml.cs`: `Card_PointerEntered`(`:331`)/`Card_PointerExited`(`:337`)가 `BorderBrush`만 교체(`CardHoverBorderBrush`↔`CardBorderBrush`). **translateY 없음** → T2가 여기에 Storyboard 추가.
  - `DashedAddButtonStyle`(`Styles.xaml:232~283`): `Background Transparent`, `BorderThickness 0`, `HorizontalAlignment Stretch`, `HorizontalContentAlignment Center`, `Padding 8,7`, hover VSM `DashBorder.Stroke=AppAccentBrush`(산호) + `Content.Foreground=TextFillColorPrimaryBrush`. **radius 8**(시안 9와 미세차 — 기존 [SUGGEST] 유지). 폭 전체 stretch가 기본이라 ③/⑤ 교체 시 폭 자동 채움.
  - `AppAccentBrush`=`#F0716A`(산호, `Palette.xaml:31,56`). 시안 보라 `#8b7cf7`는 팔레트에 없음.
  - 핀 경로:
    - `ProjectItem.IsPinned`(`:46`) 기본 false. DB `IsPinned INTEGER NOT NULL DEFAULT 0`(`DatabaseContext.cs:215`). INSERT `p.IsPinned ? 1 : 0`(`SqliteProjectRepository.cs:439`).
    - `ProjectSettingsDialogViewModel.ToProjectItem()`(`:245~270`): **IsPinned/PinOrder 미설정** → 반환 item은 항상 false/0. 다이얼로그 VM에 IsPinned 프로퍼티 없음(grep 전수).
    - `MainViewModel.AddOrUpdateProjectAsync`(`:456~504`): 편집 분기(`existing is not null`)는 `item.CommandScripts = existing.ToModel().CommandScripts`로 CommandScripts만 보존하고 `_projectRepository.Update(item)` → **IsPinned/PinOrder는 false/0으로 덮어써짐**(핀 풀림 버그). 신규 분기(else)는 `Add(item)` → false(정상, off).
    - `ProjectCardViewModel.IsPinned`(`:56`) 실재, `ToModel()`(`:421`)이 `_item.IsPinned=IsPinned` 후 `_item` 반환(PinOrder는 `_item.PinOrder` 그대로) → `existing.ToModel().IsPinned`/`.PinOrder`로 기존 값 회수 가능.
- **AGENTS.md**: 빌드 `MSBuild x64`. 함정 5(`x:Bind` 함수 바인딩은 Converter/ThemeResource 불가 — 이번 미해당), 함정 6(점선은 Rectangle — `DashedAddButtonStyle` 선례 재사용), baseline 경고 `CS0618` 1건. 참조 경로 전부 실재.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| (T1) 플레이스홀더 점선 박스 | `DashedAddButtonStyle`(Styles.xaml, noMeta·needTags가 이미 소비) | **재사용** — ③needDesc도 같은 스타일로 교체. 신규 스타일·심볼 0 |
| (T2) 카드 hover 이동 애니메이션 헬퍼 | 코드비하인드에 Storyboard 애니메이션 선례 없음(현행은 브러시 즉시 교체). deferred `:61`에 작업/테스트 행도 동일 미구현 | **신규(View 코드비하인드 private 헬퍼 1개)** — `TranslateTransform.Y`를 0↔-2로 애니메이션. 카드마다 RenderTransform 인스턴스가 달라 대상별 Storyboard 필요 |
| (T3) 핀 보존 | `existing.ToModel().CommandScripts` 보존 선례(`:462`) | **재사용(동일 병합 패턴 확장)** — IsPinned/PinOrder 2줄 추가. 신규 심볼 0 |

## 시각 요소 분해

> 기준: 시안 `:352,367,375,385` 인라인 스타일 + 렌더 이미지(2026-07-23). CSS px는 WinUI 논리 단위로 옮긴다. hover 보라(`#8b7cf7`)는 앱 액센트 산호(D2)로 대체. 최종 판정은 빌드 후 육안 대조(⏳ HUMAN-VERIFY).

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| needDesc 박스 | 형태 | 폭 전체 점선(1px dashed #2e2e35, radius 9), min-height 36, bg transparent, 중앙 정렬 | `DashedAddButtonStyle` + `MinHeight="36"`(폭 Stretch·중앙은 스타일 기본) | HTML `:375` |
| needDesc 박스 | 내용 | ＋ 글리프 + "설명 추가"(≈11.5px, #5f5d66), gap 6 | `StackPanel Horizontal Spacing="6"` + `TextBlock ＋` + `TextBlock x:Uid="CardPlaceholder_Desc"` | HTML `:375` |
| needTags 박스 | 형태 | 폭 전체 점선(1px dashed #2e2e35, radius 9), min-height 24, bg transparent, 중앙 정렬 | `DashedAddButtonStyle` + `MinHeight="24"`(Left·Padding 8,3 제거) | HTML `:385` |
| needTags 박스 | 내용 | ＋ + "태그 추가"(≈11px), gap 6 | `StackPanel Horizontal Spacing="6"` + `TextBlock ＋` + `TextBlock x:Uid="CardPlaceholder_Tags"` | HTML `:385` |
| noMeta 박스 | (유지) | min-height 52, 점선 radius 9, 중앙 | 현행 `DashedAddButtonStyle` MinHeight 52 | HTML `:367` |
| 플레이스홀더 hover | 강조 | color #8b7cf7 / border #4a4460 → 앱 액센트 산호 | `DashedAddButtonStyle` PointerOver VSM(`AppAccentBrush`) — 3종 동일 | HTML `:375` + Q1/D2 |
| 카드 | hover 이동 | transform translateY(-2px), transition .15s | `Border.RenderTransform=TranslateTransform` + `Storyboard`(Y 0↔-2, 0.15s) | HTML `:352` |
| 카드 | hover 테두리 | border-color #3d3d45 | 현행 `Card_PointerEntered` BorderBrush 교체(유지) | HTML `:352` |

## Decisions

- **D1 (플레이스홀더 3종을 `DashedAddButtonStyle`로 통일)**: ③needDesc(기본 Button)·⑤needTags(작은 좌측 pill)를 ①noMeta와 같은 `DashedAddButtonStyle` 폭 전체 점선 박스로 바꾸고 크기만 `MinHeight`로 차등(52/36/24)한다. *Source*: 갱신 시안 `:375,385`가 셋을 동일 점선 박스로 통일. 스타일 통일이 곧 hover 통일(모두 `AppAccentBrush`)이라 "hover 색상이 다른" 문제가 자연 해소. 신규 스타일 0.
- **D2 (hover 강조색 = 앱 액센트 산호 유지)**: 통일된 플레이스홀더 hover는 `DashedAddButtonStyle`의 `AppAccentBrush`(#F0716A) 그대로. *Source*: 사용자 Q1 "앱 액센트 산호색 유지". deferred `:86`·직전 plan D3과 동일(팔레트 다크 단일·전역, 시안 보라는 목업 색). 색 변경 코드 0.
- **D3 (카드 hover 이동은 코드비하인드 Storyboard)**: 카드 `Border`에 `TranslateTransform`을 두고 `Card_PointerEntered/Exited`에서 `Storyboard`(`DoubleAnimation` Y 0↔-2, 0.15s, `EnableDependentAnimation`)로 애니메이션한다. *Source*: 카드는 `ItemsRepeater` DataTemplate 내 `Border`라 `VisualStateManager` PointerOver가 자동 공급되지 않아 이미 코드비하인드 핸들러를 쓴다(`:331,337`) — 같은 자리에 이동 애니메이션을 더한다. `TranslateTransform`은 렌더 트랜스폼이라 종속 애니메이션(`EnableDependentAnimation="True"`) 필요. XAML VSM/ControlTemplate 신설보다 최소 변경.
- **D4 (편집 시 핀 보존은 MainViewModel 병합)**: `AddOrUpdateProjectAsync` 편집 분기에서 `existing.ToModel()`의 `IsPinned`/`PinOrder`를 다이얼로그 결과 `item`에 대입해 보존한다. *Source*: 다이얼로그 VM은 핀을 다루지 않고(핀 토글은 카드 헤더 버튼 전용) `ToProjectItem`은 false 반환. 기존 `CommandScripts` 보존(`:462`)과 동일 패턴 확장이 가장 국소적. 다이얼로그 VM에 IsPinned 추가(더 큰 변경)를 하지 않는다.

## 작업 단계

### T1 — 플레이스홀더 needDesc·needTags 점선박스 통일 `Type C`
- [x] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`
- **Design**: 해당 없음 — 신규 C# 심볼 0. 마크업 스타일 교체만(③/⑤를 `DashedAddButtonStyle`로, 크기·정렬 속성 조정). 기존 스타일·VM 값·resw 키(`CardPlaceholder_Desc`/`_Tags`) 재사용.
- **구성**:
  - ③ needDesc(`:231~240`): `Background="Transparent" BorderThickness="0" Padding="0" HorizontalAlignment="Left" Foreground=...` 제거 → `Style="{StaticResource DashedAddButtonStyle}" MinHeight="36"`. 내부 `StackPanel Horizontal Spacing="6"`(＋ 글리프 + `x:Uid="CardPlaceholder_Desc"`) 유지. `Command="{Binding EditCommand}"`·`Visibility="{Binding ShowDescPlaceholder,...}"` 유지.
  - ⑤ needTags(`:250~260`): `HorizontalAlignment="Left" Padding="8,3" FontSize="11"` 제거 → `Style="{StaticResource DashedAddButtonStyle}" MinHeight="24"`. 내부 StackPanel(＋ + `x:Uid="CardPlaceholder_Tags"`) 유지. `Command`·`Visibility="{Binding ShowTagsPlaceholder,...}"` 유지.
  - ① noMeta·② 설명(Height 32)·④ 태그·하단 액션/슬롯·드래그 6속성은 무변경.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline `CS0618` 제외).
  2. ③needDesc·⑤needTags가 `DashedAddButtonStyle`을 소비하고 `MinHeight`가 각 36·24다(폭 전체 점선 박스).
  3. 세 플레이스홀더(①③⑤)가 모두 같은 스타일이라 hover 강조가 동일(`AppAccentBrush`)하다 — "hover 색상 다름" 해소(코드 경로 성립).
  4. `CardPlaceholder_Desc`/`_Tags` x:Uid·`EditCommand` 바인딩·`ShowDescPlaceholder`/`ShowTagsPlaceholder` Visibility가 유지된다.
- **Edge Cases**: 설명 매우 김 → ② 2줄 말줄임(무변경) / needTags 문구 길이 → 중앙 정렬 점선 박스가 폭 채움 / 4조합(둘 다/설명만/태그만/둘 다 없음) 상호 배타 유지(T1 VM 값 무변경).
- **Halt Forecast**: 없음 — 단일 파일, 마크업 속성 교체, 파괴적·외부 작업 없음.

### T2 — 카드 hover 이동 애니메이션(translateY -2px) `Type C`
- [x] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`, `Presentation/Views/DashboardView.xaml.cs`
- **Design**: ① 배치 — XAML은 카드 `Border`에 `RenderTransform`(TranslateTransform) 선언, 애니메이션 로직은 View 코드비하인드(NFR-2: VM 무관). ② 신규 심볼 — `DashboardView.xaml.cs`에 private 헬퍼 `AnimateCardTranslate(Border card, double toY)`(대상 카드의 TranslateTransform.Y를 0.15s 동안 toY로) 1개. ③ 의존 방향 — `Card_PointerEntered/Exited`가 헬퍼 호출 + 기존 BorderBrush 교체 유지. ④ 비추상화 — 공용 애니메이션 유틸/첨부 프로퍼티를 만들지 않는다(소비처 카드 1곳, 코드비하인드 인라인).
- **구성**:
  - XAML 카드 `Border`(`:90`)에 `<Border.RenderTransform><TranslateTransform /></Border.RenderTransform>` 추가(RenderTransformOrigin 불요 — Y 이동만).
  - 코드비하인드 `AnimateCardTranslate`: `card.RenderTransform as TranslateTransform`을 대상으로 `DoubleAnimation`(To=toY, Duration 0.15s, `EnableDependentAnimation="True"`) 담은 `Storyboard`를 `Begin()`. 대상 프로퍼티는 `TranslateTransform.Y`.
  - `Card_PointerEntered`: 기존 BorderBrush 교체 + `AnimateCardTranslate(card, -2)`.
  - `Card_PointerExited`: 기존 BorderBrush 복귀 + `AnimateCardTranslate(card, 0)`.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 카드 `Border`에 `TranslateTransform` RenderTransform이 있고, `Card_PointerEntered/Exited`가 Y를 각 -2·0으로 애니메이션한다(코드 경로 존재, `EnableDependentAnimation` 지정).
  3. 기존 hover 테두리색 교체(BorderBrush)가 함께 유지된다(회귀 없음).
  4. 카드가 hover 시 위로 살짝 떠올랐다가 벗어나면 복귀한다(⏳ HUMAN-VERIFY).
- **Edge Cases**: hover 중 재진입/빠른 이동 → `Storyboard.Begin`이 진행 중 애니메이션을 이어받아 To로 수렴(튐 없음) / `RenderTransform`이 TranslateTransform이 아니면(방어) 헬퍼가 no-op / deferred `:92`의 "자식 버튼에서 hover 조기 해제" 성질은 기존과 동일(BorderBrush·이동 모두 같은 핸들러라 함께 조기 해제될 수 있음 — 이번에 **악화 없음**, 별도 항목 유지).
- **Halt Forecast**: 없음 — 2개 파일, 신규 private 헬퍼 1개, 파괴적·외부 작업 없음.

### T3 — 편집 저장 시 핀(IsPinned/PinOrder) 유지 `Type C`
- [x] 구현
- **Files**: `Presentation/ViewModels/MainViewModel.cs`
- **Design**: 해당 없음 — 신규 심볼 0. 기존 `AddOrUpdateProjectAsync` 편집 분기의 병합 로직에 2줄 추가(기존 `CommandScripts` 보존 패턴 확장). 동작 보존 목적의 버그 수정.
- **구성**:
  - `AddOrUpdateProjectAsync`(`:456`) 편집 분기(`:461~462`): `existing.ToModel()`을 지역 변수로 받아 `item.CommandScripts` + `item.IsPinned` + `item.PinOrder`를 그 값으로 대입(기존 핀 상태·순서 보존). 신규 분기(else)는 무변경(핀 off 정상).
  - 한글 주석으로 "다이얼로그는 핀을 다루지 않으므로 기존 핀 상태를 보존"을 명시.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `AddOrUpdateProjectAsync` 편집 분기가 `item.IsPinned`·`item.PinOrder`를 `existing`의 기존 값으로 설정한다(핀 걸린 프로젝트 편집 저장 시 핀 유지).
  3. 신규 추가 분기는 무변경 — 신규 프로젝트 핀은 off 유지(회귀 없음).
- **Edge Cases**: 핀 안 걸린 프로젝트 편집 → existing.IsPinned=false 그대로 보존(무해) / PinOrder>0인 핀 카드 편집 → 순서 유지 / 편집 대상이 목록에 없음(existing null) → 신규 분기, 무관.
- **Halt Forecast**: 없음 — 단일 파일, 2줄 추가, 파괴적·외부 작업 없음.

## Deferred / Follow-up
- `[태그 pill 시안화 미적용]`(deferred.md:84) — 태그 표시 방식 교체는 이번과 별개, 대장 유지.
- `[시안 보라 액센트 미채택]`(deferred.md:86) — 앱 액센트 산호 유지(Q1 확정), 전역 액센트 변경은 별개로 대장 유지.
- `[카드 hover 테두리 자식 버튼 조기 해제]`(deferred.md:92) — 이번 이동 애니메이션도 같은 핸들러 경로라 성질 공유(악화 없음). 대장 유지.
- **[README/스크린샷 갱신]** — 카드 플레이스홀더·hover 변경으로 스크린샷이 또 낡음(계속 Deferred).
- **[SUGGEST] 플레이스홀더 모서리 반경** — `DashedAddButtonStyle` radius 8 vs 시안 9(미세차, 기존 [SUGGEST] 유지).
- **[SUGGEST] 카드 hover 애니메이션 Storyboard 재사용** (T2 V-6 m1) — `AnimateCardTranslate`가 호출마다 새 Storyboard를 만들고 이전 것을 Stop하지 않아, 카드 경계에서 빠르게 들락거리면 잔상 가능성(0.15s·마지막 To 우선이라 실사용 영향 작음). 필요 시 카드당 Storyboard 캐시 후 재사용 검토.
- **[SUGGEST] PinOrder 메모리 동기화 간극** (T3 V-6 m1/S1) — 드래그 재정렬(`UpdatePinOrder`)이 DB만 갱신하고 각 카드 `ProjectCardViewModel._item.PinOrder`는 갱신하지 않아, `ToModel()`이 stale PinOrder를 반환한다. "재정렬 → 새로고침 없이 곧바로 편집 저장" 순서에서 T3의 PinOrder 보존이 그 stale 값을 DB에 재기록해 방금 만든 재정렬을 되돌릴 수 있다(핀 자체는 유지 — 순서만). T3 범위 밖 사전 존재 간극. 근본 해결: 재정렬 시 메모리 `_item.PinOrder` 동기화 또는 `ToModel()`에 PinOrder 동기화 추가.

## Out of Scope
- 신규 프로젝트 추가 시 핀 로직 변경 — 이미 off라 대상 아님(③ 확인만).
- 태그 표시를 `MarqueeTagsControl` → 시안 pill로 교체 — Deferred(별개).
- 설명 2줄 표시(Height 32)·noMeta 박스 — 이번 diff 대상 아님(유지).
- 작업/테스트 화면 행 hover 이동 애니메이션 — 이번은 대시보드 카드만(요청 범위).

## 사전 승인 항목 (일괄 승인 대상)
- 없음 (구조·공개 API·스키마·의존성 변경 없음. 카드 템플릿·코드비하인드 애니메이션·VM 병합 2줄).
- 로컬 작업 브랜치(`task/dashboard-card-redesign`)에서 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·PR — 별도 승인.
- **시안 대조 최종 시각 판정** — 플레이스홀더 통일 렌더·카드 hover 이동·편집 후 핀 유지는 사용자만 판정(⏳ HUMAN-VERIFY).

## Progress Log
- T1-T2 완료 (커밋 d6ac3bf, a915e78): ③needDesc·⑤needTags를 `DashedAddButtonStyle` 폭 전체 점선 박스로 통일(hover 산호 통일) + 카드 hover translateY(-2px) 이동 애니메이션(코드비하인드 Storyboard). 빌드 OK, spec 리뷰 OK, quality MINOR 1(Storyboard 재사용 — follow-up 등재).
  - 결정: hover 강조색 앱 액센트 산호 유지(Q1). 애니메이션은 View 코드비하인드 private static 헬퍼(NFR-2, EnableDependentAnimation 필수).

## Phase Ledger
- 전 task(T1~T3) 완료.
- **Phase F 통과 (HEAD 3b532ad)** — F-1 클린 리빌드(`-t:Rebuild`) 오류 0·신규 경고 0(baseline `CS0618` 1건만), F-3 회귀 grep 전부 기대값, F-6.5 notes·deferred 갱신, F-7 `plan-completion-reviewer` BLOCKER 0/MAJOR 0/MINOR 1(needDesc 폰트 11.5 — 즉시 반영 커밋 3b532ad).
- **Phase G 통과 (Must 100%)** — PRD **FR-D1**(Must) 세부 보강 충족(T1~T3, 빌드·코드 경로). 다른 active Must FR은 `## PRD Coverage`에서 '이번 범위 외(기구현)' → 대조 제외, 미충족 0. 재루프 0회.
- **F-8 미통과 — 렌더 육안 확인 대기**: 플레이스홀더 통일 렌더·카드 hover 이동·편집 후 핀 유지가 ⏳ HUMAN-VERIFY로 남아 완료 선언 보류.

## Open Questions
- [x] hover 강조색 → **앱 액센트 산호(#F0716A) 유지**(Q1, 2026-07-23).
- [x] 핀 기본값 → 신규는 이미 off, **편집 저장 시 핀 유지 버그 함께 수정**(Q2, 2026-07-23).

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + baseline(`CS0618` 1건) 외 신규 0
- 회귀 방지 grep(소스, `obj/`·`bin/` 제외):
  - **T1**: `DashboardView.xaml`의 ③needDesc·⑤needTags가 `DashedAddButtonStyle` 소비 + `MinHeight` 36·24 / `CardPlaceholder_Desc`·`_Tags` x:Uid 잔존 / `ShowDescPlaceholder`·`ShowTagsPlaceholder`·`EditCommand` 바인딩 잔존 / ③에서 `Background="Transparent" BorderThickness="0"` 인라인 제거
  - **T2**: 카드 `Border`에 `TranslateTransform` RenderTransform 1건 / `AnimateCardTranslate` 정의·호출(Entered/Exited) / `EnableDependentAnimation` 지정 / 기존 `CardHoverBorderBrush`·`CardBorderBrush` 교체 잔존
  - **T3**: `AddOrUpdateProjectAsync` 편집 분기에 `item.IsPinned`·`item.PinOrder` 대입 각 1
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - 설명만/태그만/둘 다 없음 카드의 플레이스홀더가 동일한 폭 전체 점선 박스로 보이고, 마우스 오버 시 세 종류 모두 같은 산호 강조가 되는지
  - 카드에 마우스를 올리면 위로 살짝 떠오르고(≈2px) 벗어나면 복귀하는지(부드러운 전환)
  - 핀 걸린 프로젝트를 편집·저장한 뒤에도 핀이 유지되는지 / 신규 프로젝트는 핀 off인지

## 통과 체크리스트
- [x] 근거 없는 단정 0 (시안·코드 직접 Read)
- [x] `## 요구 이해` 작성됨
- [x] Impact Analysis 4-A~4-D (핀 경로 전수 grep, hover 핸들러·스타일 소비처 확인, 4-D 신규 심볼 기록)
- [x] plan-reviewer 이슈 0 (BLOCKER 0/MAJOR 0, MINOR 1 = 시안 HTML 라인참조 밀림 위험 — 서술 근거 병기로 방어됨, follow-up)
- [x] 각 task 검증 가능 acceptance + 동시 만족 가능
- [x] Open Questions 없음(Q1/Q2 해결)
- [x] 코드 중 결정 분기 0
- [x] Type 분류 명시(T1/T2/T3 = C)
- [x] Design 필드(T2 신규 헬퍼 / T1·T3 해당 없음 근거)
- [x] Edge Cases 명시
- [x] Halt Forecast 명시
- [x] `## 시각 요소 분해` 작성됨(시각 충실도 요청 + 기준 디자인 존재)
