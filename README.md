# Cursor MCP Monitor

A .NET console application that monitors Cursor MCP (Machine Control Protocol) log files in real-time. This tool helps track client interactions, errors, and protocol messages in the Cursor AI editor.

## Features

- Monitors the Cursor logs directory for new subdirectories in real-time
- Detects and tails `Cursor MCP.log` files in `exthost/anysphere.cursor-always-local` folders
- Parses and color-codes different types of log messages:
  - Client creation and connection events
  - Offering listings
  - Errors and warnings
  - Client lifecycle transitions
- Configurable log directory and polling interval
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

- `LogsRoot`: The root directory to monitor for Cursor logs. If null, defaults to:
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

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.