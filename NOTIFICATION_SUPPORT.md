# MCP Server Notification Support

## Overview

The MCP Server now implements comprehensive notification support as required by the Model Context Protocol (MCP) specification, including both client-initiated notifications and **server-initiated notifications via Server-Sent Events (SSE)**.

## Implemented Notifications

### Core MCP Notifications (Client → Server)

- **`notifications/initialized`** - Required notification sent by client after successful initialization
- **`notifications/cancelled`** - Sent by client to cancel a request
- **`notifications/progress`** - Progress updates for long-running operations

### Server-Initiated Notifications (Server → Client via SSE)

- **`notifications/resources/list_changed`** - Sent when the list of available resources changes
- **`notifications/tools/list_changed`** - Sent when the list of available tools changes
- **`notifications/prompts/list_changed`** - Sent when the list of available prompts changes
- **`notifications/resources/updated`** - Sent when a specific resource is updated
- **`notifications/progress`** - Progress updates pushed from server

### Test Notifications

- **`test/notification`** - Test notification handler for debugging

## Server-Sent Events (SSE) Implementation

### Features
- **Real-time Push Notifications**: Server can send notifications to clients without polling
- **Session-based Connections**: Each MCP session can maintain its own SSE connection
- **Connection Management**: Automatic heartbeat, reconnection support, and cleanup
- **Message Queuing**: Notifications are queued when SSE is disconnected and delivered upon reconnection
- **Broadcasting**: Support for both targeted and broadcast notifications

### Establishing SSE Connections
Clients can establish SSE connections by sending a GET request:
```
GET /?session=<session-id> HTTP/1.1
Accept: text/event-stream
```

Or by including the session ID in the MCP-Session-Id header:
```
GET / HTTP/1.1
Accept: text/event-stream
MCP-Session-Id: <session-id>
```

### SSE Event Types
- **`connected`** - Sent when SSE connection is established
- **`heartbeat`** - Periodic keepalive (every 30 seconds)
- **`notification`** - JSON-RPC notifications from server

## API for Server-Initiated Notifications

### Broadcasting Methods
```csharp
// Broadcast to all connected sessions
await MCPServer.BroadcastNotification(notification);

// Send to specific session
await MCPServer.QueueNotificationForSession(sessionId, notification);
```

### Convenience Methods
```csharp
// Resource notifications
await MCPServer.SendResourceListChangedNotification();
await MCPServer.SendResourceUpdatedNotification(uri);

// Tool and prompt notifications
await MCPServer.SendToolListChangedNotification();
await MCPServer.SendPromptListChangedNotification();

// Progress notifications
await MCPServer.SendProgressNotification(progressToken, 0.5, 1.0);
```

## Lifecycle Implementation

The server now properly implements the MCP lifecycle:

1. **Initialize Phase**: Client sends `initialize` request with protocol version negotiation
2. **Initialized Notification**: Client sends `notifications/initialized` to complete initialization
3. **Operation Phase**: Normal request/response communication
4. **Shutdown Phase**: Clean connection termination

## Session Management

- Sessions are created during initialization
- Sessions are tracked for initialization state
- Only `initialize` and `ping` requests are allowed before initialization is complete
- Automatic session cleanup for expired sessions (24-hour default)

## Protocol Version Support

The server supports multiple MCP protocol versions with automatic negotiation:
- `2025-06-18` (latest)
- `2024-11-05`

## Menu Commands

New editor menu items under **Editor > MCP**:
- **Server Status** - Shows current server state and session information
- **Cleanup Expired Sessions** - Manually cleanup old sessions

## Usage

1. Start the MCP server from the editor menu
2. Client connects and sends `initialize` request
3. Server responds with capabilities and negotiated protocol version
4. Client sends `notifications/initialized` notification
5. Session is now ready for normal operations

## Implementation Details

### Notification Interface

```csharp
public interface IMCPNotification
{
    string Name { get; }
    Task HandleAsync(JsonRpcRequest request, string sessionId, string protocolVersion);
}
```

### Creating Custom Notifications

```csharp
[MCPNotification("your/notification")]
public class YourNotification : IMCPNotification
{
    public string Name => "your/notification";
    
    public async Task HandleAsync(JsonRpcRequest request, string sessionId, string protocolVersion)
    {
        // Handle notification
        await Task.CompletedTask;
    }
}
```

## Security

- Origin validation for HTTP requests
- Lifecycle validation prevents premature requests
- Session isolation and cleanup
- Request ID tracking for cancellation
