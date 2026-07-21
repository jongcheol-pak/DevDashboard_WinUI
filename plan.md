# plan.md — 테스트 화면(TestPage) 시안 정합 후속 수정 10건

**PRD**: docs/prd.md (FR-E1 상태 필터 탭 / FR-E3 메모 다이얼로그 / FR-E5 목록 restyle — 메모 표시 포함)
**이전 plan**: 테스트 화면·등록 다이얼로그 시안 정합 재구현(T1~T5) — Phase G 통과(Must 100%), **F-8(시각 육안 확인) 대기 상태로 종결**. 이번 요청이 그 F-8 확인 결과다.

## 요구 이해

> 원문(사용자, 2026-07-21): "[1번 디자인, 2번 현재 구현된 디자인]
> - 항목 사이에 라인이 없음
> - 항목 오른쪽에 표시되는 실패, 미실행, 통과 배지 크기가 동일해야 하는데 다름
> - 헤더(UI.UX 표시 부분)에 컨텍스트 메뉴가 있는데 필요 없음, 항목이 없으면 해당 카테고리 카드는 표시하지 않음.
> - 메모를 추가한 경우 디자인 화면 처럼 표시되지 않음
> - 필터(전체,통과,실패, 미실행)에서 선택하면 디자인 처럼 글자가 두껍게 표시되어야 하는데 색상만 변경 되는거 같음.
> - 필터(전체,통과,실패, 미실행)에서 전체 필터가 동작 하지 않음. 미실행 클릭 후 전체 클릭하면 미실행 항목만 표시 됨.
> - 항목 헤더의 오른쪽에 표시되는 그래프에서 세로 사이즈가 다른데 확인해서 수정
> - 목록에 마우스 오버시 색생 번경 되는데 적용 안된거 같음
> - 항목에서 앞부분에 통과,미실행,실패 상태를 표시하는 아이콘이 있는데 원으로 구현되어 있는데 디자인하고 다름 확인해서 수정
> - [3번 이미지]처럼 메모 입력 화면이 구현되어야 하는데 다르게 구현되어 있음 동일하게 화면 수정."

이해한 요구:
- **① 목록 외형을 시안(이미지1)에 맞춤**: 테스트 행 사이 구분선 추가, 상태 pill 폭 통일, 상태 아이콘을 원형 → **라운드 사각형**, 행 마우스 오버 배경색, 메모가 있으면 **행 아래 인용 블록**(좌측 컬러 바 + 깃발 아이콘 + 메모 텍스트)으로 표시.
- **② 스위트 그룹 정리**: 그룹 헤더의 우클릭 메뉴(이름수정/삭제)를 **기능째 제거**(사용자 확정), 표시할 항목이 없는 스위트 카드는 **아예 표시하지 않음**(전체 필터에서도).
- **③ 진행바 세로 크기 불일치 해소**: 통과율에 따라 바 두께가 달라 보이는 문제를 두께 고정으로 해소.
- **④ 상태 필터 탭 결함 2건**: 선택 탭 글자를 **굵게**(현재는 색만 변함), **"전체" 탭이 필터를 해제하지 못하는 버그** 수정.
- **⑤ 메모 입력 다이얼로그를 시안(이미지3)대로 재구성**: 제목 "메모 추가" + 대상 테스트 이름 + 안내 placeholder + 취소/저장.

## Goal

이전 plan의 F-8(육안 확인)에서 드러난 시안 불일치 8건과 기능 결함 2건(전체 필터 미동작·탭 굵기 미표시)을 해소해, `TestPage`의 목록·필터·메모 경로가 시안(이미지1·3)과 일치하고 필터가 정상 동작하게 한다.

## Investigation Log (근거)

### 문서·대장
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- **AGENTS.md 신선도 점검**: 이번에 참조하는 경로(`Presentation/Views/TestPage.xaml(.cs)`·`Views/Dialogs/`·`ViewModels/TestPageViewModel.cs`·`Resources/{Styles,Palette}.xaml`·`Strings/{ko-KR,en-US}/Resources.resw`) 전부 실재. **함정 2(ko/en 양쪽)·3(resw 키 형식)·5(x:Bind 함수 바인딩은 Brush/Visibility 직접 반환)·7(RadioButton 커스텀 템플릿 GoToState 분리)·11(공용 DataTemplate 소비처 확인)** 직결 — 아래 반영. 어긋남 0건.
- **Deferred 대장(`docs/plans/deferred.md`) 확인**: 관련 항목 — `[테스트 행에서 방법·메모 미표시]`(이전 plan Deferred) 중 **메모 몫을 이번에 재수용**(T2), 방법(Method) 몫은 잔여 유지. `[Test* 구 resw 고아 정리]`(대장)에 이번에 고아가 되는 키(`TestEditNoteTitle`·`TestEditCategoryTitle`)를 합류시킨다(정리 자체는 이번 범위 밖). `[실패 색 시안 불일치]`(대장 마지막 줄) — 사용자가 이번에 언급하지 않아 **호박색 유지**. 나머지 대기 항목은 무관.
- **PRD 경량 확인**: 이번 변경은 **FR-E1**(상태 필터 탭 — 전체 필터 결함·선택 표시)·**FR-E3**(메모 다이얼로그)·**FR-E5**(목록 restyle — 스위트 그룹·상태 아이콘/배지·**메모 표시**)에 닿는다 → PRD 연결(Phase G 활성). FR-E5는 이전 plan에서 "메모 표시" 몫을 시안 반영으로 제외했었는데, 이번 요구 ①로 **다시 커버 대상이 된다**.

### 현행 구현 (직접 Read)

**`TestPage.xaml`**
- `TestItemRowTemplate`(:19-85): 행 Grid 1행 3열(상태 아이콘 Border / 이름 / 상태 pill). **구분선 없음, 메모 표시 없음, hover 없음.**
  - 상태 아이콘(:52-61): `Width/Height=22`, **`CornerRadius="11"`(원형)**, 배경 `StatusSoftBrush`, 글리프 색 `StatusBrush`.
  - 상태 pill(:72-83): `TagBadgeStyle`(CornerRadius 4·Padding 8,3) + 배경 `StatusSoftBrush`. **폭 지정 없음** → 텍스트 길이("통과" 2자 vs "미실행" 3자)만큼 폭이 달라진다 = 지적 ②의 원인.
- `TestSuiteGroupTemplate`(:91-147): 헤더에 `Grid.ContextFlyout`(이름수정/삭제 MenuFlyout, :99-115) — 제거 대상. 우측 `ProgressBar Value={PassRate} Width=120`(:137-141).
- 상태 필터 탭바(:254-295): `RadioButton Style=FilterTabStyle`, **전체 탭만 `Tag=""`**(:255), 나머지 `Tag="Pass"/"Fail"/"Untested"`.
- 그룹 목록(:298-301): `ItemsControl`(`ListView` 아님 → 기본 hover 없음).
- **소비처 전수**: `TestItemRowTemplate`·`TestSuiteGroupTemplate` 모두 이 파일 안에서만 참조(grep — `:144`, `:300`). 함정 11(공용 템플릿) 해당 없음.

