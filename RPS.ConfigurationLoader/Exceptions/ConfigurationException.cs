// <copyright file="ConfigurationException.cs" company="RPS">
// Copyright (c) RPS. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace RPS.ConfigurationLoader.Exceptions;

[Serializable]
public class ConfigurationException : Exception {
    public ConfigurationException() {
    }

    public ConfigurationException(string message) : base(message) {
    }

    public ConfigurationException(string message, Exception inner) : base(message, inner) {
    }

    protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }
}
