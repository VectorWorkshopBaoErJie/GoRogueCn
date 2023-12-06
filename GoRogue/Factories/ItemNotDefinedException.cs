﻿using System;
using JetBrains.Annotations;

namespace GoRogue.Factories
{
    /// <summary>
    /// Exception thrown by <see cref="AdvancedFactory{TBlueprintID, TBlueprintConfig, TProduced}" /> or <see cref="Factory{TBlueprintID, TProduced}" />
    /// objects when a blueprint that doesn't exist is used.
    /// </summary>
    [Serializable]
    [PublicAPI]
    public class ItemNotDefinedException<TBlueprintID> : Exception
    {
        /// <summary>
        /// Creates an exception with default message.
        /// </summary>
        public ItemNotDefinedException()
            : base("A blueprint ID was used that has not been added to the factory.")
        { }

        /// <summary>
        /// Creates an exception with the specified inner exception and message.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Exception that caused this exception</param>
        public ItemNotDefinedException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Creates an exception with a message based on the specified factory ID.
        /// </summary>
        /// <param name="factoryId">Factory id that caused the error.</param>
        public ItemNotDefinedException(TBlueprintID factoryId)
            : base($"The blueprint ID '{factoryId}' was used but has not been added to the factory.")
        { }
    }
}