**`TestPage.xaml.cs`**
- `StatusTab_Checked`(:106-111): `if (sender is not RadioButton { Tag: string tag }) return;` → `Vm.SelectedStatus = string.IsNullOrEmpty(tag) ? null : tag;`
  - **⑥ "전체 필터 미동작"의 가장 유력한 정적 설명**: 전체 탭의 `Tag=""`가 패턴 `Tag: string tag`에 걸리지 못하면(빈 attribute가 `null`로 파싱되는 경우) **`return`으로 조용히 빠져나가 `SelectedStatus`가 직전 값("Untested") 그대로 남는다** — 사용자 증상(미실행 클릭 후 전체를 눌러도 미실행만 표시)과 정확히 일치한다. RadioButton 자체는 체크되므로 밑줄·색은 바뀐다(사용자가 "색상만 변경"이라 관찰한 것과도 정합).
  - 반증 자료 확인: 레포 전체에서 `Tag=""`는 이 한 곳뿐(grep 전수) — 정상 동작하는 선례가 없어 이 가설을 반증할 근거가 없다. VM 경로(`OnSelectedStatusChanged`→`Rebuild`, `SelectedStatus is null`이면 전 항목 표시)·`Checked` 배선·`SuiteGroups` 갱신은 코드상 정상임을 직접 확인했고, 이 외에 증상을 설명하는 정적 경로를 찾지 못했다.
  - **가설 한계(중요)**: `Tag=""`가 `string.Empty`로 파싱된다면 기존 코드가 이미 정상이고 D1의 수정은 **동작상 no-op**가 된다(그 경우 원인은 코드 밖 — 런타임 재조사 필요). 파싱 결과는 정적으로 확인 불가하므로, T1의 grep acceptance만으로 "고쳐졌다"고 단정하지 않는다 — 실동작 확인은 ⏳ HUMAN-VERIFY이며, 재현이 남으면 **가설 기각 → 런타임 재조사**로 넘긴다(아래 D1·Deferred).
  - **수정 방향(원인 제거 + 경로 견고화)**: 전체 탭 Tag를 빈 문자열이 아닌 명시값(`"All"`)으로 바꾸고, 핸들러도 `Tag`를 `as string`으로 받아 **null·빈 문자열·"All"을 모두 전체로** 해석한다. 어느 파싱 결과여도 전체 필터가 성립한다.
- 상태 브러시·글리프 헬퍼(:32-93): `StatusBrush`/`StatusSoftBrush`/`StatusGlyph`(✓/✕/○)/`StatusText` 존재 — 아이콘 재구성에 재사용.
- `EditNote_Click`(:172-195): **인라인 `ContentDialog` + 맨 TextBox**(Title=`TestEditNoteTitle`="진행 내용 수정"). 시안(이미지3)의 대상 테스트명·안내 placeholder 없음 = 지적 ⑩.
- `RenameSuite_Click`(:199-215)·`DeleteSuite_Click`(:217-225): 헤더 메뉴 제거 시 고아가 된다 → 함께 제거.

**`TestPageViewModel.cs`**
- `Rebuild()`(:65-101): `if (items.Count == 0 && SelectedStatus is not null) continue;` — **전체 필터에서는 빈 스위트도 노출**한다 = 지적 ③ 후반의 원인. 조건에서 상태 필터 종속을 빼면 해소.
- `RenameSuite`(:120-129)·`DeleteSuite`(:132-138): 소비처는 위 두 핸들러뿐(grep 전수) → 함께 제거.
- `AddSuite`(:106-117): **이미 소비처 0(고아)** — 이번 변경으로 생긴 것이 아니므로 이번 범위 밖(Deferred 등재).

**`Resources/Styles.xaml`**
- `FilterTabStyle`(:371-438): `CheckStates/Checked`가 `SelectedUnderline.Background`와 `Content.Foreground`만 설정 — **FontWeight 없음** = 지적 ⑤의 원인. 함정 7대로 CommonStates(호버)와 요소가 분리돼 있어 `Content.FontWeight` 추가는 이 구조를 깨지 않는다. 소비처는 TestPage 상태 탭 4곳뿐(grep 전수).
- `TagBadgeStyle`(:189-193): CornerRadius 4·Padding 8,3·Margin 0,0,4,4 — pill·배지 공용(소비처 다수) → **스타일 자체는 건드리지 않고** 소비 지점에서 `MinWidth`를 준다.

**`Resources/Palette.xaml`** (다크 `Default` 단일 — 함정 4)
- 사용 가능: `AppBorderBrush #26262C`(구분선)·`AppBorderStrongBrush #2E2E34`(진행바 트랙)·`SubtleFillColorTertiary #26262C`(행 hover)·`AppWarningBrush #D9954A`(메모 강조)·`AppInputBrush`(메모 블록 배경)·`AppInfoBrush #5B93D8`(다이얼로그 부제)·`AppAccentBrush #F0716A`.
- **`AppHoverFillColor`는 있으나 대응 Brush가 없다**(`AppHoverFillBrush` 미정의 — grep 확인) → hover에는 정의된 `SubtleFillColorTertiary`를 쓴다.

**진행바 두께 불일치(⑦)의 원인**: WinUI `ProgressBar`는 **트랙(배경)과 인디케이터의 높이가 서로 다른 기본 디자인**이라, 통과율 0%인 스위트는 얇은 트랙 선만, 통과율이 있는 스위트는 두꺼운 인디케이터가 보여 "세로 사이즈가 다른" 것으로 관찰된다(스크린샷2의 UI·UX 두꺼운 바 vs 프론트엔드 얇은 선). 컨트롤 `Height` 지정이 트랙·인디케이터 중 어디에 걸리는지는 템플릿 내부 구현에 의존하므로, **직접 그린 2겹 Border**(트랙 + 좌측 정렬 인디케이터, 둘 다 Height 4)로 대체해 결과를 확정한다(함정 5와 같은 "직접 반환/직접 그림" 방침).

