# KCViewer 번역 데이터

KCViewer 접속기가 받는 한국어 번역입니다. 접속기 설정 창의 「번역 주소」 기본값이 이 폴더입니다.

```
https://raw.githubusercontent.com/MallangMochi/KCViewer/main/Data/
```

## 폴더

| 폴더 | 내용 |
|---|---|
| `Translations/ko-KR/` | 접속기가 받는 파일: `ship.json`(함선·함급·접미사·함종), `equipment.json`(장비·장비 종류), `quest.json`(임무와 그 보상), `expedition.json`(원정), `operation.json`(해역·작전·적 함대), `Locks.json`(출격 잠금), `update.json`(파일별 판 번호) |
| `source/ko-KR/fill/` | 우리 임시 번역: KC3 번역에 아직 없는 것. `reward.json`은 임무 보상 메모의 줄임말·오타를 바로잡는 별칭과 KC3에 없는 이름 |
| `source/ko-KR/force/` | 우리가 고정한 표기: KC3 번역보다 앞섬 |

## 만드는 방식

- 번역할 원문은 게임에서 직접 뽑습니다(마스터 데이터, 임무 목록, 출격 전투의 적 함대 이름).
- 번역은 **우리가 고정한 표기 > KC3改 한국어 번역 > 우리 임시 번역** 순으로 합칩니다. KC3에 번역이 생기면 우리 임시 번역은 지웁니다.
- 아무도 번역하지 않은 원문은 넣지 않습니다. 접속기는 그 자리에 게임 원문을 보여 줍니다.
- 내용이 바뀐 파일만 `update.json`의 판 번호를 올립니다. 접속기는 시작할 때와 1시간마다 판 번호를 비교해 바뀐 파일만 받습니다.

### 임무 보상 (quest.json의 `reward_jp`, `reward`)

- 자원 4종(연료·탄약·강재·보크사이트)은 게임이 임무와 함께 보내므로 넣지 않습니다.
- 그 밖의 보상은 KC3改 일본어 임무 메모의 「[獲]」 부분을 읽어 넣습니다.
  - `reward_jp`: 메모 원문
  - `reward.get`: 받는 보상(한국어 이름, 원문 이름, 수량, 개수 단계 ★, 종류: 함선·장비·아이템·가구·해방)
  - `reward.choices`: 선택 보상 묶음(묶음마다 하나 고름)
- 메모에 소모품(고속건조재·고속수복재·개발자재·개수자재)이 하나도 없으면 KC3改 본체의 `quests_meta.json` 소모품 보상을 더합니다.
- 메모는 사람이 적은 것이라 틀릴 수 있습니다.

### 표기

- 함선 이름: 개조 접미사는 원문을 둡니다(改, 改二, drei 등). 컨버트 접미사는 번역합니다(甲 → 갑, 航 → 항 등). 예: 후부키 改二, 즈이카쿠 改二 갑, 치토세 항 改二
- 함종: 줄이지 않은 이름(경순양함, 정규항모)
- 함급: 「급」으로 통일(아야나미급, 비스마르크급)
- 장비: 噴式은 「분식」으로 쓰고 뒤 낱말에 붙임(분식전투기, 분식케이운 개)

## 라이선스

- 이 저장소: MIT (`../LICENSE`)
- 함선·장비·임무 등의 한국어 번역 대부분과 임무 보상 메모는 KC3改의 kc3-translations(https://github.com/KC3Kai/kc3-translations)에서 가져왔습니다. MIT, Copyright (c) 2015-2021 KC3改. 전문은 `LICENSE-KC3.txt`에 있습니다.
- 임무 소모품 보상은 KC3改 본체(https://github.com/KC3Kai/KC3Kai)의 `src/data/quests_meta.json`에서 가져왔습니다. MIT, Copyright (c) 2015-2026 dragonjet. 전문은 `LICENSE-KC3Kai.txt`에 있습니다.
- 게임 원문(일본어 이름과 글)의 권리는 게임의 권리자에게 있습니다.
