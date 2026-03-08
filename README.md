# MCP Unity Editor (게임 엔진)

[![](https://badge.mcpx.dev?status=on 'MCP Enabled')](https://modelcontextprotocol.io/introduction)
[![](https://img.shields.io/badge/Unity-000000?style=flat&logo=unity&logoColor=white 'Unity')](https://unity.com/releases/editor/archive)
[![](https://img.shields.io/badge/Node.js-339933?style=flat&logo=nodedotjs&logoColor=white 'Node.js')](https://nodejs.org/en/download/)
[![](https://img.shields.io/badge/License-MIT-red.svg 'MIT License')](https://opensource.org/licenses/MIT)

> [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) 기반 한글화 패키지

```
                              ,/(/.   *(/,
                          */(((((/.   *((((((*.
                     .*((((((((((/.   *((((((((((/.
                 ./((((((((((((((/    *((((((((((((((/,
             ,/(((((((((((((/*.           */(((((((((((((/*.
            ,%%#((/((((((*                    ,/(((((/(#&@@(
            ,%%##%%##((((((/*.             ,/((((/(#&@@@@@@(
            ,%%######%%##((/(((/*.    .*/(((//(%@@@@@@@@@@@(
            ,%%####%#(%%#%%##((/((((((((//#&@@@@@@&@@@@@@@@(
            ,%%####%(    /#%#%%%##(//(#@@@@@@@%,   #@@@@@@@(
            ,%%####%(        *#%###%@@@@@@(        #@@@@@@@(
            ,%%####%(           #%#%@@@@,          #@@@@@@@(
            ,%%##%%%(           #%#%@@@@,          #@@@@@@@(
            ,%%%#*              #%#%@@@@,             *%@@@(
            .,      ,/##*.      #%#%@@@@,     ./&@#*      *`
                ,/#%#####%%#/,  #%#%@@@@, ,/&@@@@@@@@@&\.
                 `*#########%%%%###%@@@@@@@@@@@@@@@@@@&*'
                    `*%%###########%@@@@@@@@@@@@@@&*'
                        `*%%%######%@@@@@@@@@@&*'
                            `*#%%##%@@@@@&*'
                               `*%#%@&*'

     ███╗   ███╗ ██████╗██████╗         ██╗   ██╗███╗   ██╗██╗████████╗██╗   ██╗
     ████╗ ████║██╔════╝██╔══██╗        ██║   ██║████╗  ██║██║╚══██╔══╝╚██╗ ██╔╝
     ██╔████╔██║██║     ██████╔╝        ██║   ██║██╔██╗ ██║██║   ██║    ╚████╔╝
     ██║╚██╔╝██║██║     ██╔═══╝         ██║   ██║██║╚██╗██║██║   ██║     ╚██╔╝
     ██║ ╚═╝ ██║╚██████╗██║             ╚██████╔╝██║ ╚████║██║   ██║      ██║
     ╚═╝     ╚═╝ ╚═════╝╚═╝              ╚═════╝ ╚═╝  ╚═══╝╚═╝   ╚═╝      ╚═╝
```

MCP Unity는 Unity Editor용 Model Context Protocol 구현체로, AI 어시스턴트가 Unity 프로젝트와 상호작용할 수 있게 해줍니다. 이 패키지는 Unity와 MCP 프로토콜을 구현하는 Node.js 서버 사이의 브릿지를 제공하여, Cursor, Windsurf, Claude Code, Codex CLI, GitHub Copilot 등의 AI 에이전트가 Unity Editor 내에서 작업을 실행할 수 있게 합니다.

## 주요 기능

### IDE 통합 - 패키지 캐시 접근

MCP Unity는 VSCode 계열 IDE(Visual Studio Code, Cursor, Windsurf)와 자동으로 통합되어 Unity `Library/PackedCache` 폴더를 워크스페이스에 추가합니다:

- Unity 패키지에 대한 코드 인텔리전스 향상
- Unity 패키지의 자동완성 및 타입 정보 개선
- AI 코딩 어시스턴트의 프로젝트 종속성 이해도 향상

### MCP 서버 도구

MCP를 통해 Unity 씬과 게임오브젝트를 조작하고 조회할 수 있는 다양한 도구를 제공합니다:

**씬 & 게임오브젝트 관리**
- `execute_menu_item`: Unity 메뉴 아이템 실행
- `select_gameobject`: 게임오브젝트 선택
- `update_gameobject`: 게임오브젝트 속성 업데이트 (이름, 태그, 레이어, 활성/정적 상태)
- `update_component`: 컴포넌트 필드 업데이트 또는 컴포넌트 추가
- `get_gameobject`: 게임오브젝트의 상세 정보 조회
- `get_scene_info`: 활성 씬 정보 조회
- `duplicate_gameobject` / `delete_gameobject` / `reparent_gameobject`: 게임오브젝트 관리
- `create_scene` / `load_scene` / `save_scene` / `delete_scene` / `unload_scene`: 씬 관리
- `create_prefab`: MonoBehaviour 스크립트로 프리팹 생성
- `add_asset_to_scene`: AssetDatabase의 에셋을 씬에 추가
- `batch_execute`: 여러 도구 작업을 단일 배치로 실행

**Transform**
- `move_gameobject` / `rotate_gameobject` / `scale_gameobject` / `set_transform`: Transform 조작

**머티리얼**
- `create_material` / `assign_material` / `modify_material` / `get_material_info`: 머티리얼 관리

**스크립트**
- `create_script`: C# 스크립트 파일 생성 (MonoBehaviour, ScriptableObject, 일반 클래스 템플릿)
- `attach_script`: 게임오브젝트에 스크립트 컴포넌트 연결
- `get_script_info`: 스크립트의 직렬화 필드 및 공개 메서드 정보 조회
- `recompile_scripts`: 스크립트 리컴파일

**UI**
- `create_ui_element`: UI 요소 생성 (Canvas, Button, Text, Image, Panel, Slider, Toggle, InputField, Dropdown, ScrollView) - Canvas 및 EventSystem 자동 설정
- `modify_ui_element`: UI 요소 속성 수정 (텍스트, 폰트 크기, 색상, 위치, 크기, 활성 상태)

**애니메이션**
- `get_animator_info`: Animator 컴포넌트의 파라미터, 레이어, 상태 등 상세 정보 조회
- `set_animator_parameter`: Animator 파라미터 값 설정 (Float, Int, Bool, Trigger 지원)

**에셋 검색**
- `search_assets`: 타입, 라벨, 폴더 경로 등 필터로 프로젝트 에셋 검색
- `get_asset_dependencies`: 에셋의 종속성 조회 (재귀 옵션 지원)
- `reimport_asset`: 에셋 리임포트 (강제 전체 리임포트 옵션)

**빌드**
- `build_project`: 지정 플랫폼(Windows, macOS, Android, iOS, WebGL)으로 프로젝트 빌드
- `get_build_settings`: 현재 빌드 설정 조회 (타겟 플랫폼, 씬, 설정)

**프로젝트 설정**
- `get_project_settings`: 카테고리별 프로젝트 설정 조회 (player, quality, physics, time, build)
- `set_project_settings`: 카테고리별 프로젝트 설정 변경 (player, quality, physics, time)
- `get_build_scenes` / `set_build_scenes`: Build Settings 씬 목록 조회 및 설정

**플레이 모드**
- `enter_play_mode` / `exit_play_mode`: 플레이 모드 시작/종료
- `pause_editor`: 에디터 일시정지/재개
- `step_frame`: 일시정지 상태에서 프레임 단위 진행

**물리**
- `physics_raycast`: 씬에서 물리 레이캐스트 수행 (히트 지점, 법선, 거리, 게임오브젝트 반환)
- `get_physics_settings`: 물리 설정 조회 (중력, 솔버 반복, 임계값)

**스크린샷**
- `capture_screenshot`: Game View 또는 Scene View 스크린샷 캡처 (base64 PNG 반환)

**실행 취소/다시 실행**
- `undo` / `redo`: 실행 취소/다시 실행
- `get_undo_history`: 실행 취소 히스토리 조회

**기타**
- `add_package`: Unity Package Manager를 통한 패키지 설치
- `run_tests`: Unity Test Runner로 테스트 실행
- `send_console_log`: Unity 콘솔에 로그 전송
- `get_console_logs`: Unity 콘솔 로그 조회

### MCP 서버 리소스

- `unity://menu-items`: 사용 가능한 메뉴 아이템 목록 조회
- `unity://scenes-hierarchy`: 현재 씬의 게임오브젝트 계층구조 조회
- `unity://gameobject/{id}`: 특정 게임오브젝트의 상세 정보 조회
- `unity://logs`: Unity 콘솔 로그 조회
- `unity://packages`: 설치된 패키지 정보 조회
- `unity://assets`: Asset Database의 에셋 정보 조회
- `unity://tests/{testMode}`: Unity Test Runner의 테스트 정보 조회

## 요구사항

- Unity 6 이상 - [서버 설치](#서버-설치)
- Node.js 18 이상 - [서버 시작](#서버-시작)
- npm 9 이상 - [서버 디버깅](#서버-디버깅)

> **프로젝트 경로 관련 참고사항**
>
> MCP Unity는 공백이 포함된 프로젝트 경로를 지원합니다. 다만 연결 문제가 발생하면 공백이 없는 경로로 프로젝트를 이동해보세요.

## <a name="서버-설치"></a>설치

### 1단계: Node.js 설치

MCP Unity 서버를 실행하려면 Node.js 18 이상이 필요합니다.

1. [Node.js 다운로드 페이지](https://nodejs.org/en/download/)에서 LTS 버전을 다운로드합니다.
2. 설치 후 터미널에서 확인합니다:
   ```bash
   node --version
   ```

macOS에서 Homebrew를 사용하는 경우:
```bash
brew install node@18
```

### 2단계: Unity Package Manager로 패키지 설치

1. Unity Package Manager를 엽니다 (Window > Package Manager)
2. 좌측 상단 "+" 버튼을 클릭합니다
3. "Add package from git URL..." 을 선택합니다
4. 다음 URL을 입력합니다: `https://github.com/geuneda/geuneda-mcp-unity.git`
5. "Add"를 클릭합니다

### 3단계: AI LLM 클라이언트 설정

#### 방법 1: Unity Editor에서 설정 (권장)

1. Unity Editor를 엽니다
2. Tools > MCP Unity > Server Window로 이동합니다
3. 사용하는 AI LLM 클라이언트의 "Configure" 버튼을 클릭합니다
4. 팝업에서 설정 설치를 확인합니다

#### 방법 2: 수동 설정

AI 클라이언트의 MCP 설정 파일을 열고 다음 설정을 추가합니다:

> `ABSOLUTE/PATH/TO`를 실제 MCP Unity 설치 경로로 교체하세요. Unity Editor MCP Server 창(Tools > MCP Unity > Server Window)에서 경로를 복사할 수 있습니다.

**JSON 기반 클라이언트** (Cursor, Windsurf, Claude Code, GitHub Copilot 등):

```json
{
   "mcpServers": {
       "mcp-unity": {
          "command": "node",
          "args": [
             "ABSOLUTE/PATH/TO/mcp-unity/Server~/build/index.js"
          ]
       }
   }
}
```

**Codex CLI** (`~/.codex/config.toml`):

```toml
[mcp_servers.mcp-unity]
command = "node"
args = ["ABSOLUTE/PATH/TO/mcp-unity/Server~/build/index.js"]
```

## <a name="서버-시작"></a>Unity Editor MCP 서버 시작

1. Unity Editor를 엽니다
2. Tools > MCP Unity > Server Window로 이동합니다
3. "Start Server"를 클릭하여 WebSocket 서버를 시작합니다
4. AI 코딩 IDE(Cursor, Windsurf, Claude Code, Codex CLI, GitHub Copilot 등)를 열고 Unity 도구를 실행합니다

> AI 클라이언트가 WebSocket 서버에 연결되면 창의 녹색 박스에 자동으로 표시됩니다.

## 선택사항: WebSocket 포트 설정

기본적으로 WebSocket 서버는 포트 '8090'에서 실행됩니다. 포트를 변경하려면:

1. Unity Editor를 엽니다
2. Tools > MCP Unity > Server Window로 이동합니다
3. "WebSocket Port" 값을 원하는 포트 번호로 변경합니다
4. Node.js 서버를 재시작합니다
5. "Start Server"를 다시 클릭하여 재연결합니다

## 선택사항: 타임아웃 설정

기본 타임아웃은 10초입니다. 변경하려면:

1. Unity Editor를 엽니다
2. Tools > MCP Unity > Server Window로 이동합니다
3. "Request Timeout (seconds)" 값을 변경합니다
4. Node.js 서버를 재시작합니다

## 선택사항: 다중 클라이언트 동시 접속

MCP Unity는 여러 MCP 클라이언트의 동시 접속을 지원합니다. Claude Code의 에이전트 팀 기능 등으로 여러 에이전트를 동시에 실행할 때 유용합니다.

- 기본 최대 동시 접속 수는 **10**입니다
- 변경하려면 Tools > MCP Unity > Server Window에서 "Max Connections" 값을 조정합니다
- 유효 범위: 1~50 (파일 디스크립터 안전 제한)

## <a name="서버-디버깅"></a>서버 디버깅

### Node.js 서버 빌드

문제가 발생하면 Unity Editor에서 강제 설치할 수 있습니다:

1. Unity Editor를 엽니다
2. Tools > MCP Unity > Server Window로 이동합니다
3. "Force Install Server" 버튼을 클릭합니다

수동 빌드:

```bash
cd ABSOLUTE/PATH/TO/mcp-unity/Server~
npm install
npm run build
node build/index.js
```

### MCP Inspector로 디버깅

```bash
npx @modelcontextprotocol/inspector node Server~/build/index.js
```

### 콘솔 로그 활성화

```bash
# macOS / Linux
export LOGGING=true
export LOGGING_FILE=true

# Windows PowerShell
$env:LOGGING = "true"
$env:LOGGING_FILE = "true"
```

## 자주 묻는 질문

### MCP Unity란 무엇인가요?

MCP Unity는 Model Context Protocol(MCP)을 사용하여 Unity Editor 환경과 AI 어시스턴트 LLM 도구를 연결하는 브릿지입니다. Unity Editor 내부에서 WebSocket 서버를 실행하고, Node.js 서버가 MCP를 구현하여 AI 어시스턴트가 Unity에 명령을 보내고 정보를 받을 수 있게 합니다.

### 왜 MCP Unity를 사용해야 하나요?

- **개발 가속화**: AI 프롬프트를 사용하여 반복적인 작업 자동화, 보일러플레이트 코드 생성, 에셋 관리
- **생산성 향상**: 메뉴를 클릭하거나 스크립트를 작성하지 않고도 Unity Editor 기능과 상호작용
- **접근성 개선**: Unity Editor나 C# 스크립팅에 익숙하지 않은 사용자도 AI 안내를 통해 프로젝트 수정 가능
- **확장성**: 프로토콜과 도구 세트를 확장하여 프로젝트별 기능을 AI에 노출 가능

### 어떤 IDE를 지원하나요?

- Cursor
- Windsurf
- Claude Desktop
- Claude Code
- Codex CLI
- GitHub Copilot
- Google Antigravity

### 커스텀 도구로 확장할 수 있나요?

네. `McpToolBase`를 상속하는 C# 클래스를 만들어 커스텀 Unity Editor 기능을 노출하고, Node.js 서버의 `Server/src/tools/` 디렉토리에 TypeScript 핸들러를 추가하면 됩니다.

### 연결이 안 되는 경우

- WebSocket 서버가 실행 중인지 확인합니다 (Unity Server Window)
- MCP 클라이언트에서 콘솔 로그 메시지를 보내 재연결을 시도합니다
- Unity Editor MCP Server 창에서 포트 번호를 변경합니다

### 서버가 시작되지 않는 경우

- Unity 콘솔에서 에러 메시지를 확인합니다
- Node.js가 올바르게 설치되어 PATH에 포함되어 있는지 확인합니다
- Server 디렉토리에서 모든 종속성이 설치되어 있는지 확인합니다

## 테스트 실행

### C# 테스트 (Unity)

1. Unity Editor를 엽니다
2. Window > General > Test Runner로 이동합니다
3. "EditMode" 탭을 선택합니다
4. "Run All"을 클릭하여 모든 테스트를 실행합니다

### TypeScript 테스트 (서버)

```bash
cd Server~
npm test
```

Watch 모드로 실행:
```bash
npm run test:watch
```

## 라이선스

이 프로젝트는 [MIT License](LICENSE.md)로 배포됩니다.

## 원본 프로젝트

- 원본: [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity)
- [Model Context Protocol](https://modelcontextprotocol.io)
- [Unity Technologies](https://unity.com)
- [Node.js](https://nodejs.org)