**resw 현황**(`Strings/ko-KR/Resources.resw`, ko/en 대칭 확인): `TestEditNoteTitle`="진행 내용 수정"(:385)·`TestEditCategoryTitle`(:390)·`TestStatus_*`(:408-411)·`TestSuitePassCount`(:405)·`Dialog_Save`/`Dialog_Cancel`(공용) 존재. 신규 필요: 메모 다이얼로그 제목·placeholder.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| `TestNoteDialog`(메모 다이얼로그) | `TestEditDialog`(ContentDialog 서브클래스 + `ShowAsync` 섀도잉 + `InputLabelStyle` 라벨) | **신규(패턴 복제)** — 기존은 인라인 `ContentDialog`라 시안의 부제·placeholder를 담을 구조가 없다. 레포 관례(다이얼로그 = ContentDialog 서브클래스, AGENTS.md)에 맞춰 파일로 분리 |
| `NoteVisibility(string)` | 이전 plan T5에서 제거된 동명 헬퍼(현재 부재), `TaskPage`에 유사 Visibility 헬퍼 선례 | **신규(선례 재현)** — 함정 5(x:Bind 함수 바인딩은 Converter 불가)로 `Visibility` 직접 반환 |
| `IndicatorWidth(double)` | 없음(현행은 `ProgressBar.Value` 바인딩) | **신규(정적 헬퍼)** — 커스텀 진행바의 인디케이터 폭 계산 |
| `StatusIconBackground`/`StatusIconBorderBrush`/`StatusGlyphForeground` | `StatusBrush`·`StatusSoftBrush` 존재하나 "채움 + 흰 글리프 / 테두리형" 구분이 없음 | **재사용 + 신규** — 기존 브러시 재사용, 아이콘 3요소(배경·테두리·글리프색) 판정만 신규 헬퍼 |
| 행 hover 배경 | `FilterTabStyle`은 ControlTemplate VisualState 방식(DataTemplate에는 적용 불가) | **신규(이벤트 핸들러)** — `DataTemplate` 안에서는 VSM `GoToState`가 동작하지 않으므로 `PointerEntered`/`PointerExited`로 명시 처리 |
| 행 구분선 | `SeparatorStyle`(Rectangle, `Styles.xaml:196`) | **미사용(재사용 검토 결과)** — 행 루트 Border의 `BorderThickness="0,1,0,0"`로 충분(요소 추가 없음) |

## 시각 요소 분해

> 기준 디자인: 사용자 제공 렌더 이미지 — 이미지1(테스트 목록 시안)·이미지3(메모 다이얼로그 시안). HTML/CSS 소스는 없으므로 **px 수치는 시각 판독 근사**이며 구조·형태가 기준이다. 최종 판정은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY).

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|---|---|---|---|
| 테스트 행 | 행 사이 구분선 | 있음(얇은 1px, 어두운 회색). 헤더와 첫 행 사이에도 있음 | 이미지1 판독 |
| 테스트 행 | 마우스 오버 | 배경이 살짝 밝아짐 | 사용자 요구(⑧) — 시안은 정적 이미지라 hover 미포함 |
| 상태 아이콘 | 형태 | **라운드 사각형**(모서리 둥근 정사각, 약 22px) | 이미지1 + 사용자 확정(Q1: 세 상태 모두 라운드 사각형) |
| 상태 아이콘 | 통과 | 파란 채움 + **흰색** ✓ | 이미지1 |
| 상태 아이콘 | 실패 | 호박 채움 + **흰색** ✕ | 사용자 확정(Q1 preview `[■✕]`) |
| 상태 아이콘 | 미실행 | 저투명 배경 + 회색 테두리 + 회색 ○ | 이미지1 + Q1 preview `[□○]` |
| 상태 pill | 폭 | **세 상태 동일**("미실행" 3자 기준 고정 폭) | 사용자 요구(②), 이미지1 |
| 상태 pill | 텍스트 정렬 | 가운데 | 이미지1 |
| 메모 블록 | 위치 | 테스트 이름 **아래**, 이름 열에 정렬(아이콘 열 들여쓰기 유지) | 이미지1(결제취소_웹훅_서명검증) |
| 메모 블록 | 좌측 표시 | 세로 컬러 바(주황 계열) | 이미지1 |
| 메모 블록 | 아이콘 | 깃발(⚑) 1개, 텍스트 앞 | 이미지1 |
| 메모 블록 | 배경 | 행보다 어두운 입력칸 계열 박스, 모서리 둥금 | 이미지1 |
| 메모 블록 | 표시 조건 | 메모가 있을 때만 | 이미지1(다른 두 행에는 없음) |
| 스위트 진행바 | 두께 | **통과율과 무관하게 일정**(얇은 막대) | 이미지1(백엔드 2/3) + 사용자 요구(⑦) |
| 스위트 진행바 | 인디케이터 색 | 파랑(통과색 계열) | 이미지1 |
| 스위트 헤더 | 우클릭 메뉴 | 없음 | 사용자 요구(③) |
| 스위트 카드 | 표시 조건 | 표시할 항목이 있을 때만 | 사용자 요구(③) |
| 필터 탭 | 선택 시 글자 | **굵게** + 밝은 색 + 밑줄 | 이미지1("전체"가 굵고 밑줄) |
| 필터 탭 | 비선택 글자 | 보통 굵기, 저강도 회색 | 이미지1 |
| 메모 다이얼로그 | 제목 | "메모 추가"(굵게) | 이미지3 |
| 메모 다이얼로그 | 부제 | 대상 테스트 이름, 파랑 계열, 제목 아래 | 이미지3 |
| 메모 다이얼로그 | 입력칸 | 여러 줄, placeholder "메모를 입력하세요. 비워 두고 저장하면 메모가 삭제됩니다." | 이미지3(원문 그대로) |
| 메모 다이얼로그 | 버튼 | 좌 "취소"(보통) / 우 "저장"(accent 채움), 우측 정렬 | 이미지3 |

## Decisions

