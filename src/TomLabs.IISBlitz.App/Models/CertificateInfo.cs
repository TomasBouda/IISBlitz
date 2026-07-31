using System;

namespace TomLabs.IISBlitz.App.Models;

public record CertificateInfo(string Subject, string Issuer, DateTime NotAfter, string Thumbprint, string Port);
