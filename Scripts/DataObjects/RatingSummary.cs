using System;

namespace ModIO
{
    [Serializable]
    public class RatingSummary : IEquatable<RatingSummary>
    {
        // - Constructors - 
        public static RatingSummary GenerateFromAPIObject(API.RatingSummaryObject apiObject)
        {
            RatingSummary newRatingSummary = new RatingSummary();
            newRatingSummary._data = apiObject;
            return newRatingSummary;
        }

        public static RatingSummary[] GenerateFromAPIObjectArray(API.RatingSummaryObject[] apiObjectArray)
        {
            RatingSummary[] objectArray = new RatingSummary[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = RatingSummary.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.RatingSummaryObject _data;

        public int totalRatings         { get { return _data.total_ratings; } }
        public int positiveRatings      { get { return _data.positive_ratings; } }
        public int negativeRatings      { get { return _data.negative_ratings; } }
        public int percentagePositive   { get { return _data.percentage_positive; } }
        public float weightedAggregate  { get { return _data.weighted_aggregate; } }
        public string displayText       { get { return _data.display_text; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as RatingSummary);
        }

        public bool Equals(RatingSummary other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
