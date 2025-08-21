# MCP Server-Sent Events (SSE) Implementation

## Overview

The Model Context Protocol (MCP) Server now implements comprehensive Server-Sent Events (SSE) support for real-time server-initiated notifications. This enables the server to push notifications to connected clients without requiring polling.

## Features

### SSE Connection Management
- **Persistent Connections**: SSE streams are maintained per session with automatic reconnection support
- **Session Integration**: SSE state is tracked in the `MCPSession` object
- **Connection Lifecycle**: Proper setup, heartbeat, and cleanup handling
- **Queue Management**: Notifications are queued when SSE is not connected and delivered upon connection

### Supported Notifications

#### Standard MCP Notifications
- **`notifications/resources/list_changed`** - Sent when the list of available resources changes
- **`notifications/tools/list_changed`** - Sent when the list of available tools changes  
- **`notifications/prompts/list_changed`** - Sent when the list of available prompts changes
- **`notifications/resources/updated`** - Sent when a specific resource is updated
- **`notifications/progress`** - Progress updates for long-running operations

#### Connection Events
- **`connected`** - Sent when SSE connection is established
- **`heartbeat`** - Periodic keepalive events (every 30 seconds)

## Usage

### Client Connection
Clients establish SSE connections by sending a GET request with:
```
Accept: text/event-stream
MCP-Session-Id: <session-id>
```

### Server API

#### Broadcasting Notifications
```csharp
// Broadcast to all connected sessions
await MCPServer.BroadcastNotification(notification);

// Send to specific session
await MCPServer.QueueNotificationForSession(sessionId, notification);
```

#### Convenience Methods
```csharp
// Resource list changed
await MCPServer.SendResourceListChangedNotification();
await MCPServer.SendResourceListChangedNotification(sessionId); // specific session

// Tool list changed  
await MCPServer.SendToolListChangedNotification();

// Prompt list changed
await MCPServer.SendPromptListChangedNotification();

// Resource updated
await MCPServer.SendResourceUpdatedNotification(uri);

// Progress notification
await MCPServer.SendProgressNotification(progressToken, 0.5, 1.0);
```

## Implementation Details

### Session Management
The `MCPSession` class has been extended to include:
- `SseStream` - The output stream for SSE events
- `SseConnected` - Connection status flag
- `SseCancellationTokenSource` - For clean connection cancellation
- `PendingNotifications` - Queue for notifications when SSE is disconnected

### Event Format
SSE events follow the standard format:
```
event: <event-type>
data: <json-data>

```

For notifications, the event type is `notification` and data contains the JSON-RPC notification.

### Connection Lifecycle
1. Client sends GET request with `Accept: text/event-stream`
2. Server validates session and establishes SSE connection
3. Server sends `connected` event
4. Server delivers any queued notifications
5. Server sends periodic `heartbeat` events
6. Connection maintained until client disconnects or server shuts down

### Error Handling
- Invalid sessions return 404
- Missing session ID returns 400
- Stream errors are logged and connections cleaned up
- Graceful handling of client disconnections

## Menu Commands

New testing commands available in **Editor > MCP > Test Notifications**:
- **Send Resource List Changed** - Test resource list change notification
- **Send Tool List Changed** - Test tool list change notification  
- **Send Progress Notification** - Test progress notification

## Security Considerations

- Origin validation for CORS
- Session-based access control
- Automatic cleanup of abandoned connections
- Resource cleanup on server shutdown

## Protocol Compliance

This implementation follows the MCP specification for server-initiated notifications and maintains compatibility with the standard JSON-RPC notification format used throughout the protocol.

## Examples

### JavaScript Client
```javascript
const eventSource = new EventSource('http://localhost:8098/', {
    headers: {
        'MCP-Session-Id': sessionId
    }
});

eventSource.addEventListener('notification', (event) => {
    const notification = JSON.parse(event.data);
    console.log('Received notification:', notification.method);
});

eventSource.addEventListener('connected', (event) => {
    console.log('SSE connected:', event.data);
});

eventSource.addEventListener('heartbeat', (event) => {
    console.log('Heartbeat:', event.data);
});
```

### Triggering Notifications
Tools and commands can trigger notifications to inform clients of changes:

```csharp
// After modifying resources
await MCPServer.SendResourceListChangedNotification();

// After updating a specific resource
await MCPServer.SendResourceUpdatedNotification("file:///path/to/resource");

// During long operations
await MCPServer.SendProgressNotification("operation-123", 0.3, 1.0);
```

This implementation provides a robust foundation for real-time communication between MCP servers and clients.