- **D1 (전체 필터 수정 방식)**: 전체 탭 `Tag="All"` + 핸들러에서 `Tag as string`으로 받아 null·빈 문자열·"All"을 모두 전체로 해석. *Source*: `TestPage.xaml:255`, `TestPage.xaml.cs:106-111`. 어느 파싱 결과여도 전체 필터가 성립한다. **가설이 틀린 경우의 경로**: 사용자 확인에서 증상이 그대로 재현되면 Tag 가설을 기각하고 런타임 재조사(`pjc:pjc-systematic-debugging`)로 전환한다 — 이번 plan 안에서 추가 추측 수정을 하지 않는다.
- **D2 (탭 굵기)**: `FilterTabStyle`의 `CheckStates/Checked`에 `Content.FontWeight = SemiBold` Setter 추가. *Source*: `Styles.xaml:421-429` — 같은 요소의 다른 프로퍼티라 CommonStates(호버)와 충돌하지 않는다(함정 7 구조 유지). **부작용 차단**: `Content`(ContentPresenter)의 FontWeight는 자식에 상속되므로 개수 배지 숫자(`TestPage.xaml:261` 등 4곳, FontSize 10)까지 굵어진다 — 시안(이미지1)의 배지 숫자는 굵지 않으므로 배지 TextBlock에 `FontWeight="Normal"`을 명시해 라벨만 굵어지게 한다.
- **D3 (상태 아이콘)**: 세 상태 모두 `CornerRadius="6"` 22×22. 통과·실패는 상태색 **채움 + 흰 글리프**, 미실행은 **저투명 배경 + 회색 테두리 + 회색 ○**. *Source*: 사용자 Q1 답변(2026-07-21).
- **D4 (pill 폭 통일)**: 소비 지점에 `MinWidth="52"` + 텍스트 `TextAlignment="Center"`. 공용 `TagBadgeStyle`은 무변경(다른 화면 영향 차단). "미실행"(11px 3자 ≈ 33px) + Padding 16 = 49px가 최댓값이라 52px면 세 상태 모두 넘치지 않는다.
- **D5 (구분선 범위)**: 행 루트 Border `BorderThickness="0,1,0,0"`, 색 `AppBorderBrush`. 첫 행 상단에도 선이 생겨 **헤더와 목록 사이 구분선**이 함께 만들어진다(이미지1과 일치). 마지막 행 아래에는 선이 없어 카드 하단 경계와 겹치지 않는다.
- **D6 (진행바)**: `ProgressBar` → **Grid(Width 120, Height 4) 안에 트랙 Border + 좌측 정렬 인디케이터 Border**. 인디케이터 색은 통과색(`PassBrush` — 이미지1의 파란 바), 트랙은 `AppBorderStrongBrush`. 폭은 정적 헬퍼 `IndicatorWidth(PassRate)`. *Source*: 이미지1 + 위 "진행바 두께 불일치 원인".
- **D7 (스위트 조작 제거 범위)**: 헤더 `ContextFlyout` + `RenameSuite_Click`/`DeleteSuite_Click` 핸들러 + VM `RenameSuite`/`DeleteSuite` 메서드까지 제거. *Source*: 사용자 Q2 답변(2026-07-21) — 사용자가 "기능째 제거"를 확정했다.
  - **사실 확인(정정)**: `TestCategory`는 등록 시 작업 카테고리 **이름을 복사해 프로젝트 DB에 별도로 생성**되므로, 앱 설정에서 작업 카테고리를 고치거나 지워도 기존 `TestCategory`에는 반영되지 않는다(`AddTestToSuite`, `TestPageViewModel.cs:143-161`). 따라서 "앱 설정에서 관리된다"는 정당화는 성립하지 않는다 — 제거의 근거는 사용자 확정이며, 결과적으로 **오타·구 이름 스위트를 정리할 UI가 0이 된다**(T3의 빈 스위트 숨김으로 화면에서는 사라지되 DB에는 잔존). 이 잔존 문제는 Deferred `[스위트 정리 경로 부재]`로 등재한다.
  - `AddSuite`는 이전부터 고아라 이번 범위 밖(Deferred).
- **D8 (행 hover 구현)**: `DataTemplate` 안에서는 VisualStateManager `GoToState`가 동작하지 않으므로 행 루트 Border의 `PointerEntered`/`PointerExited` 핸들러로 `Background`를 바꾼다. 색은 `UserControl.Resources`에 `RowHoverBrush`(Color=`SubtleFillColorTertiary`)를 두고 코드에서 `Resources["RowHoverBrush"]`로 가져온다(팔레트 색 유지 + `Application.Current.Resources` 경유 ThemeDictionaries 조회의 불확실성 회피).
- **D9 (메모 블록 스타일)**: 배경 `AppInputBrush`, 좌측 `BorderThickness="2,0,0,0"` + `AppWarningBrush`, 깃발 아이콘(Segoe Fluent `&#xE7C1;`) + 텍스트 `TextFillColorSecondaryBrush` 12px, `CornerRadius="4"`. 표시 여부는 `NoteVisibility(ProgressNote)`.
- **D10 (메모 다이얼로그 구조)**: 신규 `Presentation/Views/Dialogs/TestNoteDialog.xaml(.cs)` — ContentDialog 서브클래스, 생성자 `(string testName, string currentNote)`, 결과는 `ResultNote`. 제목·버튼은 `TestEditDialog`와 같은 방식(코드비하인드 `LocalizationService.Get`), 부제는 `AppInfoBrush`. 시안 문구 "비워 두고 저장하면 메모가 삭제됩니다"는 **현재 동작과 일치**(`EditProgressNote`가 빈 문자열을 그대로 저장) — 동작 변경 없음.
- **D11 (신규 resw 키)**: `TestNote_Title`("메모 추가")·`TestNote_Placeholder`("메모를 입력하세요. 비워 두고 저장하면 메모가 삭제됩니다.") ko/en 양쪽(함정 2). 코드 소비이므로 **접미 없는 베어네임**(함정 3). 구 `TestEditNoteTitle`·`TestEditCategoryTitle`은 고아가 되지만 삭제는 대장의 고아 정리 항목에 합류(이번 범위 밖).
- **D12 (실패 색)**: 호박 `#E8B45A` 유지(PRD §3). 시안 이미지1에 실패 예시가 없고 사용자가 이번에 언급하지 않음 — 대장의 `[실패 색 시안 불일치]` 항목 그대로 유지.

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-E1 (테스트 전체 페이지 + 통계 카드 + 상태 필터 탭) | Must | T1 | ✅ 커버(필터 탭 결함 수정 몫) |
| FR-E3 (등록/편집·**메모**·삭제 확인 다이얼로그) | Must | T4 | ✅ 커버(메모 다이얼로그 몫) |
| FR-E5 (목록 restyle — 스위트 그룹·통과율 바·상태 아이콘/배지·**에러/메모 표시**) | Should | T2, T3 | ⚠️ **부분 커버** — 스위트 그룹·통과율 바·상태 아이콘/배지·**메모 표시**(이전 plan에서 제외했던 몫) 충족. **"에러 표시"는 미착수**: `TestItem`에 에러 필드가 없고 도메인 변경은 이번 Out of Scope, 시안(이미지1)에도 에러 표현이 없다 → Deferred `[FR-E5 에러 표시 미착수]`로 이연(F-7 리뷰 M1) |
| FR-E2 (테스트 상태 모델 통과/실패/미실행) | Must | — | 이번 범위 외 (기구현 — 상태 모델 무변경) |
| FR-E4 / FR-T8 (테스트↔작업 연결·칸반 통과율 배지) | Should | — | 이번 범위 외 (기구현, 실동작 확인은 사용자) |
| FR-T*·FR-C*·FR-S*·FR-H*·FR-N* 등 그 외 active Must | Must/Should | — | 이번 범위 외 (기구현, 이번 diff 무관) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(기존 경고 5건 제외) |
| NFR-2 (계층 위반 0) | — | 전 task | 검증 대상(VM→페이지 역참조 없음) |
| NFR-3 (DB 스키마 하위호환) | — | — | ✅ 무영향(도메인·스키마·직렬화 무변경) |
| NFR-4 (다국어 ko/en 대칭) | — | T4 | 신규 resw 키 ko/en 양쪽(함정 2) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## 작업 단계

