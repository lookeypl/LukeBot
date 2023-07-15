using System;
using System.Collections.Generic;
using LukeBot.Logging;


namespace LukeBot.Config
{
    /**
     * A handy set of utilities to manage some more complex (but repeatable) scenarios.
     *
     * Uses Conf class underneath - make sure to first initialize Config with Conf.Initialize()
     */
    public class ConfUtil
    {
        /**
         * Append an entry to an array. Sorts using default comparers.
         *
         * If Array does not exist in Config, it will be created.
         */
        public static void ArrayAppend<T>(Path path, T entry)
        {
            ArrayAppend<T>(path, entry, null);
        }

        /**
         * Append an array of entries to an array. Sorts using default comparers.
         *
         * If Array does not exist in Config, it will be created.
         */
        public static void ArrayAppend<T>(Path path, T[] entries)
        {
            ArrayAppend<T>(path, entries, null);
        }

        /**
         * Append an entry to an array and sort the array contents.
         *
         * If Array does not exist in Config, it will be created.
         */
        public static void ArrayAppend<T>(Path path, T entry, IComparer<T> comparer)
        {
            ArrayAppend<T>(path, new T[] { entry }, comparer);
        }

        /**
         * Append an entry to an array and sort the array contents.
         *
         * If Array does not exist in Config, it will be created.
         */
        public static void ArrayAppend<T>(Path path, T[] entries, IComparer<T> comparer)
        {
            T[] array;
            if (!Conf.TryGet<T[]>(path, out array))
            {
                array = new T[entries.Length];
                Array.Copy(entries, 0, array, 0, entries.Length);
                Array.Sort<T>(array, comparer);
                Conf.Add(path, Property.Create<T[]>(array));
                return;
            }

            int oldLength = array.Length;
            Array.Resize(ref array, array.Length + entries.Length);
            Array.Copy(entries, 0, array, oldLength, entries.Length);
            Array.Sort<T>(array, comparer);
            Conf.Modify<T[]>(path, array);
        }

        /**
         * Remove an entry to an array. Removing a last element will remove the whole Path
         * from the Config.
         *
         * Prints a warning if an entry does not exist and leaves quietly (no exception is thrown)
         */
        public static void ArrayRemove<T>(Path path, T entry)
        {
            ArrayRemove<T>(path, s => !s.Equals(entry));
        }

        /**
         * Remove an entry to an array. Removing a last element will remove the whole Path
         * from the Config.
         *
         * Prints a warning if an entry does not exist and leaves quietly (no exception is thrown).
         *
         * Predicate @p pred is forwarded to Array.FindAll() call. That is, @p pred must return true
         * for elements to be kept, and false for elements to remove.
         */
        public static void ArrayRemove<T>(Path path, Predicate<T> pred)
        {
            T[] array;
            if (!Conf.TryGet<T[]>(path, out array))
            {
                Logger.Log().Warning("Cannot remove entry - path {0} does not exist.", path.ToString());
                return;
            }

            array = Array.FindAll<T>(array, pred);

            if (array.Length == 0)
                Conf.Remove(path);
            else
                Conf.Modify<T[]>(path, array);
        }
    }
}