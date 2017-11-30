using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO
{
    // ---------[ BASE CLASSES ]---------
    public class Filter
    {
        public readonly static Filter NONE = new Filter();

        protected int limit = 20;

        public virtual string GenerateQueryString()
        {
            return "_limit=" + limit;
        }
    }

    public abstract class Filter<T> : Filter
    {
        internal delegate int SortDelegate(T a, T b);
        internal delegate T_Field GetFieldDelegate<T_Field>(T o);

        // ---------[ FIELD INFORMATION MAPPING ]---------
        internal class FieldInformation
        {
            public string apiFilterName;
            public SortDelegate sortAscendingDelegate;

            public GetFieldDelegate<int> getFieldAsInt;
            public GetFieldDelegate<float> getFieldAsFloat;
            public GetFieldDelegate<string> getFieldAsString;
            public GetFieldDelegate<string[]> getFieldAsStringArray;

            public FieldInformation(string filterName)
            {
                this.apiFilterName = filterName;
            }
        }

        // ------[ VARIABLES ]------
        protected string sortString = "";
        protected Comparison<T> sortDelegate = null;

        protected delegate bool FieldFilterDelegate(T o);
        protected Dictionary<int, string> filterStringMap = new Dictionary<int, string>();
        protected Dictionary<int, FieldFilterDelegate> filterDelegateMap = new Dictionary<int, FieldFilterDelegate>();

        // ------[ INITIALIZATION ]---
        internal Filter(string initialSortString, SortDelegate initialSortDelegate)
        {
            Debug.Assert(!String.IsNullOrEmpty(initialSortString)
                         && initialSortDelegate != null);

            sortString = initialSortString;
            sortDelegate = (a,b) => initialSortDelegate(a,b);
        }

        public abstract void ResetSorting();
        internal abstract FieldInformation GetFieldInformation(int fieldIdentifier);

        // ------[ OUTPUT FUNCTIONS ]------
        public override string GenerateQueryString()
        {
            string filterString = "_limit=" + limit;
            filterString += "&_sort=" + sortString;

            foreach(string fs in filterStringMap.Values)
            {
                filterString += "&" + fs;
            }

            return filterString;
        }
        public T[] GetFilteredArray(T[] objectArray)
        {
            List<T> filteredList = new List<T>(objectArray.Length);

            foreach(T o in objectArray)
            {
                bool doAdd = true;
                foreach(FieldFilterDelegate isObjectAccepted in filterDelegateMap.Values)
                {
                    if(!isObjectAccepted(o))
                    {
                        doAdd = false;
                        break;
                    }
                }

                if(doAdd)
                {
                    filteredList.Add(o);
                }
            }

            filteredList.Sort(sortDelegate);

            T[] retVal;

            if(filteredList.Count <= limit)
            {
                retVal = filteredList.ToArray();
            }
            else
            {
                retVal = new T[limit];
                filteredList.CopyTo(0, retVal, 0, limit);
            }

            return retVal;
        }

        // ------[ SORTING APPLICATION ]------
        protected void _ApplySortAscending(int fieldIdentifier)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);
            sortString = info.apiFilterName;
            sortDelegate = (a,b) => info.sortAscendingDelegate(a,b);
        }
        protected void _ApplySortDescending(int fieldIdentifier)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);
            sortString = "-" + info.apiFilterName;
            sortDelegate = (a,b) => info.sortAscendingDelegate(b,a);
        }

        // ------[ FILTER APPLICATION ]------
        protected void _ClearOnField(int fieldIdentifier)
        {
            filterStringMap.Remove(fieldIdentifier);
            filterDelegateMap.Remove(fieldIdentifier);
        }

        // ---[ INTEGER FILTERS ]---
        protected void _ApplyIntEquality(int fieldIdentifier, int value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) == value; };
        }
        protected void _ApplyIntInequality(int fieldIdentifier, int value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) != value; };
        }
        protected void _ApplyIntInArray(int fieldIdentifier,
                                                 int[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return values.Contains(info.getFieldAsInt(o)); };
        }
        protected void _ApplyIntNotInArray(int fieldIdentifier,
                                                    int[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return !values.Contains(info.getFieldAsInt(o)); };
        }
        protected void _ApplyIntMinimum(int fieldIdentifier,
                                                int value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-min=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) >= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-gt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) > value; };
            }
        }
        protected void _ApplyIntMaximum(int fieldIdentifier,
                                                int value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-max=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) <= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-lt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsInt(o) < value; };
            }
        }
        protected void _ApplyIntRange(int fieldIdentifier,
                                              int minimum, bool isMinimumInclusive,
                                              int maximum, bool isMaximumInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string minString;
            string maxString;
            FieldFilterDelegate minDelegate;
            FieldFilterDelegate maxDelegate;

            if(isMinimumInclusive)
            {
                minString = info.apiFilterName + "-min=" + minimum;
                minDelegate = (o) => { return info.getFieldAsInt(o) >= minimum; };
            }
            else
            {
                minString = info.apiFilterName + "-gt=" + minimum;
                minDelegate = (o) => { return info.getFieldAsInt(o) > minimum; };
            }

            if(isMaximumInclusive)
            {
                maxString = info.apiFilterName + "-max=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsInt(o) <= maximum; };
            }
            else
            {
                maxString = info.apiFilterName + "-lt=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsInt(o) < maximum; };
            }

            filterStringMap[fieldIdentifier] = minString + "&" + maxString;
            filterDelegateMap[fieldIdentifier] = (o) => { return minDelegate(o) && maxDelegate(o); };
        }

        // ---[ FLOAT FILTERS ]---
        protected void _ApplyFloatEquality(int fieldIdentifier, float value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) == value; };
        }
        protected void _ApplyFloatInequality(int fieldIdentifier, float value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) != value; };
        }
        protected void _ApplyFloatInArray(int fieldIdentifier,
                                                   float[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return values.Contains(info.getFieldAsFloat(o)); };
        }
        protected void _ApplyFloatNotInArray(int fieldIdentifier,
                                                      float[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0].ToString();
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return !values.Contains(info.getFieldAsFloat(o)); };
        }
        protected void _ApplyFloatMinimum(int fieldIdentifier,
                                                  float value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-min=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) >= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-gt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) > value; };
            }
        }
        protected void _ApplyFloatMaximum(int fieldIdentifier,
                                                  float value, bool isValueInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            if(isValueInclusive)
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-max=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) <= value; };
            }
            else
            {
                filterStringMap[fieldIdentifier] = info.apiFilterName + "-lt=" + value;
                filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsFloat(o) < value; };
            }
        }
        protected void _ApplyFloatRange(int fieldIdentifier,
                                                float minimum, bool isMinimumInclusive,
                                                float maximum, bool isMaximumInclusive)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string minString;
            string maxString;
            FieldFilterDelegate minDelegate;
            FieldFilterDelegate maxDelegate;

            if(isMinimumInclusive)
            {
                minString = info.apiFilterName + "-min=" + minimum;
                minDelegate = (o) => { return info.getFieldAsFloat(o) >= minimum; };
            }
            else
            {
                minString = info.apiFilterName + "-gt=" + minimum;
                minDelegate = (o) => { return info.getFieldAsFloat(o) > minimum; };
            }

            if(isMaximumInclusive)
            {
                maxString = info.apiFilterName + "-max=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsFloat(o) <= maximum; };
            }
            else
            {
                maxString = info.apiFilterName + "-lt=" + maximum;
                maxDelegate = (o) => { return info.getFieldAsFloat(o) < maximum; };
            }

            filterStringMap[fieldIdentifier] = minString + "&" + maxString;
            filterDelegateMap[fieldIdentifier] = (o) => { return minDelegate(o) && maxDelegate(o); };
        }

        // ---[ STRING FILTERS ]---
        protected void _ApplyStringEquality(int fieldIdentifier,
                                                    string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsString(o) == value; };
        }
        protected void _ApplyStringInequality(int fieldIdentifier,
                                                      string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsString(o) != value; };
        }
        protected void _ApplyStringInArray(int fieldIdentifier,
                                                   string[] values)
        {
            Debug.Assert(values.Length > 0);
            
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0];
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return values.Contains(info.getFieldAsString(o)); };
        }
        protected void _ApplyStringNotInArray(int fieldIdentifier,
                                                      string[] values)
        {
            Debug.Assert(values.Length > 0);
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            string valueList = values[0];
            for(int i = 1;
                i < values.Length;
                ++i)
            {
                valueList += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-in=" + valueList;

            filterDelegateMap[fieldIdentifier] = (o) => { return !values.Contains(info.getFieldAsString(o)); };
        }

        protected void _ApplyStringLike(int fieldIdentifier,
                                                string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-lk=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsString(o).Like(value.Replace('*', '%')); };
        }
        protected void _ApplyStringNotLike(int fieldIdentifier,
                                                   string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "-not-lk=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return !info.getFieldAsString(o).Like(value.Replace('*', '%')); };
        }

        // ---[ ARRAY FILTERS ]---
        protected void _ApplyStringArrayContains(int fieldIdentifier,
                                                         string value)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + value;
            filterDelegateMap[fieldIdentifier] = (o) => { return info.getFieldAsStringArray(o).Contains(value); };
        }
        protected void _ApplyStringArrayContainsAll(int fieldIdentifier,
                                                            string[] values)
        {
            FieldInformation info = GetFieldInformation(fieldIdentifier);

            Debug.Assert(values.Length > 0);
            string filterString = values[0];
            for(int i = 1; // first tag is already handled
                i < values.Length;
                ++i)
            {
                filterString += "," + values[i];
            }
            filterStringMap[fieldIdentifier] = info.apiFilterName + "=" + filterString;

            filterDelegateMap[fieldIdentifier] = (T o) =>
            {
                List<string> unmatchedValues = new List<string>(values);
                foreach(string fieldValue in info.getFieldAsStringArray(o))
                {
                    unmatchedValues.Remove(fieldValue);
                }
                return unmatchedValues.Count == 0;
            };
        }
    }

    internal static class FilterUtility
    {
        internal static void AddIntField<T>(this Dictionary<int, Filter<T>.FieldInformation> fieldInformationMap,
                                            int fieldIdentifier, string apiFilterName,
                                            Filter<T>.GetFieldDelegate<int> getFieldAsInt,
                                            Filter<T>.SortDelegate sortFieldAscending)
        {
            Filter<T>.FieldInformation info = new Filter<T>.FieldInformation(apiFilterName);
            info.getFieldAsInt = getFieldAsInt;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
        
        internal static void AddFloatField<T>(this Dictionary<int, Filter<T>.FieldInformation> fieldInformationMap,
                                              int fieldIdentifier, string apiFilterName,
                                              Filter<T>.GetFieldDelegate<float> getFieldAsFloat,
                                              Filter<T>.SortDelegate sortFieldAscending)
        {
            Filter<T>.FieldInformation info = new Filter<T>.FieldInformation(apiFilterName);
            info.getFieldAsFloat = getFieldAsFloat;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
        
        internal static void AddStringField<T>(this Dictionary<int, Filter<T>.FieldInformation> fieldInformationMap,
                                               int fieldIdentifier, string apiFilterName,
                                               Filter<T>.GetFieldDelegate<string> getFieldAsString,
                                               Filter<T>.SortDelegate sortFieldAscending)
        {
            Filter<T>.FieldInformation info = new Filter<T>.FieldInformation(apiFilterName);
            info.getFieldAsString = getFieldAsString;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
        
        internal static void AddStringArrayField<T>(this Dictionary<int, Filter<T>.FieldInformation> fieldInformationMap,
                                                    int fieldIdentifier, string apiFilterName,
                                                    Filter<T>.GetFieldDelegate<string[]> getFieldAsStringArray,
                                                    Filter<T>.SortDelegate sortFieldAscending)
        {
            Filter<T>.FieldInformation info = new Filter<T>.FieldInformation(apiFilterName);
            info.getFieldAsStringArray = getFieldAsStringArray;
            info.sortAscendingDelegate = sortFieldAscending;
            fieldInformationMap[fieldIdentifier] = info;
        }
    }

    // ---------[ QUERY FILTERS ]---------
    public class GetAllModsFilter : Filter<Mod>
    {
        public enum Field
        {
            ID,
            GameID,
            SubmittedBy,
            DateAdded,
            DateUpdated,
            DateLive,
            Logo,
            Homepage,
            Name,
            NameID,
            Summary,
            Description,
            MetadataBlob,
            Modfile,
            Price,
            Tags,
            Downloads,
            Popularity,
            Ratings,
            Subscribers,
        }

        private const int FIELD_ID_STATUS = -1;
        public enum ModStatus
        {
            Authorized, // auth = Only return authorized mods (default).
            Unauthorized, // unauth = Only return un-authorized mods.
            Banned, // ban = Only return banned mods.
            Archived, // archive = Only return archived content (out of date builds).
            Deleted, // delete = Only return deleted mods.
        }

        public readonly static new GetAllModsFilter NONE = new GetAllModsFilter();

        // ---------[ FIELD MAPPING ]---------
        private static Dictionary<int, FieldInformation> fieldInformationMap;

        static GetAllModsFilter()
        {
            fieldInformationMap = new Dictionary<int, FieldInformation>(Enum.GetNames(typeof(Field)).Length);

            // integer(int32)  Unique id of the mod.
            fieldInformationMap.AddIntField<Mod>((int)Field.ID, "id",
                                                 mod => mod.ID,
                                                 (a,b) => { return a.ID.CompareTo(b.ID); });
            // integer(int32)  Unique id of the parent game.
            fieldInformationMap.AddIntField<Mod>((int)Field.GameID, "game_id",
                                                 mod => mod.gameID,
                                                 (a,b) => { return a.gameID.CompareTo(b.gameID); });
            // integer(int32)  Unique id of the user who has ownership of the game.
            fieldInformationMap.AddIntField<Mod>((int)Field.SubmittedBy, "submitted_by",
                                                 mod => mod.submittedBy.ID,
                                                 (a,b) => { return a.submittedBy.ID.CompareTo(b.submittedBy.ID); });
            // integer(int32)  Unix timestamp of date registered.
            fieldInformationMap.AddIntField<Mod>((int)Field.DateAdded, "date_added",
                                                 mod => mod.dateAdded,
                                                 (a,b) => { return a.dateAdded.CompareTo(b.dateAdded); });
            // integer(int32)  Unix timestamp of date updated.
            fieldInformationMap.AddIntField<Mod>((int)Field.DateUpdated, "date_updated",
                                                 mod => mod.dateUpdated,
                                                 (a,b) => { return a.dateUpdated.CompareTo(b.dateUpdated); });
            // integer(int32)  Unix timestamp of date mod was set live.
            fieldInformationMap.AddIntField<Mod>((int)Field.DateLive, "date_live",
                                                 mod => mod.dateLive,
                                                 (a,b) => { return a.dateLive.CompareTo(b.dateLive); });

            // string  The filename of the logo.
            fieldInformationMap.AddStringField<Mod>((int)Field.Logo, "logo",
                                                    mod => mod.logo.filename,
                                                    (a,b) => { return a.logo.filename.CompareTo(b.logo.filename); });
            // string  Official homepage of the mod.
            fieldInformationMap.AddStringField<Mod>((int)Field.Homepage, "homepage",
                                                    mod => mod.homepage,
                                                    (a,b) => { return a.homepage.CompareTo(b.homepage); });
            // string  Name of the mod.
            fieldInformationMap.AddStringField<Mod>((int)Field.Name, "name",
                                                    mod => mod.name,
                                                    (a,b) => { return a.name.CompareTo(b.name); });
            // string  The unique SEO friendly URL for your game.
            fieldInformationMap.AddStringField<Mod>((int)Field.NameID, "name_id",
                                                    mod => mod.nameID,
                                                    (a,b) => { return a.nameID.CompareTo(b.nameID); });
            // string  Summary of the mod.
            fieldInformationMap.AddStringField<Mod>((int)Field.Summary, "summary",
                                                    mod => mod.summary,
                                                    (a,b) => { return a.summary.CompareTo(b.summary); });
            // string  An extension of the summary. HTML Supported.
            fieldInformationMap.AddStringField<Mod>((int)Field.Description, "description",
                                                    mod => mod.description,
                                                    (a,b) => { return a.description.CompareTo(b.description); });
            // string  Comma-separated list of metadata words.
            fieldInformationMap.AddStringField<Mod>((int)Field.MetadataBlob, "metadata_blob",
                                                    mod => mod.metadataBlob,
                                                    (a,b) => { return a.metadataBlob.CompareTo(b.metadataBlob); });
            // integer(int32)  Unique id of the Modfile Object marked as current release.
            fieldInformationMap.AddIntField<Mod>((int)Field.Modfile, "modfile",
                                                 mod => mod.modfile.ID,
                                                 (a,b) => { return a.modfile.ID.CompareTo(b.modfile.ID); });
            // TODO(@jackson): Check this filter name
            // string  Sort results by weighted rating using _sort filter, value should be ratings for descending or -ratings for ascending results.
            fieldInformationMap.AddFloatField<Mod>((int)Field.Ratings, "ratings",
                                                   mod => mod.ratingSummary.weightedAggregate,
                                                   (a,b) => { return a.ratingSummary.weightedAggregate.CompareTo(b.ratingSummary.weightedAggregate); });

            // string  OAuth 2 only. The status of the mod (only recognised by game admins), default is 'auth'.
            fieldInformationMap.AddStringField<Mod>(FIELD_ID_STATUS, "status",
                                                    (mod) => { Debug.LogError("Filtering on status locally is currently not implemented"); return ""; },
                                                    (a,b) => { Debug.LogWarning("Sorting on status locally is currently not implemented"); return a.ID.CompareTo(b.ID); });

            
            // --- Currently unable to be filtered/sorted locally ---
            // string  Sort results by most subscribers using _sort filter, value should be subscribers for descending or -subscribers for ascending results.
            fieldInformationMap.AddStringField<Mod>((int)Field.Subscribers, "subscribers",
                                                    mod => { Debug.LogError("Filtering on subscribers locally is currently not implemented"); return ""; },
                                                    (a,b) => { Debug.LogWarning("Sorting on subscribers locally is currently not implemented"); return a.ID.CompareTo(b.ID); });
            // string  Sort results by most downloads using _sort filter parameter, value should be downloads for descending or -downloads for ascending results.
            fieldInformationMap.AddStringField<Mod>((int)Field.Downloads, "downloads",
                                                    mod => { Debug.LogError("Filtering on downloads locally is currently not implemented"); return ""; },
                                                    (a,b) => { Debug.LogWarning("Sorting on downloads locally is currently not implemented"); return a.ID.CompareTo(b.ID); });
            // string  Sort results by popularity using _sort filter, value should be popular for descending or -popular for ascending results.
            fieldInformationMap.AddStringField<Mod>((int)Field.Popularity, "popular",
                                                    mod => { Debug.LogError("Filtering on popularity locally is currently not implemented"); return ""; },
                                                    (a,b) => { Debug.LogWarning("Sorting on popularity locally is currently not implemented"); return a.ID.CompareTo(b.ID); });


            // string  Comma-separated values representing the tags you want to filter the results by.
            //      Only tags that are supported by the parent game can be applied.
            //      To determine what tags are eligible, see the tags values within 'Tag Options' column on the parent Game Object.
            fieldInformationMap.AddStringArrayField<Mod>((int)Field.Tags, "tags",
                                                         mod => mod.GetTagNames(),
                                                         (a,b) => { Debug.LogError("The 'tags' attribute cannot be sorted on"); return a.ID.CompareTo(b.ID); });
        }

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModsFilter() : base("id", (a,b) => a.ID.CompareTo(b.ID))
        {
        }
        internal override FieldInformation GetFieldInformation(int fieldIdentifier)
        {
            return GetAllModsFilter.fieldInformationMap[fieldIdentifier];
        }

        public override void ResetSorting()
        {
            ApplySortAscending(Field.ID);
        }

        // ------[ SORTING APPLICATION ]------
        public void ApplySortAscending(Field field)
        {
            base._ApplySortAscending((int)field);
        }
        public void ApplySortDescending(Field field)
        {
            base._ApplySortAscending((int)field);
        }

        // ---------[ FILTER MODIFICATION ]---------
        public void ClearOnField(Field field)
        {
            base._ClearOnField((int)field);
        }
        public void ClearStatus()
        {
            base._ClearOnField(FIELD_ID_STATUS);
        }

        // ---[ SPECIALIZED FILTERS ]---
        public void ApplyNameQuery(string query)
        {
            filterStringMap[(int)Field.Name] = "_q=" + query;
            filterDelegateMap[(int)Field.Name] = mod => mod.name.Contains(query);
        }
        public void ApplyStatusEquals(ModStatus status)
        {
            string filterValue = "";
            switch(status)
            {
                case ModStatus.Authorized:
                {
                    filterValue = "auth";
                }
                break;
                case ModStatus.Unauthorized:
                {
                    filterValue = "unauth";
                }
                break;
                case ModStatus.Banned:
                {
                    filterValue = "ban";
                }
                break;
                case ModStatus.Archived:
                {
                    filterValue = "archive";
                }
                break;
                case ModStatus.Deleted:
                {
                    filterValue = "delete";
                }
                break;
            }

            base._ApplyStringEquality(FIELD_ID_STATUS, filterValue);
        }

        // ---[ INTEGER FILTERS ]---
        public void ApplyIntEquality(Field field, int value)
        {
            base._ApplyIntEquality((int)field, value);
        }
        public void ApplyIntInequality(Field field, int value)
        {
            base._ApplyIntInequality((int)field, value);
        }
        public void ApplyIntInArray(Field field, int[] values)
        {
            base._ApplyIntInArray((int)field, values);
        }
        public void ApplyIntNotInArray(Field field, int[] values)
        {
            base._ApplyIntNotInArray((int)field, values);
        }
        public void ApplyIntMinimum(Field field, int value, bool isValueInclusive)
        {
            base._ApplyIntMinimum((int)field, value, isValueInclusive);
        }
        public void ApplyIntMaximum(Field field, int value, bool isValueInclusive)
        {
            base._ApplyIntMaximum((int)field, value, isValueInclusive);
        }
        public void ApplyIntRange(Field field,
                                           int minimum, bool isMinimumInclusive,
                                           int maximum, bool isMaximumInclusive)
        {
            base._ApplyIntRange((int)field,
                                         minimum, isMinimumInclusive,
                                         maximum, isMaximumInclusive);
        }

        // ---[ FLOAT FILTERS ]---
        public void ApplyFloatEquality(Field field, float value)
        {
            base._ApplyFloatEquality((int)field, value);
        }
        public void ApplyFloatInequality(Field field, float value)
        {
            base._ApplyFloatInequality((int)field, value);
        }
        public void ApplyFloatInArray(Field field, float[] values)
        {
            base._ApplyFloatInArray((int)field, values);
        }
        public void ApplyFloatNotInArray(Field field, float[] values)
        {
            base._ApplyFloatNotInArray((int)field, values);
        }
        public void ApplyFloatMinimum(Field field, float value, bool isValueInclusive)
        {
            base._ApplyFloatMinimum((int)field, value, isValueInclusive);
        }
        public void ApplyFloatMaximum(Field field, float value, bool isValueInclusive)
        {
            base._ApplyFloatMaximum((int)field, value, isValueInclusive);
        }
        public void ApplyFloatRange(Field field,
                                             float minimum, bool isMinimumInclusive,
                                             float maximum, bool isMaximumInclusive)
        {
            base._ApplyFloatRange((int)field,
                                           minimum, isMinimumInclusive,
                                           maximum, isMaximumInclusive);
        }

        // ---[ STRING FILTERS ]---
        public void ApplyStringEquality(Field field, string value)
        {
            base._ApplyStringEquality((int)field, value);
        }
        public void ApplyStringInequality(Field field, string value)
        {
            base._ApplyStringInequality((int)field, value);
        }
        public void ApplyStringInArray(Field field, string[] values)
        {
            base._ApplyStringInArray((int)field, values);
        }
        public void ApplyStringNotInArray(Field field, string[] values)
        {
            base._ApplyStringNotInArray((int)field, values);
        }
        public void ApplyStringLike(Field field, string value)
        {
            base._ApplyStringLike((int)field, value);
        }
        public void ApplyStringNotLike(Field field, string value)
        {
            base._ApplyStringNotLike((int)field, value);
        }

        // ---[ ARRAY FILTERS ]---
        public void ApplyStringArrayContains(Field field, string value)
        {
            base._ApplyStringArrayContains((int)field, value);
        }
        public void ApplyStringArrayContainsAll(Field field, string[] values)
        {
            base._ApplyStringArrayContainsAll((int)field, values);
        }
    }

    public class GetAllModfilesFilter : Filter<Modfile>
    {
        public static readonly new GetAllModfilesFilter NONE = new GetAllModfilesFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModfilesFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetGameActivityFilter : Filter<GameActivity>
    {
        public static readonly new GetGameActivityFilter NONE = new GetGameActivityFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetGameActivityFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllModActivityByGameFilter : Filter<ModActivity>
    {
        public static readonly new GetAllModActivityByGameFilter NONE = new GetAllModActivityByGameFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModActivityByGameFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetModActivityFilter : Filter<ModActivity>
    {
        public static readonly new GetModActivityFilter NONE = new GetModActivityFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetModActivityFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }
        
        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllModTagsFilter : Filter<ModTag>
    {
        public static readonly new GetAllModTagsFilter NONE = new GetAllModTagsFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModTagsFilter() : base("name", (a,b) => a.name.CompareTo(b.name))
        {
        }

        public override void ResetSorting()
        {
            sortString = "name";
            sortDelegate = (a,b) => { return a.name.CompareTo(b.name); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllModKVPMetadataFilter : Filter<MetadataKVP>
    {
        public static readonly new GetAllModKVPMetadataFilter NONE = new GetAllModKVPMetadataFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModKVPMetadataFilter() : base("id", (a,b) => a.key.CompareTo(b.key))
        {
        }

        public override void ResetSorting()
        {
            sortString = "key";
            sortDelegate = (a,b) => { return a.key.CompareTo(b.key); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllModDependenciesFilter : Filter<ModDependency>
    {
        public static readonly new GetAllModDependenciesFilter NONE = new GetAllModDependenciesFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModDependenciesFilter() : base("id", (a,b) => a.modID - b.modID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "mod_id";
            sortDelegate = (a,b) => { return a.modID.CompareTo(b.modID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllGameTeamMembersFilter : Filter<TeamMember>
    {
        public static readonly new GetAllGameTeamMembersFilter NONE = new GetAllGameTeamMembersFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllGameTeamMembersFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllModTeamMembersFilter : Filter<TeamMember>
    {
        public static readonly new GetAllModTeamMembersFilter NONE = new GetAllModTeamMembersFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModTeamMembersFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllModCommentsFilter : Filter<Comment>
    {
        public static readonly new GetAllModCommentsFilter NONE = new GetAllModCommentsFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllModCommentsFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetAllUsersFilter : Filter<User>
    {
        public static readonly new GetAllUsersFilter NONE = new GetAllUsersFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetAllUsersFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }

    public class GetUserSubscriptionsFilter : Filter<Mod>
    {
        public static readonly new GetUserSubscriptionsFilter NONE = new GetUserSubscriptionsFilter();

        // ---------[ ABSTRACT IMPLEMENTATION ]---------
        public GetUserSubscriptionsFilter() : base("id", (a,b) => a.ID - b.ID)
        {
        }

        public override void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.ID.CompareTo(b.ID); };
        }

        public override string GenerateQueryString()
        {
            Debug.Assert(ModManager.APIClient != null
                         && ModManager.APIClient.gameID > -1,
                         "This filter cannot be used until the ModManager has been initialized and the APIClient given a valid Game ID");

            return base.GenerateQueryString() + "&game_id=" + ModManager.APIClient.gameID;
        }
        
        internal override FieldInformation GetFieldInformation(int fieldIdentifier) { return null; }
    }
}