### T1 — 상태 필터 탭: "전체" 필터 정상화 + 선택 탭 굵게 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`(탭 Tag), `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`(`StatusTab_Checked`), `DevDashboard_WinUI/Resources/Styles.xaml`(`FilterTabStyle`)
- **구성**:
  - (D1) 전체 탭 `Tag=""` → `Tag="All"`. 핸들러를 `if (sender is not RadioButton rb) return; var tag = rb.Tag as string; Vm.SelectedStatus = string.IsNullOrEmpty(tag) || tag == "All" ? null : tag;`로 교체(빈 Tag가 null로 파싱돼도 전체로 해석되도록 — 원인 제거 + 경로 견고화).
  - (D2) `FilterTabStyle`의 `CheckStates/Checked`에 `<Setter Target="Content.FontWeight" Value="SemiBold" />` 추가 + 탭 4개의 개수 배지 TextBlock에 `FontWeight="Normal"` 명시(상속으로 배지 숫자까지 굵어지는 것 차단 — 시안의 배지 숫자는 굵지 않다).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(기존 5건 제외).
  2. `TestPage.xaml`에 `Tag=""` 잔존 0건, `Tag="All"` 1건.
  3. `StatusTab_Checked`에 `Tag: string` 패턴 매칭이 없고 `as string` + null/빈문자열/"All" 분기가 있다.
  4. `Styles.xaml`의 `CheckStates/Checked`에 `Content.FontWeight` Setter가 있고, `CommonStates`는 여전히 `HoverBackground`만 건드린다(함정 7 회귀 없음).
  5. 탭 4개의 개수 배지 TextBlock에 `FontWeight="Normal"`이 있다.
  > ⚠️ 이 acceptance는 전부 마크업 존재 확인이라 **"전체 필터가 실제로 동작하는가"를 검증하지 못한다**(D1 가설 한계). 실동작은 ⏳ HUMAN-VERIFY이며, 재현되면 가설 기각 후 런타임 재조사로 전환한다.
- **Edge Cases**: XAML 초기 로드 시 전체 탭 `IsChecked="True"` 발화(Vm은 `InitializeComponent` 전에 대입되므로 non-null — 기존 주석 근거 유지) / Tag가 null인 경우 / 같은 탭 재클릭(Checked 미발화 — 상태 유지가 정상).
- **Halt Forecast**: 없음 — `FilterTabStyle` 소비처는 TestPage 상태 탭 4곳뿐(grep 전수), 파괴적·외부 작업 없음.

### T2 — 테스트 항목 행 재구성: 구분선·pill 폭·상태 아이콘·hover·메모 표시 `Type D`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`(`TestItemRowTemplate` + `UserControl.Resources`), `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`(아이콘 헬퍼·`NoteVisibility`·hover 핸들러)
- **Design**: ① 배치 — 전부 `TestPage`(뷰 국소, Presentation). ② 신규 심볼 — `NoteVisibility(string note)`(메모 유무 → `Visibility` 직접 반환) / `StatusIconBackground(string status)`(통과·실패는 상태색 채움, 미실행은 저투명) / `StatusIconBorderBrush(string status)`(미실행만 회색 테두리, 나머지 투명) / `StatusGlyphForeground(string status)`(통과·실패 흰색, 미실행 회색) / `Row_PointerEntered`·`Row_PointerExited`(행 배경 토글). ③ 의존 방향 — XAML `x:Bind` 함수 바인딩이 코드비하인드 정적 헬퍼를 참조(함정 5), 헬퍼는 기존 `_passBrush`/`_failBrush`/`_untestedBrush`·`StatusSoftBrush`만 참조하며 외부 의존 없음. ④ 비추상화 — 상태색을 `Palette.xaml`로 이관하거나 상태별 스타일 리소스를 만들지 않는다(현행 코드비하인드 고정 방식 유지 — 이관은 Deferred `[SUGGEST]`).
- **구성**:
  - (D5) 행 루트를 `Border`로 감싸 `BorderThickness="0,1,0,0"` + `BorderBrush={ThemeResource AppBorderBrush}`, 내부에 기존 Grid. `ContextFlyout`은 이 Border로 옮긴다(행 전체 우클릭 유지).
  - (D3) 상태 아이콘 `CornerRadius="11"` → `"6"`, 배경/테두리/글리프색을 신규 헬퍼로 교체(`BorderThickness="1.5"`).
  - (D4) 상태 pill에 `MinWidth="52"`, 내부 TextBlock `TextAlignment="Center"` + `HorizontalAlignment="Stretch"`.
  - (D9) 행 Grid를 2행으로 확장 — 1행 이름, 2행 메모 블록(이름 열에 배치, `Visibility={x:Bind local:TestPage.NoteVisibility(ProgressNote), Mode=OneWay}`). 상태 아이콘·pill은 `Grid.RowSpan="2"` + 세로 중앙.
  - (D8) 행 루트 Border에 `PointerEntered`/`PointerExited` 배선, `UserControl.Resources`에 `RowHoverBrush` 추가. **내부 Grid의 `Background="Transparent"`는 유지**하고(빈 영역까지 hit-test 되어야 행 전체 hover가 성립), Exited 시 루트 Border 배경은 `null`이 아니라 **`Transparent`로 복원**한다.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `TestItemRowTemplate`에 `CornerRadius="11"` 잔존 0건, 상태 아이콘에 `CornerRadius="6"` 1건.
  3. 상태 pill에 `MinWidth` 지정이 있고 `TagBadgeStyle` 정의(`Styles.xaml:189-193`)는 무변경.
  4. `TestPage.xaml.cs`에 `NoteVisibility`·`StatusIconBackground`·`StatusIconBorderBrush`·`StatusGlyphForeground`·`Row_PointerEntered`·`Row_PointerExited`가 정의되고 전부 XAML에서 소비된다(고아 0).
  5. 메모 블록이 `ProgressNote`에 `Mode=OneWay`로 바인딩돼 메모 편집 후 갱신된다.
- **Edge Cases**: 메모가 빈 문자열·공백만 → 블록 미표시 / 메모가 매우 김 → `TextWrapping="Wrap"`으로 행 높이 증가(잘라내지 않음 — 시안이 전체 표시) / 이름 2줄 + 메모 동시 → 아이콘·pill 세로 중앙 유지 / 스위트에 항목이 1개여도 상단 구분선은 그려짐(헤더 아래 선 — 의도) / pill 클릭(`Tapped`)이 행 hover·우클릭과 충돌하지 않음(기존 `e.Handled=true` 유지).
- **Halt Forecast**: (i) 사전 해소 — `TestItemRowTemplate` 소비처가 TestPage 1곳임을 grep 전수 확인해 함정 11 리스크 제거. 그 외 파괴적·외부 의존 없음.

