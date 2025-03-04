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
- Smart error handling with exponential backoff and retry logic
- Configurable polling interval and log file patterns
- Command-line interface for easy customization
- **Structured logging** with Serilog for improved observability:
  - Console logging with formatted output
  - File logging with daily rotation
  - Contextual properties (machine name, thread ID, etc.)
  - Log level filtering and output customization

## Interactive Dashboard

The application includes a web-based dashboard for monitoring and analysis, accessible at `http://localhost:5050` when the application is running. The dashboard features real-time event streaming through WebSocket connections with automatic reconnection handling and event rate monitoring.

The terminal display shows timestamps with millisecond precision and color-coded message types for easy identification of different event types. Advanced search functionality includes text-based search with highlighting and keyboard shortcut support.

A command palette (Ctrl/Cmd + P) provides quick access to common actions:
- Clear logs (Ctrl/Cmd + K)
- Copy visible entries (Ctrl/Cmd + C)
- Toggle auto-scroll (Ctrl/Cmd + S)
- Focus search (/)

The dashboard includes status indicators for WebSocket connection state, active client count, and events per second. It supports both dark and light themes with system theme detection.

## Installation

You can install the tool globally using the .NET CLI:

```bash
# Install from NuGet.org
dotnet tool install --global CursorMCPMonitor

# Or install from GitHub Packages
dotnet nuget add source --name github "https://nuget.pkg.github.com/willibrandon/index.json"
dotnet tool install --global CursorMCPMonitor --add-source github
```

After installation, you can run the tool from anywhere using:
```bash
cursor-mcp --help
```

To update to the latest version:
```bash
dotnet tool update --global CursorMCPMonitor
```

To uninstall:
```bash
dotnet tool uninstall --global CursorMCPMonitor
```

## Configuration

The application can be configured through `appsettings.json`:

```json
{
  "LogsRoot": null,
  "PollIntervalMs": 1000,
  "LogPattern": "Cursor MCP.log",
  "Verbosity": "Debug",
  "Filter": null,
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "CursorMCPMonitor"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
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
- `LogPattern`: The log file pattern to monitor (supports glob patterns like "Cursor MCP*.log")
- `Verbosity`: The verbosity level (Debug, Information, Warning, Error). Defaults to Debug to show all messages.
- `Filter`: Optional text pattern to filter log content (only lines containing this text will be displayed)
- `Serilog`: Configuration for structured logging (see [Serilog configuration](#serilog-configuration))

You can also override settings using environment variables:
```bash
# Windows
set LogsRoot=C:\CustomPath\Cursor\logs
set PollIntervalMs=500
# Linux/macOS
export LogsRoot=/custom/path/cursor/logs
export PollIntervalMs=500
```

### Serilog Configuration

The application uses Serilog for structured logging, which can be configured in the `appsettings.json` file:

- `Serilog:MinimumLevel:Default`: The default minimum log level
- `Serilog:MinimumLevel:Override`: Override minimum log levels for specific namespaces
- `Serilog:Enrich`: Enrichers to add contextual information to logs
- `Serilog:Properties`: Custom properties to include in all log events

By default, logs are written to:
- Console: Formatted for human readability
- Files: Stored in the `logs` directory with daily rotation as `cursormonitor-YYYYMMDD.log`

## Command-Line Options

You can override configuration settings using command-line options:

```bash
# Specify a custom logs directory
dotnet run -- --logs-root "C:\Users\username\AppData\Roaming\Cursor\logs"

# Set a custom polling interval (500ms)
dotnet run -- --poll-interval 500

# Use a different log pattern (glob pattern support)
dotnet run -- --log-pattern "Cursor MCP*.log"

# Set verbosity level
dotnet run -- --verbosity debug

# Filter logs to only show lines containing specific text
dotnet run -- --filter "CreateClient"

# Combine multiple options
dotnet run -- --logs-root "/path/to/logs" --poll-interval 500 --verbosity error --filter "Error in MCP"
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
# Make sure you're in the repository root directory
cd /path/to/CursorMCPMonitor

# Build the image (note the -f flag to specify Dockerfile location)
docker build -t cursor-mcp-monitor -f src/CursorMCPMonitor/Dockerfile .

# Run the container with volume mapping for logs
# For Windows PowerShell:
docker run -it --rm -v "$env:APPDATA\Cursor\logs:/app/logs" -e LogsRoot=/app/logs cursor-mcp-monitor

# For Windows CMD:
docker run -it --rm -v "%APPDATA%\Cursor\logs:/app/logs" -e LogsRoot=/app/logs cursor-mcp-monitor

# For macOS/Linux:
docker run -it --rm -v "$HOME/Library/Application Support/Cursor/logs:/app/logs" -e LogsRoot=/app/logs cursor-mcp-monitor
```

> **Important**: 
> - Always run the Docker build command from the repository root directory, not from the project directory. This ensures that all necessary files are included in the build context.
> - When running the Docker container, you need to map your local Cursor logs directory into the container and set the `LogsRoot` environment variable to point to the mapped directory.

## Logging and Observability

The application implements structured logging using Serilog, which provides several benefits:

- **Contextual Information**: Each log entry includes contextual properties like machine name, thread ID, and source context
- **Multiple Output Formats**: Logs are written to both console and files in formatted output
- **Log Levels**: Different log levels (Debug, Information, Warning, Error, Fatal) help filter the most relevant information
- **Structured Data**: Log events include structured data that can be queried and analyzed
- **Log Files**: Log files are stored in the application's `logs` directory with daily rotation

Example log format (console):
```
[2025-03-03 12:34:56.789] [INF] [CursorMCPMonitor.Services.LogProcessorService] CreateClient detected: Cursor MCP.log 2025-03-03 12:34:56.123 [info] a602: Handling CreateClient action
```

Example log format (file):
```
2025-03-03 12:34:56.789 +00:00 [INF] [CursorMCPMonitor.Services.LogProcessorService] CreateClient detected: Cursor MCP.log 2025-03-03 12:34:56.123 [info] a602: Handling CreateClient action
```

## Error Handling

The application includes advanced error handling:

- Exponential backoff with jitter for file access errors
- Automatic recovery from transient issues
- Detailed error reporting with color-coded console output
- File rotation and truncation detection
- Structured error logging with contextual information

## Use Cases

- Debug MCP server implementations by monitoring client-server interactions
- Analyze protocol messages and error patterns
- Track client lifecycle and connection states
- Monitor server capabilities and offerings
- Verify correct protocol implementation
- Track application performance and error rates through structured logs

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.