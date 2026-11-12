#nullable enable
using PlantsVsZombies.Game;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>
    /// Reads, writes and invokes named non-public members of a game session for Player smoke scenarios and tests,
    /// searching the session itself and every collaborator it exposes so callers do not depend on which one owns a member.
    /// </summary>
    internal static class GameMemberReflection
    {
        /// <summary>Selects public and non-public instance members.</summary>
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Reads a field from whichever object in the session declares it.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The field name.</param>
        /// <returns>The current field value; null when the field holds null, as with the reflection call it mirrors.</returns>
        /// <exception cref="MissingMemberException">No object in the session declares the field.</exception>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the field.</exception>
        internal static object GetField(PvZGame game, string name)
        {
            var field = FindField(game, name, out var owner) ?? throw new MissingMemberException(nameof(PvZGame), name);
            return field.GetValue(owner)!;
        }

        /// <summary>Writes a field on whichever object in the session declares it.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The field name.</param>
        /// <param name="value">The value to store.</param>
        /// <exception cref="MissingMemberException">No object in the session declares the field.</exception>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the field.</exception>
        internal static void SetField(PvZGame game, string name, object? value)
        {
            var field = FindField(game, name, out var owner) ?? throw new MissingMemberException(nameof(PvZGame), name);
            field.SetValue(owner, value);
        }

        /// <summary>Invokes a method on whichever object in the session declares it.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The method name.</param>
        /// <param name="arguments">The method arguments, or null for a method without parameters.</param>
        /// <returns>The method result, or null for a void method, as with the reflection call it mirrors.</returns>
        /// <exception cref="MissingMemberException">No object in the session declares the method.</exception>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the method.</exception>
        internal static object Invoke(PvZGame game, string name, object?[]? arguments)
        {
            var method = FindMethod(game, name, out var owner) ?? throw new MissingMemberException(nameof(PvZGame), name);
            return method.Invoke(owner, arguments)!;
        }

        /// <summary>Finds the single field with the given name in the session or its collaborators.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The field name.</param>
        /// <param name="owner">The object that declares the field.</param>
        /// <returns>The field metadata, or null when no object in the session declares the field.</returns>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the field.</exception>
        internal static FieldInfo? FindField(PvZGame game, string name, out object owner)
        {
            FieldInfo? found = null;
            owner = game;
            foreach (var candidate in Owners(game))
            {
                var field = FindInHierarchy(candidate.GetType(), type => type.GetField(name, Members | BindingFlags.DeclaredOnly));
                if (field == null)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new AmbiguousMatchException("Field " + name + " is declared by both " + found.DeclaringType + " and " + field.DeclaringType + ".");
                }

                found = field;
                owner = candidate;
            }

            return found;
        }

        /// <summary>Finds the single method with the given name in the session or its collaborators.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The method name.</param>
        /// <param name="owner">The object that declares the method.</param>
        /// <returns>The method metadata, or null when no object in the session declares the method.</returns>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the method.</exception>
        internal static MethodInfo? FindMethod(PvZGame game, string name, out object owner)
        {
            MethodInfo? found = null;
            owner = game;
            foreach (var candidate in Owners(game))
            {
                var method = FindInHierarchy(candidate.GetType(), type => type.GetMethod(name, Members | BindingFlags.DeclaredOnly));
                if (method == null)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new AmbiguousMatchException("Method " + name + " is declared by both " + found.DeclaringType + " and " + method.DeclaringType + ".");
                }

                found = method;
                owner = candidate;
            }

            return found;
        }

        /// <summary>Resolves a field once so later reads and writes need not name its owner.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The field name.</param>
        /// <returns>The bound field, or null when no object in the session declares it (like the reflection lookup it replaces).</returns>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the field.</exception>
        internal static BoundGameField BindField(PvZGame game, string name)
        {
            var field = FindField(game, name, out var owner);
            return field == null ? null! : new BoundGameField(field, owner);
        }

        /// <summary>Resolves a method once so later calls need not name its owner.</summary>
        /// <param name="game">The game session to search.</param>
        /// <param name="name">The method name.</param>
        /// <returns>The bound method, or null when no object in the session declares it (like the reflection lookup it replaces).</returns>
        /// <exception cref="AmbiguousMatchException">More than one object in the session declares the method.</exception>
        internal static BoundGameMethod BindMethod(PvZGame game, string name)
        {
            var method = FindMethod(game, name, out var owner);
            return method == null ? null! : new BoundGameMethod(method, owner);
        }

        /// <summary>Enumerates the session followed by each collaborator exposed through an internal property.</summary>
        /// <param name="game">The game session whose collaborators are enumerated.</param>
        /// <returns>The session and its collaborators in declaration order.</returns>
        private static IEnumerable<object> Owners(PvZGame game)
        {
            yield return game;
            foreach (var property in typeof(PvZGame).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.GetIndexParameters().Length != 0 || !property.PropertyType.IsClass || property.PropertyType.Assembly != typeof(PvZGame).Assembly
                    || property.Name.StartsWith("Required", StringComparison.Ordinal))
                {
                    continue;
                }

                var collaborator = property.GetValue(game);
                if (collaborator != null)
                {
                    yield return collaborator;
                }
            }
        }

        /// <summary>Applies a declared-only lookup along a type's base-class chain.</summary>
        /// <typeparam name="T">The kind of member metadata being searched for.</typeparam>
        /// <param name="type">The most derived type to start from.</param>
        /// <param name="find">The declared-only lookup for one type.</param>
        /// <returns>The first member found, or null when the chain declares none.</returns>
        private static T? FindInHierarchy<T>(Type type, Func<Type, T?> find) where T : class
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var member = find(current);
                if (member != null)
                {
                    return member;
                }
            }

            return null;
        }
    }
}