### T3 — 스위트 그룹 헤더: 진행바 두께 고정 + 우클릭 메뉴·빈 스위트 제거 `Type D`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`(`TestSuiteGroupTemplate`), `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`(`IndicatorWidth` 추가, `RenameSuite_Click`·`DeleteSuite_Click` 제거), `DevDashboard_WinUI/Presentation/ViewModels/TestPageViewModel.cs`(`Rebuild` 조건, `RenameSuite`·`DeleteSuite` 제거)
- **Design**: ① 배치 — 진행바는 `TestSuiteGroupTemplate` 안에 인라인(스위트 그룹 전용), 폭 계산 헬퍼는 `TestPage` 코드비하인드. ② 신규 심볼 — `IndicatorWidth(double passRate)`(통과율 → 인디케이터 픽셀 폭, `ProgressBarWidth` 상수 120 기준). ③ 의존 방향 — XAML이 헬퍼를 참조, 헬퍼는 상수만 참조. VM 변경은 `Rebuild` 내부 조건 한 줄 + 메서드 2개 삭제로 외부 계약 무변경(`TestSuiteGroup` record 그대로). ④ 비추상화 — 재사용 가능한 "진행바 컨트롤/스타일"을 만들지 않는다(소비처 1곳, YAGNI).
- **구성**:
  - (D6) `ProgressBar` → `Grid Width=120 Height=4` + 트랙 `Border`(`AppBorderStrongBrush`, CornerRadius 2) + 인디케이터 `Border`(`PassBrush`, `HorizontalAlignment="Left"`, `Width={x:Bind local:TestPage.IndicatorWidth(PassRate)}`).
  - (D7) 헤더 `Grid.ContextFlyout` 제거, `RenameSuite_Click`·`DeleteSuite_Click`·VM `RenameSuite`·`DeleteSuite` 제거.
  - (③) `Rebuild()`의 `if (items.Count == 0 && SelectedStatus is not null) continue;` → `if (items.Count == 0) continue;` (주석도 새 동작에 맞게 갱신).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `TestPage.xaml`에 `ProgressBar` 잔존 0건, `TestSuiteGroupTemplate`에 `ContextFlyout` 잔존 0건.
  3. `RenameSuite`·`DeleteSuite`(VM·코드비하인드 양쪽) 잔존 0건 — 레포 전체 grep(`obj/` 제외).
  4. `TestPageViewModel.Rebuild`에 `SelectedStatus is not null`을 포함한 빈 스위트 조건이 없고 `items.Count == 0` 단독 조건이 있다.
  5. `IndicatorWidth` 정의가 있고 XAML에서 소비된다.
- **Edge Cases**: 통과율 0% → 인디케이터 폭 0(트랙만 보이되 두께는 동일 4px) / 100% → 폭 120 / 항목 0개 스위트 → 카드 자체 미표시라 진행바 계산에 도달하지 않음 / 모든 스위트가 필터로 비면 목록이 완전히 빈 화면(빈 상태 안내는 시안·기존 모두 없음 — 무변경) / `TestCategories`가 null(기존 `?? []` 가드 유지).
- **Halt Forecast**: (ii-a) 사전 승인 — VM 공개 메서드 `RenameSuite`·`DeleteSuite` **제거**(호출부 전수 = 이번에 함께 제거되는 핸들러 2곳뿐, grep 확인). 그 외 없음.

### T4 — 메모 입력 다이얼로그 시안 정합(신규 `TestNoteDialog`) `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/Dialogs/TestNoteDialog.xaml`(신규), `DevDashboard_WinUI/Presentation/Views/Dialogs/TestNoteDialog.xaml.cs`(신규), `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`(`EditNote_Click` 교체), `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — `Presentation/Views/Dialogs/`(레포 관례: 다이얼로그 = ContentDialog 서브클래스). ② 신규 심볼 — `TestNoteDialog(string testName, string currentNote)`: 메모 입력 전용 다이얼로그, `ResultNote`로 결과 노출. ③ 의존 방향 — `TestPage.EditNote_Click`이 생성·소비, 다이얼로그는 `LocalizationService`만 참조(필드 1개라 별도 VM은 과설계 — `TestEditDialogViewModel` 같은 VM을 두지 않는다). ④ 비추상화 — 공용 "단일 텍스트 입력 다이얼로그"를 만들지 않는다(소비처 1곳이며, 스위트 이름수정 다이얼로그는 T3에서 사라져 2번째 소비처가 생기지 않는다).
- **구성**:
  - (D10) 시안 구성 — 제목 `TestNote_Title`, 부제 = 대상 테스트 이름(`AppInfoBrush`, `TextWrapping="Wrap"`), 여러 줄 TextBox(`MaxLength=500`, `MinHeight=110`, `AcceptsReturn="True"`, `TextWrapping="Wrap"`), 버튼 `Dialog_Save`/`Dialog_Cancel`(공용 키 재사용 — 값 무변경) + **`DefaultButton="Primary"`**(기존 인라인 다이얼로그와 동일 — 저장 버튼이 accent로 채워지는 조건). `ShowAsync`를 `new`로 섀도잉해 `XamlRoot` 설정(기존 패턴).
  - **placeholder 소비 방식(함정 3)**: `TestNote_Placeholder`는 접미 없는 베어네임이므로 `x:Uid`로 붙이면 **빌드는 통과하되 placeholder가 빈 문자열**이 된다 → 코드비하인드에서 `NoteBox.PlaceholderText = LocalizationService.Get("TestNote_Placeholder")`로 설정한다(제목·버튼과 동일 방식).
  - `TestPage.EditNote_Click`: 인라인 ContentDialog 구성 코드를 지우고 `TestNoteDialog`를 띄운 뒤 Primary이면 `Vm.EditProgressNote(test, dialog.ResultNote)` 호출.
  - (D11) resw 신규 2키 ko/en 양쪽.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `TestPage.xaml.cs`의 `EditNote_Click`에 `new ContentDialog` 잔존 0건, `TestNoteDialog` 사용 1건.
  3. `TestNote_Title`·`TestNote_Placeholder`가 ko/en **양쪽**에 각 언어 값으로 존재하고 접미 없는 베어네임이다(함정 2·3).
  4. placeholder 문구가 시안 원문 "메모를 입력하세요. 비워 두고 저장하면 메모가 삭제됩니다."와 **글자 그대로** 일치하고, `x:Uid`가 아니라 코드비하인드에서 `PlaceholderText`로 주입된다(함정 3).
  6. 다이얼로그에 `DefaultButton="Primary"`가 설정돼 있다.
  5. 저장 시 기존과 동일하게 `EditProgressNote`가 호출된다(빈 문자열 저장 = 메모 삭제, 동작 변경 0).
