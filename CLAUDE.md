# CLAUDE.md

이 파일은 이 저장소의 코드를 작업할 때 Claude Code (claude.ai/code)에게 지침을 제공합니다.

## 프로젝트 개요

MCP Unity는 MCP 지원 클라이언트(Cursor, Windsurf, Claude Code, Codex CLI, GitHub Copilot)에 2계층 아키텍처를 통해 Unity Editor 기능을 노출합니다:

- **Unity Editor (C#)**: 도구/리소스를 실행하는 Unity 내부 WebSocket 서버
- **Node.js 서버 (TypeScript)**: WebSocket을 통해 AI 클라이언트와 Unity를 연결하는 MCP stdio 서버

**데이터 흐름**: MCP 클라이언트 ⇄ (stdio) ⇄ Node 서버 (`Server~/src/index.ts`) ⇄ (WebSocket) ⇄ Unity Editor (`Editor/UnityBridge/McpUnityServer.cs`)

## 빌드 및 개발 명령어

### Node.js 서버 (`Server~/`)
```bash
npm install          # 의존성 설치
npm run build        # TypeScript를 build/로 컴파일
npm run watch        # 감시 모드 컴파일
npm start            # 서버 실행 (node build/index.js)
npm test             # Jest 테스트 실행 (--experimental-vm-modules 사용)
npm run test:watch   # 감시 모드 테스트
npm run inspector    # 디버깅을 위한 MCP Inspector 실행
```

### Unity 측
- Unity Editor를 통한 빌드/테스트
- **Tools > MCP Unity > Server Window**에서 설정
- **Window > General > Test Runner**에서 EditMode 테스트

## 주요 디렉토리

```
Editor/                       # Unity Editor 패키지 (C#)
├── Tools/                    # MCP 도구 (McpToolBase 상속)
├── Resources/                # MCP 리소스 (McpResourceBase 상속)
├── Services/                 # TestRunnerService, ConsoleLogsService
├── UnityBridge/              # WebSocket 서버 + 메시지 라우팅
│   ├── McpUnityServer.cs     # 서버 생명주기를 관리하는 싱글톤
│   └── McpUnitySocketHandler.cs  # WebSocket 핸들러
└── Utils/                    # 로깅, 설정, 워크스페이스 헬퍼

Server~/                      # Node.js MCP 서버 (TypeScript/ESM)
├── src/index.ts              # 진입점 - 도구/리소스 등록
├── src/tools/                # MCP 도구 정의 (zod + 핸들러)
├── src/resources/            # MCP 리소스 정의
└── src/unity/mcpUnity.ts     # Unity에 연결하는 WebSocket 클라이언트
```

## 주요 불변 사항

- **WebSocket 엔드포인트**: `ws://localhost:8090/McpUnity` (설정 가능)
- **설정 파일**: `ProjectSettings/McpUnitySettings.json`
- **도구/리소스 이름은 Node와 Unity 간 정확히 일치해야 합니다** (`lower_snake_case` 사용)
- **실행 스레드**: 모든 도구 실행은 EditorCoroutineUtility를 통해 Unity 메인 스레드에서 실행

## 새 도구 추가

### 1. Unity 측 (C#)
`Editor/Tools/YourTool.cs` 생성:
```csharp
public class YourTool : McpToolBase {
    public override string Name => "your_tool";  // Node 측과 일치해야 함
    public override JObject Execute(JObject parameters) {
        // 구현
    }
}
```
`McpUnityServer.cs` → `RegisterTools()`에 등록.

### 2. Node 측 (TypeScript)
`Server~/src/tools/yourTool.ts` 생성:
```typescript
export function registerYourTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  server.tool("your_tool", "Description", paramsSchema.shape, async (params) => {
    return await mcpUnity.sendRequest({ method: "your_tool", params });
  });
}
```
`Server~/src/index.ts`에 등록.

### 3. 빌드
```bash
cd Server~ && npm run build
```

## 새 리소스 추가

도구와 동일한 패턴:
- Unity: `McpResourceBase`를 상속하고, `Fetch()`를 구현하고, `RegisterResources()`에 등록
- Node: `server.resource()`로 등록하고, `mcpUnity.sendRequest()`를 통해 전달

## 설정

**McpUnitySettings.json** 필드:
- `Port` (기본값 8090): Unity WebSocket 서버 포트
- `RequestTimeoutSeconds` (기본값 10): Node 요청 타임아웃
- `AllowRemoteConnections` (기본값 false): true일 때 0.0.0.0에 바인딩

**환경 변수** (Node 측):
- `UNITY_HOST`: Unity 호스트 재정의 (원격 연결용)
- `LOGGING=true`: 콘솔 로깅 활성화
- `LOGGING_FILE=true`: log.txt에 로그 작성

## 디버깅

- **MCP Inspector**: `cd Server~ && npm run inspector`
- **Unity 로그**: 설정의 `EnableInfoLogs`로 제어
- **Node 로그**: `LOGGING=true` 환경 변수 설정

## 일반적인 함정

- **이름 불일치**: Node의 도구/리소스 이름은 Unity의 `Name`과 정확히 일치해야 합니다
- **긴 메인 스레드 작업**: 동기 `Execute()`는 Unity를 차단합니다; 긴 작업에는 `IsAsync = true`와 `ExecuteAsync()`를 사용하세요
- **Unity 도메인 리로드**: 스크립트 리로드 중 서버가 중지됩니다; 영구적인 인메모리 상태에 의존하지 마세요
- **포트 충돌**: 기본값은 8090입니다; 다른 프로세스가 사용 중인지 확인하세요
- **멀티플레이어 플레이 모드**: 복제 인스턴스는 자동으로 서버 시작을 건너뜁니다; 메인 에디터만 MCP를 호스팅합니다

## 코드 규칙

- **C# 클래스**: PascalCase (예: `CreateSceneTool`)
- **TypeScript 함수**: camelCase (예: `registerCreateSceneTool`)
- **도구/리소스 이름**: lower_snake_case (예: `create_scene`)
- **커밋**: 컨벤셔널 형식 - `feat(scope):`, `fix(scope):`, `chore:`
- **실행 취소 지원**: 씬 수정에 `Undo.RecordObject()` 사용

## 요구사항

- Unity 2022.3+ (Unity 6 권장)
- Node.js 18+
- npm 9+
