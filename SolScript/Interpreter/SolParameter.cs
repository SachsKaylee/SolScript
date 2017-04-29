﻿using JetBrains.Annotations;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This type is used to define the name and type of a function parameter
    ///     or the name and type during a variable declaration.
    /// </summary>
    public class SolParameter
    {
        /// <summary>
        ///     Used by the parser.
        /// </summary>
        public SolParameter()
        {
            Name = "$unnamed";
            Type = SolType.AnyNil;
        }

        /// <summary> Creates a new parameter using the type "any!". </summary>
        /// <param name="name"> The name </param>
        public SolParameter([NotNull] string name)
        {
            Name = name;
            Type = SolType.AnyNil;
        }

        /// <summary> Creates a new parameter. </summary>
        /// <param name="name"> The name </param>
        /// <param name="type"> The type </param>
        public SolParameter([NotNull] string name, SolType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary> The name of this parameter. </summary>
        [NotNull]
        public string Name { get; [UsedImplicitly] internal set; }

        /// <summary> The type of this parameter. </summary>
        public SolType Type { get; [UsedImplicitly] internal set; }

        #region Overrides

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Name} : {Type}";
        }

        #endregion
    }
}