- **Edge Cases**: 메모 없는 항목에서 열기 → 빈 입력칸 + placeholder / 기존 메모 편집 → 현재 값 프리필 / 전부 지우고 저장 → 메모 삭제(문구와 동작 일치) / 취소 → 변경 없음 / 500자 초과 입력 차단 / 테스트 이름이 매우 긴 경우 → 부제 줄바꿈.
- **Halt Forecast**: (ii-a) 사전 승인 — 신규 파일 2개 생성 + resw 신규 키 2개(ko/en). 파괴적·외부 호출 없음.

## 사전 승인 항목 (일괄 승인 대상)
- **신규 파일 생성**: `Presentation/Views/Dialogs/TestNoteDialog.xaml(.cs)`(T4).
- **신규 resw 키**: `TestNote_Title`·`TestNote_Placeholder` ko/en 양쪽(T4). 공용 키(`Dialog_Save`·`Dialog_Cancel`) 값은 무변경.
- **심볼 제거**: `TestPageViewModel.RenameSuite`·`DeleteSuite`, `TestPage.RenameSuite_Click`·`DeleteSuite_Click`(T3) — 호출부는 함께 제거되는 헤더 메뉴 2곳뿐(grep 전수).
- **공용 스타일 수정**: `Styles.xaml`의 `FilterTabStyle`에 Setter 1개 추가(T1) — 소비처는 TestPage 상태 탭 4곳뿐.
- 로컬 작업 브랜치(`task/testpage-design-align`)에서 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR — 이번 작업 완료 후 별도 승인.
- **시안 대조 최종 시각 판정** — 빌드는 마크업 존재만 보증한다. "시안과 같아 보이는가"(구분선 농도·아이콘 형태·pill 폭·메모 블록·진행바 두께·탭 굵기·hover 색·메모 다이얼로그 레이아웃)는 사용자만 판정(⏳ HUMAN-VERIFY).
- **"전체 필터" 실동작 확인** — 원인 가설(Tag 빈 문자열)은 정적 근거이므로, 미실행 → 전체 클릭 시 전 항목이 다시 보이는지는 앱 실행 확인 필요(⏳ HUMAN-VERIFY).

## Deferred / Follow-up
- **[테스트 행에서 방법(Method) 미표시]** — 이번에 메모 표시는 해소(T2). `TestItem.Method`는 시안에 없어 행 미표시 유지(편집 다이얼로그에서만 다룸).
- **[SUGGEST] 상태 브러시의 Palette 이관** — 상태색 3종 + soft 3종에 이번 T2로 아이콘 채움/테두리/글리프 판정 헬퍼가 더해져 코드비하인드 색 로직이 늘었다. 색이 더 늘면 `Palette.xaml`(Default) 이관 검토.
- **[FR-E5 에러 표시 미착수]** — PRD FR-E5(Should)의 "에러/메모 표시" 중 **메모 몫만** 이번에 회수됐다. 에러 표시는 `TestItem`에 에러 필드 자체가 없어 도메인 변경이 선행돼야 하고(이번 Out of Scope), 시안에도 에러 표현이 없다. 실패 사유를 목록에서 보고 싶다는 요구가 생기면 필드 추가부터 별도 논의. (F-7 리뷰 M1, 2026-07-21)
- **[SUGGEST] 진행바 폭 상수 이중화** — `TestPage.xaml.cs`의 `ProgressBarWidth`(120)와 `TestPage.xaml`의 트랙 `Grid Width="120"`이 주석으로만 동기화된다. 한쪽만 바꾸면 인디케이터 비율이 조용히 어긋난다. 폭을 더 손보게 되면 한 곳에서만 정의하도록 정리 검토. (T3 quality 리뷰 SUGGEST, 2026-07-21)
- **[스위트 정리 경로 부재]** — D7로 스위트 이름수정·삭제 UI가 사라지고, T3의 빈 스위트 숨김으로 오타·구 이름 스위트는 화면에서만 사라진 채 DB에 잔존한다(`TestCategory`는 작업 카테고리 이름의 스냅샷 복사본이라 앱 설정 변경이 전파되지 않음). 잔존 스위트 정리 수단이 필요해지면 별도 논의(위 `[기존 자유명/"작업" 스위트 마이그레이션]`과 함께 다룰 후보).
- **[전체 필터 가설 기각 시 런타임 재조사]** — D1의 원인 가설(`Tag=""` → null 파싱)이 틀리면 T1은 동작상 no-op다. 사용자 확인에서 "미실행 → 전체" 증상이 재현되면 이 항목을 열어 `pjc:pjc-systematic-debugging`으로 런타임 재조사한다.
- **[`AddSuite` 고아 정리]** — `TestPageViewModel.AddSuite`는 이번 변경 이전부터 소비처 0. `RenameSuite`/`DeleteSuite` 제거와 함께 정리하는 것이 자연스러우나 이번 요청 범위 밖이라 보류.
- **[Test* 구 resw 고아 정리 합류]** — T3·T4로 `TestEditCategoryTitle`·`TestEditNoteTitle`이 고아가 된다. 대장 `[Test* 구 resw 고아 정리]` 항목에 합류시켜 일괄 정리.
- **[기존 자유명/"작업" 스위트 마이그레이션]** — 이전 plan에서 이관. 과거 자유명·"작업" 스위트 테스트는 칸반 배지에 반영되지 않는다. 필요 시 별도 논의.
- **[실패 색 시안 불일치]** — 호박 `#E8B45A` 유지(D12). 붉은 실패색을 원하면 순수 값 치환.
- **[NotificationPage 시안 대조]** — 대장 유지, 이번 범위 밖.

## Out of Scope
- `TestItem`·`TestCategory` 도메인·SQLite 스키마·직렬화 변경.
- 스위트(테스트 카테고리) 이름수정·삭제 기능 — D7로 **영구 제거**(작업 카테고리 관리로 일원화).
- 통계 카드·헤더(프로젝트명 배지·스위트 필터·등록 버튼)·등록/편집 다이얼로그 — 이번 지적 대상 아님.
- 픽셀 단위 수치 일치(목업 HTML 없음 — 구조·형태까지).
- NotificationPage·TaskPage.

