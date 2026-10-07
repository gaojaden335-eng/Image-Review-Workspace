using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Middleware;

/// <summary>
/// 捕获请求返回的 4xx/5xx 响应，输出包含关联 ID 的详细日志，并将日志落盘。
/// （不再处理异常，异常由 GlobalExceptionHandlerMiddleware 处理）
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly string _logDirectory;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        // 对流式导出接口不包裹响应体，避免文件流被提前关闭
        if (context.Request.Path.StartsWithSegments("/api/export"))
        {
            var sw = Stopwatch.StartNew();
            
            // 导出接口直接调用下一个中间件，异常由全局处理器处理
            await _next(context);

            // 如果状态码是错误状态，记录日志
            if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
            {
                var logEntry = BuildLogEntry(context, correlationId, sw.ElapsedMilliseconds, null);
                await WriteLogAsync(logEntry);
                _logger.LogWarning("导出请求错误: {Path} {StatusCode} CorrelationId={CorrelationId}",
                    context.Request.Path, context.Response.StatusCode, correlationId);
            }

            return;
        }

        var stopwatch = Stopwatch.StartNew();


        // 直接调用下一个中间件，不再捕获异常
        await _next(context);


        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // 记录所有错误响应（4xx, 5xx）
        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            var logEntry = BuildLogEntry(
                context,
                correlationId,
                elapsedMs,
                null);

            await WriteLogAsync(logEntry);

            // 根据状态码级别记录不同日志
            if (context.Response.StatusCode >= 500)
            {
                _logger.LogError(
                    "服务器错误: {Method} {Path} 状态码={StatusCode}, 耗时={ElapsedMs}ms, CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    elapsedMs,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "客户端错误: {Method} {Path} 状态码={StatusCode}, 耗时={ElapsedMs}ms, CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    elapsedMs,
                    correlationId);
            }
        }

        // 记录慢请求（超过1秒）
        if (elapsedMs > 1000)
        {
            _logger.LogWarning("慢请求: {Method} {Path} - {ElapsedMs}ms CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                elapsedMs,
                correlationId);
        }

    }

    private string BuildLogEntry(
        HttpContext context,
        string correlationId,
        long elapsedMs,
        string? exceptionType)
    {
        var request = context.Request;
        var statusCode = context.Response.StatusCode;
        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[{DateTime.UtcNow:O}] CorrelationId={correlationId}");
        logBuilder.AppendLine($"Status={statusCode}, DurationMs={elapsedMs}");
        logBuilder.AppendLine($"Request: {request.Method} {request.Path}");
        logBuilder.AppendLine($"ContentType={request.ContentType}, ContentLength={request.ContentLength ?? 0}");

        if (!string.IsNullOrWhiteSpace(exceptionType))
        {
            logBuilder.AppendLine($"ExceptionType={exceptionType}");
        }

        logBuilder.AppendLine(new string('-', 80));

        return logBuilder.ToString();
    }

    private async Task WriteLogAsync(string content)
    {
        var logFile = Path.Combine(_logDirectory, $"backend-{DateTime.UtcNow:yyyyMMdd}.log");
        await File.AppendAllTextAsync(logFile, content, Encoding.UTF8);
    }
}
