시제 74식 제독업무도 바빠 改 (KCViewer) 포터블
74식 전자 관측기(Electronic Observer, MIT)를 바탕으로 만든 접속기다. https://github.com/MallangMochi/KCViewer

■ 실행
- KCViewer.exe를 실행한다. .NET이 함께 들어 있어 따로 설치하지 않아도 된다.
- 브라우저는 Windows 10/11에 기본으로 있는 WebView2 런타임을 쓴다.
- 설치하지 않는다. 설정, 기록, 브라우저 프로필은 모두 이 폴더 안(Data\)에 생긴다.

■ 폴더
- Data\live\  라이브 인스턴스: 설정, 기록, 도킹 배치, 브라우저 프로필(DMM 로그인 포함). 처음 실행할 때 생긴다.
- Data\test\  테스트 인스턴스(테스트 영역 플러그인이 띄움)
- Plugins\    플러그인을 Plugins\<이름>\<이름>.dll 모양으로 넣는다. 없거나 비어 있으면 플러그인 없이 돈다.
- Skins\      로컬 스킨. 게임 경로 그대로 넣는다(예: Skins\kcs2\resources\...). 표시 > 로컬 스킨 > 로컬 스킨 사용으로 켠다.
- GameAssets\ 접속기가 공식 서버에서 받은 게임 그림. 처음 필요할 때 생긴다.
- Update\     프로그램 업데이트 때 받은 파일. 업데이트가 끝나면 정리된다.
- 나머지는 프로그램 파일이다. KCViewer.exe와 KCBrowser.exe 안에 .NET과 라이브러리가 들어 있고, 옆의 dll은 그 안에
  넣을 수 없는 네이티브 파일이다. KCUpdater.exe는 프로그램 업데이트를 하고, install-manifest.json은 설치된 파일
  목록이다(자동 업데이트에 필요하니 지우지 않는다).

■ 업데이트
- 시작할 때 새 판을 확인해 받고, 다시 시작하면 바뀐다(도움말 > 업데이트 확인으로도 확인한다). 서명을 확인한 판만 설치한다.
- 손으로 바꿀 때는 릴리스(https://github.com/MallangMochi/KCViewer/releases)의 zip을 새 폴더에 풀고
  Data\, Plugins\, Skins\, GameAssets\를 옮긴다.

■ 화면 언어
- 한국어, 일본어, 영어만 들어 있다. 설정에서 다른 언어를 고르면 일본어나 영어 문구로 나온다.

■ 로컬 서버와 게임 그림
- 이 폴더를 kancolle 폴더(server\, assets\가 있는 곳) 아래에 두면, 로컬 서버 플러그인과 게임 그림(assets\)을 저절로 찾는다.
- 다른 곳에 두면 Data\live\Settings\Fork.json의 "Assets" > "Folder"에 assets 폴더 경로를 적는다.

■ 주의
- 프로그램 안에는 게임 그림이 들어 있지 않다. 접속기가 그리는 게임 그림(장비·자원 아이콘, 함선 배너 등)은 assets\에 없으면 필요한 것만 공식 서버에서 받아 GameAssets\에 둔다. DMM/KADOKAWA 저작물이므로 개인용으로만 쓰고 남에게 주지 않는다.
- Data\에는 로그인 정보와 기록이 있다. 폴더를 옮기거나 압축할 때 조심한다.
- 원본 74식의 프로그램 업데이트는 쓰지 않는다. 게임 데이터와 번역 갱신은 동작한다.
- 원본 74식과 달리 MITM 프록시를 쓰지 않으므로 인증서를 설치하지 않는다.

■ 라이선스
- MIT 라이선스(LICENSE, 원본 74식의 저작권 표시 포함). 함께 쓰는 라이브러리의 고지는 Licenses\에 있다.