## Open Questions
- [x] 상태 아이콘 형태(실패 포함) → **세 상태 모두 라운드 사각형**(통과·실패 채움+흰 글리프, 미실행 테두리형) — 사용자, 2026-07-21
- [x] 스위트 헤더 우클릭 메뉴 제거 시 기능 처리 → **기능째 제거** — 사용자, 2026-07-21

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + 기존 경고 5건(NU1903 1 + CS0612 4) 외 신규 0
- 회귀 방지 grep(`obj/` 제외):
  - `Tag=""` 잔존 0 / `Tag="All"` 1건(T1)
  - `Styles.xaml` `CheckStates/Checked`에 `Content.FontWeight`(T1)
  - `CornerRadius="11"` 잔존 0(T2)
  - `NoteVisibility`·`StatusIconBackground`·`StatusIconBorderBrush`·`StatusGlyphForeground`·`Row_PointerEntered`·`Row_PointerExited`·`IndicatorWidth` 정의 + XAML 소비(T2·T3)
  - `TestPage.xaml` 안에서 `ProgressBar`·스위트 헤더 `ContextFlyout` 잔존 0, 소스 전체(`*.cs`/`*.xaml`)에서 `RenameSuite`·`DeleteSuite` 잔존 0(T3) — `notes.md` 등 문서의 언급은 대상 아님
  - `EditNote_Click`에 `new ContentDialog` 잔존 0(T4)
  - `TestNote_Title`·`TestNote_Placeholder`가 ko/en 양쪽에 존재(T4)
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - **T1**: 미실행 → 전체 클릭 시 전 항목 재표시 / 선택 탭 글자 굵기·밑줄, 선택 탭에 마우스를 올려도 밑줄 유지(함정 7 회귀)
  - **T2**: 행 사이 구분선 / pill 3종 폭 동일 / 아이콘 라운드 사각형(통과·실패 채움+흰 글리프, 미실행 테두리형) / 행 hover 배경 / 메모 블록(좌측 컬러 바·깃발·텍스트)
  - **T3**: 진행바 두께가 통과율과 무관하게 일정 / 헤더 우클릭 시 메뉴 없음 / 항목 없는 스위트 카드 미표시
  - **T4**: 메모 다이얼로그 제목·테스트명 부제·placeholder 문구·취소/저장 배치, 비우고 저장 시 메모 삭제

## F-8 인계 목록 (V-9 `⏳ 미확인` — 렌더 육안 확인 필요)

> 마크업 수준 대조는 각 task의 V-9/spec 리뷰에서 완료. 아래는 **빌드로 판정 불가한 렌더 외형·실동작**이라 완료 선언 전 사용자 확인이 필요한 항목이다.

- **공통(F-7 리뷰 지적)**: 세 상태 아이콘의 **외곽 크기가 동일하게** 보이는지(m1 — 채움 상태의 테두리 두께를 0으로 바꿔 대응했으나 렌더 확인 필요) / 행을 **우클릭해 메뉴를 연 뒤 hover 배경이 잔존**하지 않는지(m6 — 잔존하면 `Tapped`/Flyout `Closed`에서 복원 필요)
- **T4**: 메모 다이얼로그의 제목("메모 추가")·대상 테스트명 부제 색/위치·입력칸 높이·placeholder 문구 / 취소·저장 버튼 배치와 저장 버튼 accent / 메모를 비우고 저장하면 실제로 삭제되는지
- **T3**: 진행바 두께가 통과율 0%·중간·100% 모두 같은지 / 인디케이터 색(통과색)과 트랙 대비 / 스위트 헤더 우클릭 시 메뉴가 뜨지 않는지 / 항목 없는 스위트 카드가 보이지 않는지
- **T2**: 행 사이 구분선 농도·간격 / 상태 아이콘 라운드 사각형(통과·실패 채움+흰 글리프, 미실행 테두리형) / 상태 pill 3종 폭 동일 / 행 마우스 오버 배경 변화 / 메모 블록(좌측 주황 바·깃발·본문) 배치와 이름 2줄일 때 아이콘·pill 세로 중앙 정렬
- **T1**: 선택 탭 라벨이 굵게 보이는지 / 개수 배지는 굵어지지 않았는지 / 선택 탭에 마우스를 올려도 밑줄이 유지되는지(함정 7) / **미실행 → 전체 클릭 시 전 항목 재표시**(D1 가설 검증)

## Phase Ledger
- 전 task(T1~T4) 완료.
- **Phase F 통과** — F-2 클린 리빌드(`-t:Rebuild`) 오류 0·신규 경고 0(기존 NU1903 1 + CS0612 4만), F-3 회귀 grep 8종 전부 기대값, F-6.5 notes·deferred 대장 반영(notes 약 24,600자 — 아카이브 기준 30,000자 미만이라 이동 없음), F-7 `plan-completion-reviewer`: BLOCKER 0 / MAJOR 1(M1 — FR-E5 "에러 표시" 몫 오표기 → PRD Coverage 부분 커버로 정정 + Deferred 등재 완료) / MINOR 6(m1 아이콘 테두리 두께 → 코드 수정, m2 Progress Log·Ledger → 반영, m3 문서 → 이미 반영, m4·m5 설명으로 종결, m6 → F-8 육안 항목 추가).
- **Phase G 통과 (Must 100%)** — 커버 대상 active Must FR(FR-E1 상태 필터 탭·FR-E3 메모 다이얼로그) 전부 충족, 재루프 0회. Should FR-E5는 메모 표시까지 충족하고 **에러 표시 몫만 Deferred로 이연**(사용자 판단 대상 — 도메인 필드 추가 선행 필요).
- **F-8 미통과 — 시각/실동작 확인 대기**: `## F-8 인계 목록`의 항목이 남아 **완료 선언 보류**(사용자 육안·실행 확인 필요).

## Progress Log
- T1~T3 완료 (커밋 da2b29a, d50a7d7, T3): 상태 필터 탭 정상화·굵기(T1), 테스트 행 재구성(구분선·pill 폭·라운드 사각형 아이콘·hover·메모 블록, T2), 스위트 헤더 진행바 커스텀·우클릭 메뉴 제거·빈 스위트 숨김(T3). 전부 빌드 OK, spec·quality 리뷰 지적 0.
  - 결정(T3): quality 리뷰가 고아 resw(`TestEditCategoryTitle`·`TestDeleteCategoryConfirm`) 제거를 MAJOR로 지적했으나, plan D11·Deferred 대장의 계획된 이연임을 근거로 반증해 **철회**됨. 개별 삭제 대신 대장의 일괄 audit에 유지.
- T4 완료 (커밋 fa8dd1a): 인라인 ContentDialog를 신규 `TestNoteDialog`로 교체(제목·대상 테스트명 부제·안내 placeholder·취소/저장), resw 신규 2키 ko/en. 빌드 OK, spec·quality 리뷰 지적 0.
- Phase F 마무리 (F-7 반영): FR-E5 "에러 표시" 몫을 부분 커버로 정정하고 Deferred 등재(M1), 상태 아이콘의 채움 상태 테두리 두께를 0으로 바꿔 세 상태의 외곽 크기를 맞춤(m1 — `StatusIconBorderThickness` 추가). 재빌드 OK.
  - 결정(T2): DataTemplate 안에서는 VSM `GoToState`가 안 먹으므로 hover를 `PointerEntered`/`PointerExited` 핸들러로 처리하고, Exited 복원값을 `null`이 아닌 `Transparent`로 둬 hit-test 영역이 좁아지지 않게 했다.
