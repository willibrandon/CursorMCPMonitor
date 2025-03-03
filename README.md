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

## Configuration

The application can be configured through `appsettings.json`:

```json
{
  "LogsRoot": null,
  "PollIntervalMs": 1000,
  "LogPattern": "Cursor MCP.log",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
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
- `LogPattern`: The log file pattern to monitor (defaults to "Cursor MCP.log")
- `Logging:LogLevel:Default`: The verbosity level (Information, Debug, Warning, Error)
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

The application supports the following command-line options:

```
--logs-root, -l     Root directory containing Cursor logs
--poll-interval, -p Polling interval in milliseconds
--verbosity, -v     Log verbosity level (debug, info, warning, error)
--log-pattern, -f   Log file pattern to monitor
```

Examples:
```bash
# Monitor logs with 500ms polling interval
dotnet run -- --poll-interval 500

# Monitor logs in a custom directory with higher verbosity
dotnet run -- --logs-root "C:\CustomPath\Cursor\logs" --verbosity debug

# Monitor a different log file pattern
dotnet run -- --log-pattern "Cursor*.log"
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

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.