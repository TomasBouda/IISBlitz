using System;

namespace TomLabs.IISBlitz.App.Models;

public record JsConsoleMessage(
    DateTime Timestamp,
    string Level,
    string Text);
