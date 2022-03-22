using UnityEngine;

namespace Platformer.Core
{
    /// <summary>
    /// Fuzzy provides methods for using values +- an amount of random deviation, or fuzz.
    /// </summary>
    static class Fuzzy
    {
        // NOTE! FUNCTIONALITY CHANGED BY CUSTOM EDITS
        // FUZZ DEFAULT VALUE REMOVED
        // HOWEVER, THESE METHODS ARE NEVER USED TO DOESN'T MATTER FOR THIS PURPOSE
        public static bool ValueLessThan(float value, float test, float fuzz)
        {
            var delta = value - test;
            return delta < 0 ? true : Random.value > delta / (fuzz * test);
        }

        public static bool ValueGreaterThan(float value, float test, float fuzz)
        {
            var delta = value - test;
            return delta < 0 ? Random.value > -1 * delta / (fuzz * test) : true;
        }

        public static bool ValueNear(float value, float test, float fuzz)
        {
            return Mathf.Abs(1f - (value / test)) < fuzz;
        }

        public static float Value(float value, float fuzz)
        {
            return value + value * Random.Range(-fuzz, +fuzz);
        }
    }
}