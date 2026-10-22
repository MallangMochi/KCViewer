# 배포 스크립트

`Viewer/`의 접속기를 포터블로 묶고, 프로그램 업데이트용 릴리스 파일을 만드는 스크립트입니다. Windows PowerShell과 .NET 10 SDK가 필요하고, 업데이터(`KCUpdater.exe`, NativeAOT)를 만들려면 Visual Studio의 C++ 빌드 도구도 필요합니다.

| 파일 | 내용 |
|---|---|
| `fork-portable.ps1` | 포터블 빌드(설치가 필요 없는 단일 파일 exe 두 개 + 업데이터 + 라이선스). `-Release`면 릴리스 파일까지 만듦 |
| `fork-portable.targets` | 포터블에 넣는 화면 언어(한국어·일본어·영어) |
| `ReleaseTool/` | 릴리스 파일 만들기·검사(서명 열쇠, 설치 목록, zip, 원격 json, 서명). 접속기와 같은 형식 정의(`Viewer/KCViewer/Fork/Update/UpdateFormat.cs`)를 씀 |

결과는 저장소 맨 위 `dist/`에 생기고 저장소에는 들어가지 않습니다.

## 포터블

```
powershell -File Scripts\fork-portable.ps1 -Zip
```

`dist\KCViewer\`와 `dist\KCViewer-<날짜>-<커밋>.zip`이 생깁니다. 태그가 없는 커밋은 개발 버전입니다.

## 릴리스

1. 릴리스할 커밋에 `Viewer/`·`Scripts/`의 변경이 남아 있지 않게 커밋하고 `v1.0.0` 꼴의 태그를 답니다.
2. 커밋과 태그를 GitHub에 올립니다.
3. 릴리스 파일을 만들고 올립니다(`gh` 로그인 필요).

```
powershell -File Scripts\fork-portable.ps1 -Release -NoteFile <메모 파일> -Publish
```

`dist\v1.0.0\`에 다음이 생기고, `-Publish`면 GitHub 릴리스 `v1.0.0`에 올라갑니다.

| 파일 | 내용 |
|---|---|
| `KCViewer\` | 포터블 폴더(안에 설치 목록 `install-manifest.json`) |
| `KCViewer-1.0.0.zip` | 릴리스 패키지. 접속기가 받는 파일이자 손으로 설치할 때 받는 파일 |
| `kcviewer-update.json` | 원격 json: 판, 메모, zip 주소·크기·SHA-256 |
| `kcviewer-update.json.sig` | 원격 json의 서명(ECDSA P-256) |

- 접속기는 `releases/latest/download/`에서 원격 json과 서명을 읽습니다. 서명이 맞고, zip 해시와 zip 안 설치 목록이 맞는 판만 설치합니다.
- `1.0.0-rc1`처럼 꼬리가 붙은 판은 사전 릴리스로 올라가서 자동 업데이트 대상이 되지 않습니다.
- 서명 공개 키는 `Viewer/Portable/update-public-key.txt`이고 빌드가 접속기 안에 넣습니다. 개인 키는 어느 저장소에도 올리지 않습니다(기본 위치는 저장소 밖 `..\keys\kcviewer-update-private.txt`, `-KeyFile`로 바꿈). 개인 키를 잃으면 새 판에 서명할 수 없으니 따로 백업해 둡니다.
- `-AllowDirty`는 커밋하지 않은 상태로 릴리스 파일을 미리 만들어 보는 데만 씁니다(`-Publish`와 함께 쓸 수 없음).
