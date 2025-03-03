# Cursor MCP Monitor

A .NET console application that monitors Model Context Protocol (MCP) interactions in the Cursor AI editor. This tool helps developers debug and analyze MCP server-client communications by monitoring log files in real-time.

## What is MCP?

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to LLMs. It follows a client-server architecture where:
- **MCP Hosts** (like Cursor) connect to multiple servers
- **MCP Clients** maintain 1:1 connections with servers
- **MCP Servers** expose specific capabilities through the standardized protocol
- **Local Data Sources** and **Remote Services** are accessed securely through MCP servers

## Features

- Real-time monitoring of MCP client-server interactions in Cursor:
  - Client creation and connection events
  - Server offering listings and capabilities
  - Protocol errors and warnings
  - Client lifecycle transitions
- Monitors the Cursor logs directory for new MCP log files
- Parses and color-codes different message types:
  - Green: Client creation and successful connections
  - Yellow: Server offering listings
  - Red: Protocol errors and client closures
  - Gray: General information messages
- Supports log rotation and file truncation
- Cross-platform support (Windows, macOS, Linux)

## Configuration

The application can be configured through `appsettings.json`:

```json
{
  "LogsRoot": null,
  "PollIntervalMs": 1000,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

- `LogsRoot`: The root directory to monitor for Cursor MCP logs. If null, defaults to:
  - Windows: `%AppData%/Cursor/logs`
  - macOS: `~/Library/Application Support/Cursor/logs`
  - Linux: `~/.config/Cursor/logs`
- `PollIntervalMs`: How often to check for new log lines (in milliseconds)

You can also override settings using environment variables:
```bash
# Windows
set LogsRoot=C:\CustomPath\Cursor\logs
# Linux/macOS
export LogsRoot=/custom/path/cursor/logs
```

## Building and Running

### Prerequisites
- .NET 9.0 SDK or later

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

## Docker Support

The application includes Docker support. To build and run using Docker:

```bash
# Build the image
docker build -t cursor-mcp-monitor .

# Run the container
docker run -it --rm cursor-mcp-monitor
```

## Use Cases

- Debug MCP server implementations by monitoring client-server interactions
- Analyze protocol messages and error patterns
- Track client lifecycle and connection states
- Monitor server capabilities and offerings
- Verify correct protocol implementation

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.