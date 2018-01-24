
namespace ModIO
{
    public interface IAPIObjectWrapper<T> where T:struct
    {
        void WrapAPIObject(T apiObject);
        T GetAPIObject();
    }
}

// , IAPIObjectWrapper<_APIOBJ_>
//     {
        
//         // - IAPIObjectWrapper Interface -
//         public void WrapAPIObject(_APIOBJ_ apiObject)
//         {
//             this._data = apiObject;
//         }

//         public _APIOBJ_ GetAPIObject()
//         {
//             return this._data;
//         }