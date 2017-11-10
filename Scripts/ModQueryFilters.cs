using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO
{
    public class ModQueryFilter
    {
        public enum Field
        {
            ID = 0,
            Name,
            Author,
            DateRegistered,
            DateUpdated,
            Price,
            Tags
        };
        
        public readonly static ModQueryFilter EMPTY = new ModQueryFilter();

        // ---------[ VARIABLES ]---------
        private int limit = 100;
        private Comparison<Mod> sortDelegate = (Mod a, Mod b) => { return a.id.CompareTo(b.id); };
        private string sortString = "id";

        private delegate bool QueryDelegate(Mod m);
        private Dictionary<Field, QueryDelegate> filterQueryMap = new Dictionary<Field, QueryDelegate>();
        private Dictionary<Field, string> filterStringMap = new Dictionary<Field, string>();

        // ---------[ OUTPUT FUNCTIONS ]---------
        public string GenerateQueryString()
        {
            string filterString = "_limit=" + limit;
            filterString += "&_sort=" + sortString;

            foreach(string fs in filterStringMap.Values)
            {
                filterString += "&" + fs;
            }

            return filterString;
        }
        public Mod[] FilterModList(Mod[] modList)
        {
            List<Mod> filteredList = new List<Mod>(modList.Length);

            foreach(Mod mod in modList)
            {
                bool doAdd = true;
                foreach(QueryDelegate modAccepted in filterQueryMap.Values)
                {
                    if(!modAccepted(mod))
                    {
                        doAdd = false;
                        break;
                    }
                }

                if(doAdd)
                {
                    filteredList.Add(mod);
                }
            }

            filteredList.Sort(sortDelegate);

            Mod[] retVal;

            if(filteredList.Count <= limit)
            {
                retVal = filteredList.ToArray();
            }
            else
            {
                retVal = new Mod[limit];
                filteredList.CopyTo(0, retVal, 0, limit);
            }

            return retVal;
        }

        // ---------[ FILTER MODIFICATION ]---------
        public void SetSortFieldAscending(Field sortField)
        {
            switch(sortField)
            {
                case Field.ID:
                {
                    sortDelegate = (a,b) => { return a.id.CompareTo(b.id); };
                    sortString = "id";
                }
                break;
                case Field.Name:
                {
                    sortDelegate = (a,b) => { return a.name.CompareTo(b.name); };
                    sortString = "name";
                }
                break;
                // case Field.Author:
                // {
                //     sortDelegate = (a,b) => { return a.member.name.CompareTo(b.member.name); };
                // }
                // break;
                case Field.DateRegistered:
                {
                    sortDelegate = (a,b) => { return a.datereg.CompareTo(b.datereg); };
                    sortString = "datereg";
                }
                break;
                case Field.DateUpdated:
                {
                    sortDelegate = (a,b) => { return a.dateup.CompareTo(b.dateup); };
                    sortString = "dateup";
                }
                break;
                case Field.Price:
                {
                    sortDelegate = (a,b) => { return a.price.CompareTo(b.price); };
                    sortString = "price";
                }
                break;

                #if DEBUG
                // #pragma warning disable 0162
                default:
                {
                    Debug.LogError("Unsortable field");
                    return;
                }
                // #pragma warning restore 0162
                #endif
            }
        }
        public void SetSortFieldDescending(Field sortField)
        {
            switch(sortField)
            {
                case Field.ID:
                {
                    sortDelegate = (a,b) => { return b.id.CompareTo(a.id); };
                    sortString = "-id";
                }
                break;
                case Field.Name:
                {
                    sortDelegate = (a,b) => { return b.name.CompareTo(a.name); };
                    sortString = "-name";
                }
                break;
                case Field.DateRegistered:
                {
                    sortDelegate = (a,b) => { return b.datereg.CompareTo(a.datereg); };
                    sortString = "-datereg";
                }
                break;
                case Field.DateUpdated:
                {
                    sortDelegate = (a,b) => { return b.dateup.CompareTo(a.dateup); };
                    sortString = "-dateup";
                }
                break;
                case Field.Price:
                {
                    sortDelegate = (a,b) => { return b.price.CompareTo(a.price); };
                    sortString = "-price";
                }
                break;

                #if DEBUG
                default:
                {
                    Debug.LogError("Unsortable field");
                    return;
                }
                #endif
            }

        }
        public void ResetSorting()
        {
            sortString = "id";
            sortDelegate = (a,b) => { return a.id.CompareTo(b.id); };
        }

        public void ApplyNameQuery(string filterString)
        {
            filterQueryMap[Field.Name] = (Mod m) => { return m.name.Contains(filterString); };
            filterStringMap[Field.Name] = "_q=" + filterString;
        }
        public void RemoveNameFilter()
        {
            filterQueryMap.Remove(Field.Name);
            filterStringMap.Remove(Field.Name);
        }

        public void ApplyMinimumPrice(float minPrice)
        {
            filterQueryMap[Field.Price] = (Mod m) => { return m.price >= minPrice; };
            filterStringMap[Field.Price] = "price-min=" + minPrice.ToString("0.00");
        }
        public void ApplyMaximumPrice(float maxPrice)
        {
            filterQueryMap[Field.Price] = (Mod m) => { return m.price <= maxPrice; };
            filterStringMap[Field.Price] = "price-max=" + maxPrice.ToString("0.00");
        }
        public void ApplyPriceRange(float minPrice, float maxPrice)
        {
            filterQueryMap[Field.Price] = (Mod m) => { return m.price >= minPrice && m.price <= maxPrice; };
            filterStringMap[Field.Price] = "price-min=" + minPrice.ToString("0.00")
                + "&price-max=" + maxPrice.ToString("0.00");
        }
        public void RemovePriceFilter()
        {
            filterQueryMap.Remove(Field.Price);
            filterStringMap.Remove(Field.Price);
        }

        public void ApplyAuthorMatch(int authorID)
        {
            filterQueryMap[Field.Author] = (Mod m) => { return m.member.id == authorID; };
            filterStringMap[Field.Author] = "member=" + authorID;
        }
        public void RemoveAuthorFilter()
        {
            filterQueryMap.Remove(Field.Author);
            filterStringMap.Remove(Field.Author);
        }

        public void ApplySingleTagMatch(string tag)
        {
            filterQueryMap[Field.Tags] = (Mod m) => { return m.tagStrings.Contains(tag); };
            filterStringMap[Field.Tags] = "tags=" + tag;
        }
        public void ApplyMultipleTagMatch(string[] tagList)
        {
            Debug.Assert(tagList.Length > 0);

            filterQueryMap[Field.Tags] = (Mod m) =>
            {
                List<string> matchTags = new List<string>(tagList);
                foreach(string tagString in m.tagStrings)
                {
                    matchTags.Remove(tagString);
                }
                return matchTags.Count == 0;
            };

            string tagListString = tagList[0];
            for(int i = 1; // first tag is already handled
                i < tagList.Length;
                ++i)
            {
                tagListString += "," + tagList[i];
            }

            filterStringMap[Field.Tags] = "tags=" + tagListString;
        }
        public void RemoveTagFilter()
        {
            filterQueryMap.Remove(Field.Tags);
            filterStringMap.Remove(Field.Tags);
        }
    }
}