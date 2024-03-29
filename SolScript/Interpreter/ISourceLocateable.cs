﻿using NodeParser;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="ISourceLocateable" /> interface assists in finding a certain element in the SolScript code.
    /// </summary>
    public interface ISourceLocateable
    {
        /// <summary>
        ///     The location in code.
        /// </summary>
        NodeLocation Location { get; }
    }
}