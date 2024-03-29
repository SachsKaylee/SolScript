﻿using System;
using System.Reflection;
using PSUtility.Enumerables;
using SolScript.Interpreter.Statements;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The SolChunkWrapper is used wrap <see cref="SolChunk" />, <see cref="MethodInfo" /> and
    ///     <see cref="ConstructorInfo" /> under one common class for usage in SolScript.
    /// </summary>
    public sealed class SolChunkWrapper
    {
        #region Type enum

        /// <summary>
        ///     The type of the wrapper chunk.
        /// </summary>
        public enum Type
        {
            /// <summary>
            ///     The wrapper wraps a <see cref="SolChunk" />.
            /// </summary>
            ScriptChunk,

            /// <summary>
            ///     The wrapper wraps a <see cref="MethodInfo" />.
            /// </summary>
            NativeMethod,

            /// <summary>
            ///     The wrapper wraps  a <see cref="ConstructorInfo" />.
            /// </summary>
            NativeConstructor
        }

        #endregion

        /// <summary>
        ///     Creates a new wrapper for a chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public SolChunkWrapper(SolChunk chunk)
        {
            m_Chunk = chunk;
            ChunkType = Type.ScriptChunk;
        }

        /// <summary>
        ///     Creates a new wrapper for a native method.
        /// </summary>
        /// <param name="method">The native method.</param>
        public SolChunkWrapper(MethodInfo method)
        {
            m_Chunk = method;
            ChunkType = Type.NativeMethod;
        }

        /// <summary>
        ///     Creates a new wrapper for a native constructor.
        /// </summary>
        /// <param name="constructor">The constructor.</param>
        public SolChunkWrapper(ConstructorInfo constructor)
        {
            m_Chunk = constructor;
            ChunkType = Type.NativeConstructor;
        }

        private readonly object m_Chunk;

        /// <summary>
        ///     The type of the wrapper chunk.
        /// </summary>
        /// <seealso cref="Type" />
        public Type ChunkType { get; }

        #region Overrides

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            return obj is SolChunkWrapper && Equals((SolChunkWrapper) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((m_Chunk != null ? m_Chunk.GetHashCode() : 0) * 397) ^ (int) ChunkType;
            }
        }

        public override string ToString()
        {
            return $"SolChunkWrapper(ChunkType: {ChunkType}, Chunk: {m_Chunk})";
        }

        #endregion

        /// <summary>
        ///     Gets an empty chunk for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The chunk.</returns>
        public static SolChunkWrapper EmptyOf(SolAssembly assembly)
        {
            SolChunkWrapper chunk;
            if (!assembly.TryGetMetaValue(SolMetaKeys.EmptyChunk, out chunk)) {
                chunk = new SolChunkWrapper(new SolChunk(assembly, SolSourceLocation.Native(), ArrayUtility.Empty<SolStatement>()));
                assembly.TrySetMetaValue(SolMetaKeys.EmptyChunk, chunk);
            }
            return chunk;
        }

        /// <summary>
        ///     Obtains a reference to the <see cref="SolChunk" /> wrapper in this wrapper.
        /// </summary>
        /// <returns>The chunk.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="ChunkType" /> is not <see cref="Type.ScriptChunk" />.</exception>
        public SolChunk GetScriptChunk()
        {
            if (ChunkType != Type.ScriptChunk) {
                throw new InvalidOperationException("Tried to obtain script chunk - The registered chunk is of type " + ChunkType + ".");
            }
            return (SolChunk) m_Chunk;
        }

        /// <summary>
        ///     Obtains a reference to the <see cref="MethodInfo" /> in this wrapper.
        /// </summary>
        /// <returns>The method.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="ChunkType" /> is not <see cref="Type.NativeMethod" />.</exception>
        public MethodInfo GetNativeMethod()
        {
            if (ChunkType != Type.NativeMethod) {
                throw new InvalidOperationException("Tried to obtain native method - The registered chunk is of type " + ChunkType + ".");
            }
            return (MethodInfo) m_Chunk;
        }

        /// <summary>
        ///     Obtains a reference to the <see cref="ConstructorInfo" /> in this wrapper.
        /// </summary>
        /// <returns>The constructor.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="ChunkType" /> is not <see cref="Type.NativeConstructor" />.</exception>
        public ConstructorInfo GetNativeConstructor()
        {
            if (ChunkType != Type.NativeConstructor) {
                throw new InvalidOperationException("Tried to obtain native constructor - The registered chunk is of type " + ChunkType + ".");
            }
            return (ConstructorInfo) m_Chunk;
        }

        private bool Equals(SolChunkWrapper other)
        {
            return Equals(m_Chunk, other.m_Chunk) && ChunkType == other.ChunkType;
        }
    }
}