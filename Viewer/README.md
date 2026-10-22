# 시제 74식 제독업무도 바빠 改 (KCViewer)

칸코레 접속기입니다. 74식 전자 관측기(Electronic Observer) 영문판을 바탕으로, 자유롭게 옮기고 붙이는 도킹 패널에 제독업무도 바빠!(KanColleViewer) 모양의 화면을 얹었습니다.

## 74식과 다른 점

- 게임 통신은 내장 브라우저의 DevTools로 읽습니다. MITM 프록시를 쓰지 않으므로 루트 인증서를 설치하지 않고 프록시 포트도 열지 않습니다.
- 정보 창과 도구 창을 제독업무 모양(라이트·다크)으로 다시 그렸습니다.
- 화면과 게임 데이터 번역은 한국어입니다. 번역은 이 저장소의 `Data/`에서 받습니다(KC3改 한국어 번역 + 우리 번역).
- 게임 그림은 프로그램에 넣지 않습니다. 접속기가 그리는 그림은 필요한 것만 공식 서버에서 받습니다.
- 74식의 프로그램 업데이트와 74식 서버로 보내는 보고는 쓰지 않습니다. 임무 트래커·개수 공창 같은 74식 데이터는 74식 데이터 저장소에서 받고, 이 저장소의 항목을 그 위에 덮어씁니다.

## 빌드

Windows와 .NET 10 SDK가 필요합니다.

```
powershell -File fork-build.ps1                                # KCViewer\bin\dev
powershell -File ..\Scripts\fork-portable.ps1 -Zip             # 설치가 필요 없는 포터블(..\dist\KCViewer)
```

릴리스(서명한 업데이트 파일)를 만드는 법은 [`Scripts/README.md`](../Scripts/README.md)에 있습니다.

## 라이선스

- MIT 라이선스입니다(`LICENSE`). 원본 74식의 저작권 표시(Andante, Ryuu Kitsune)를 함께 둡니다.
- 함께 쓰는 라이브러리의 고지는 `Licenses/`에 있습니다.
- 칸코레의 게임 그림과 데이터는 DMM/KADOKAWA 저작물이며 이 저장소에 넣지 않습니다.
