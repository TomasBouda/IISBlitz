using System;

namespace TomLabs.IISBlitz.App.Models;

public record HealthCheckEntry(DateTime Timestamp, int StatusCode, long ResponseTimeMs);
