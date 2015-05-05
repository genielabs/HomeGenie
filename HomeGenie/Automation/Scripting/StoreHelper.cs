using System;
using HomeGenie.Service;
using HomeGenie.Data;

namespace HomeGenie.Automation.Scripting
{
    public class StoreHelper
    {
        private TsList<Store> storeList;
        private string storeName;
        public StoreHelper(TsList<Store> storageList, string storeName)
        {
            this.storeList = storageList;
            this.storeName = storeName;
        }

        public ModuleParameter Get(string parameterName)
        {
            var store = GetStore(storeName);
            ModuleParameter value = null;
            value = Service.Utility.ModuleParameterGet(store.Data, parameterName);
            // create parameter if does not exists
            if (value == null)
            {
                value = Service.Utility.ModuleParameterSet(store.Data, parameterName, "");
            }
            return value;
        }

        public TsList<ModuleParameter> List
        {
            get 
            {
                var store = GetStore(this.storeName);
                return store.Data;
            }
        }

        public StoreHelper Remove(string parameterName)
        {
            var store = GetStore(storeName);
            store.Data.RemoveAll(d => d.Name == parameterName);
            return this;
        }

        public void Reset()
        {
            storeList.RemoveAll(s => s.Name == storeName);
        }

        private Store GetStore(string storeName)
        {
            var store = storeList.Find(s => s.Name == storeName);
            // create store if does not exists
            if (store == null)
            {
                store = new Store(storeName);
                storeList.Add(store);
            }
            return store;
        }
    }
}

