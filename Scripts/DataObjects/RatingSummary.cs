using System;

namespace ModIO
{
    [Serializable]
    public class RatingSummary : IEquatable<RatingSummary>, IAPIObjectWrapper<API.RatingSummaryObject>
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.RatingSummaryObject _data;

        public int totalRatings         { get { return _data.total_ratings; } }
        public int positiveRatings      { get { return _data.positive_ratings; } }
        public int negativeRatings      { get { return _data.negative_ratings; } }
        public int percentagePositive   { get { return _data.percentage_positive; } }
        public float weightedAggregate  { get { return _data.weighted_aggregate; } }
        public string displayText       { get { return _data.display_text; } }
        
        // - IAPIObjectWrapper Interface -
        public RatingSummary() {}
        public RatingSummary(API.RatingSummaryObject apiObject)
        {
            this.WrapAPIObject(apiObject);
        }

        public void WrapAPIObject(API.RatingSummaryObject apiObject)
        {
            this._data = apiObject;
        }

        public API.RatingSummaryObject GetAPIObject()
        {
            return this._data;
        }

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
