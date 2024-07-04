﻿namespace Integrator.Shared
{
    public class ApplicationConfig
    {
        public string RootFolder { get; set; }

        public string? GoogleCredentialsPath { get; set; }

        public bool BitrixEnabled { get; set; }

        public string? BitrixDomain { get; set; }

        public string? BitrixRelativeSignalUrl { get; set; }

        public string BitrixAuthToken { get; set; }
    }
}
