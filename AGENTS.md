## MCP Unity — AI 에이전트 가이드 (MCP 패키지)

### 목적 (이 저장소가 하는 일)
**MCP Unity**는 MCP 지원 클라이언트에 Unity Editor 기능을 노출합니다. 다음을 실행하여 동작합니다:
- **Unity 측 "클라이언트" (C# Editor 스크립트)**: Unity Editor 내부에서 도구/리소스를 실행하는 WebSocket 서버.
- **Node 측 "서버" (TypeScript)**: MCP 도구/리소스를 등록하고 WebSocket을 통해 Unity로 요청을 전달하는 MCP stdio 서버.

### 동작 방식 (상위 수준 데이터 흐름)
- **MCP 클라이언트** ⇄ (stdio / MCP SDK) ⇄ **Node 서버** (`Server~/src/index.ts`)
- **Node 서버** ⇄ (WebSocket JSON-RPC 유사) ⇄ **Unity Editor** (`Editor/UnityBridge/McpUnityServer.cs` + `McpUnitySocketHandler.cs`)
- **도구/리소스 이름은 Node와 Unity에서 정확히 일치해야 합니다** (일반적으로 `lower_snake_case`).

### 기본값 및 불변 사항
- **Unity WebSocket 엔드포인트**: 기본값 `ws://localhost:8090/McpUnity`.
- **설정 파일**: `ProjectSettings/McpUnitySettings.json` (Unity에서 작성/읽기; Node에서 기회적으로 읽기).
- **실행 스레드**: 도구/리소스 실행은 `EditorCoroutineUtility`를 통해 디스패치되며 **Unity 메인 스레드**에서 실행됩니다. 동기 작업은 짧게 유지하세요; 긴 작업에는 비동기 패턴을 사용하세요.

### 저장소 레이아웃 (어디에서 무엇을 변경할지)
```
/
├── Editor/                       # Unity Editor 패키지 코드 (C#)
│   ├── Tools/                    # 도구 (McpToolBase 상속)
│   ├── Resources/                # 리소스 (McpResourceBase 상속)
│   ├── UnityBridge/              # WebSocket 서버 + 메시지 라우팅
│   ├── Services/                 # 도구/리소스에서 사용하는 테스트/로그 서비스
│   └── Utils/                    # 공유 헬퍼 (설정, 로깅, 워크스페이스 통합)
├── Server~/                      # Node MCP 서버 (TypeScript, ESM)
│   ├── src/index.ts              # 도구/리소스/프롬프트를 MCP SDK에 등록
│   ├── src/tools/                # MCP 도구 정의 (zod 스키마 + 핸들러)
│   ├── src/resources/            # MCP 리소스 정의
│   └── src/unity/mcpUnity.ts      # Unity와 통신하는 WebSocket 클라이언트
└── server.json                   # MCP 레지스트리 메타데이터 (이름/버전/패키지)
```

### 빠른 시작 (로컬 개발)
- **Unity 측**
  - 이 패키지가 설치된 Unity 프로젝트를 엽니다.
  - 서버가 실행 중인지 확인합니다 (자동 시작은 `McpUnitySettings.AutoStartServer`로 제어됩니다).
  - 설정은 `ProjectSettings/McpUnitySettings.json`에 저장됩니다.

- **Node 측 (빌드)**
  - `cd Server~ && npm run build`
  - MCP 진입점은 `Server~/build/index.js`입니다 (MCP stdio 서버로 게시됨).

- **Node 측 (디버그/검사)**
  - `cd Server~ && npm run inspector`로 MCP Inspector를 사용합니다.

### 설정 (Unity ↔ Node 브리지)
Unity 설정 파일이 공유 계약입니다:
- **경로**: `ProjectSettings/McpUnitySettings.json`
- **필드**
  - **Port** (기본값 **8090**): Unity WebSocket 서버 포트.
  - **RequestTimeoutSeconds** (기본값 **10**): Node 요청 타임아웃 (설정 파일을 찾을 수 있으면 Node가 읽습니다).
  - **AllowRemoteConnections** (기본값 **false**): 활성화 시 Unity가 `0.0.0.0`에 바인딩; 그렇지 않으면 `localhost`.
  - **EnableInfoLogs**: Unity 콘솔 로깅 상세도.
  - **NpmExecutablePath**: Unity 주도 설치/빌드를 위한 선택적 npm 경로.

Node는 **현재 작업 디렉토리** 기준 `../ProjectSettings/McpUnitySettings.json`에서 설정을 읽습니다. 찾을 수 없는 경우 Node는 다음 값으로 대체합니다:
- **host**: `localhost`
- **port**: `8090`
- **timeout**: `10s`

**원격 연결 참고**:
- Unity가 다른 머신에 있는 경우, Unity에서 `AllowRemoteConnections=true`로 설정하고 Node 프로세스에 `UNITY_HOST=<unity_머신_ip_또는_호스트명>`을 설정하세요.

### 새 기능 추가

### 도구 추가
1. **Unity (C#)**
   - `McpToolBase`를 상속하는 `Editor/Tools/<YourTool>Tool.cs`를 추가합니다.
   - `Name`을 MCP 도구 이름으로 설정합니다 (권장: `lower_snake_case`).
   - 구현:
     - 동기 작업을 위해 `Execute(JObject parameters)`를 구현하거나,
     - 장시간 작업을 위해 `IsAsync = true`를 설정하고 `ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)`를 구현합니다.
   - `Editor/UnityBridge/McpUnityServer.cs` (`RegisterTools()`)에 등록합니다.

2. **Node (TypeScript)**
   - `Server~/src/tools/<yourTool>Tool.ts`를 추가합니다.
   - `Server~/src/index.ts`에 도구를 등록합니다.
   - 파라미터에 zod 스키마를 사용하고; 동일한 `method` 문자열을 사용하여 Unity로 전달합니다:
     - `mcpUnity.sendRequest({ method: toolName, params: {...} })`

3. **빌드**
   - `cd Server~ && npm run build`

### 리소스 추가
1. **Unity (C#)**
   - `McpResourceBase`를 상속하는 `Editor/Resources/<YourResource>Resource.cs`를 추가합니다.
   - `Name` (메서드 문자열)과 `Uri` (예: `unity://...`)를 설정합니다.
   - `Fetch(...)` 또는 `FetchAsync(...)`를 구현합니다.
   - `Editor/UnityBridge/McpUnityServer.cs` (`RegisterResources()`)에 등록합니다.

2. **Node (TypeScript)**
   - `Server~/src/resources/<yourResource>.ts`를 추가하고, `Server~/src/index.ts`에 등록합니다.
   - `mcpUnity.sendRequest({ method: resourceName, params: {} })`를 통해 Unity로 전달합니다.

### 로깅 및 디버깅
- **Unity**
  - `McpUnity.Utils.McpLogger`를 사용합니다 (정보 로그는 `EnableInfoLogs`로 제어됨).
  - 연결 생명주기는 `Editor/UnityBridge/McpUnityServer.cs`에서 관리됩니다 (도메인 리로드 및 플레이모드 전환 시 서버를 중지/재시작합니다).

- **Node**
  - 로깅은 환경 변수로 제어됩니다:
    - `LOGGING=true`로 콘솔 로깅을 활성화합니다.
    - `LOGGING_FILE=true`로 Node 프로세스 작업 디렉토리에 `log.txt`를 작성합니다.

### 일반적인 함정
- **포트 불일치**: Unity 기본값은 **8090**입니다; 변경 시 문서/설정을 업데이트하세요.
- **이름 불일치**: Node의 `toolName`/`resourceName`은 Unity의 `Name`과 정확히 일치해야 하며, 그렇지 않으면 Unity가 `unknown_method`로 응답합니다.
- **긴 메인 스레드 작업**: 동기 `Execute()`는 Unity 에디터를 차단합니다; 무거운 작업에는 비동기 패턴을 사용하세요.
- **원격 연결**: Unity는 `0.0.0.0`에 바인딩해야 하며 (`AllowRemoteConnections=true`) Node는 올바른 호스트를 대상으로 해야 합니다 (`UNITY_HOST`).
- **Unity 도메인 리로드**: 스크립트 리로드 중 서버가 중지되며 재시작될 수 있습니다; 리로드 간 영구적인 인메모리 상태에 의존하지 마세요.
- **멀티플레이어 플레이 모드**: 복제 인스턴스는 자동으로 서버 시작을 건너뜁니다; 메인 에디터만 MCP 서버를 호스팅합니다.

### 릴리스/버전 범프 체크리스트
- 버전을 일관되게 업데이트:
  - Unity 패키지 `package.json` (`version`)
  - Node 서버 `Server~/package.json` (`version`)
  - MCP 레지스트리 `server.json` (`version` + npm 식별자/버전)
- Node 출력 재빌드: `cd Server~ && npm run build`

### 사용 가능한 도구 (현재)
- `execute_menu_item` — Unity 메뉴 항목 실행
- `select_gameobject` — 계층 구조에서 GameObject 선택
- `update_gameobject` — GameObject 프로퍼티 업데이트 또는 생성
- `update_component` — GameObject에 컴포넌트 업데이트 또는 추가
- `add_package` — Package Manager를 통한 패키지 설치
- `run_tests` — Unity Test Runner 테스트 실행
- `send_console_log` — Unity 콘솔에 로그 전송
- `add_asset_to_scene` — 씬에 에셋 추가
- `create_prefab` — 선택적 스크립트로 프리팹 생성
- `create_scene` — 새 씬 생성 및 저장
- `load_scene` — 씬 로드 (단일 또는 추가)
- `delete_scene` — 씬 삭제 및 Build Settings에서 제거
- `save_scene` — 현재 씬 저장 (선택적 다른 이름으로 저장)
- `get_scene_info` — 활성 씬 정보 및 로드된 씬 목록 가져오기
- `unload_scene` — 계층 구조에서 씬 언로드
- `get_gameobject` — 상세 GameObject 정보 가져오기
- `get_console_logs` — Unity 콘솔 로그 가져오기
- `recompile_scripts` — 모든 프로젝트 스크립트 재컴파일
- `duplicate_gameobject` — 선택적 이름 변경/부모 변경으로 GameObject 복제
- `delete_gameobject` — 씬에서 GameObject 삭제
- `reparent_gameobject` — 계층 구조에서 GameObject 부모 변경
- `create_material` — 지정된 셰이더로 머터리얼 생성
- `assign_material` — Renderer 컴포넌트에 머터리얼 할당
- `modify_material` — 머터리얼 프로퍼티 수정 (색상, 부동소수점, 텍스처)
- `get_material_info` — 모든 프로퍼티를 포함한 머터리얼 세부 정보 가져오기

### 사용 가능한 리소스 (현재)
- `unity://menu-items` — 사용 가능한 메뉴 항목 목록
- `unity://scenes-hierarchy` — 현재 씬 계층 구조
- `unity://gameobject/{id}` — ID 또는 경로로 GameObject 세부 정보
- `unity://logs` — Unity 콘솔 로그
- `unity://packages` — 설치된 패키지 및 사용 가능한 패키지
- `unity://assets` — 에셋 데이터베이스 정보
- `unity://tests/{testMode}` — Test Runner 테스트 정보

### 업데이트 정책 (에이전트용)
- 다음의 경우 이 파일을 업데이트하세요:
  - 도구/리소스/프롬프트가 추가/제거/이름 변경될 때,
  - 설정 구조 또는 기본 포트/경로가 변경될 때,
  - 브리지 프로토콜이 변경될 때 (요청/응답 계약).
- **고신호**를 유지하세요: 어디에서 코드를 편집해야 하는지, 실행/빌드/디버그 방법, 미묘한 문제를 방지하는 불변 사항.
