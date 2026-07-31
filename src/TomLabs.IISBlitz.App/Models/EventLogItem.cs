using System;

namespace TomLabs.IISBlitz.App.Models;

public record EventLogItem(DateTime TimeGenerated, string Level, string Source, string Message);
