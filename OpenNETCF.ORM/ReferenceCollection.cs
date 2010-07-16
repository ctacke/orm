using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace OpenNETCF.ORM
{
    public interface IReferenceCollection
    {
        object[] GetNewItems();
        void ClearNewItems();
    }

    public class ReferenceCollection<T> : IEnumerable<T>, IEnumerable, IReferenceCollection
        where T : class
    {
        private List<T> m_items = new List<T>();
        private List<T> m_newItems = new List<T>(); 

        public IEnumerator<T> GetEnumerator()
        {
            return m_items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_items.GetEnumerator();
        }

        public T this[int index]
        {
            get { return m_items[index]; }
            set 
            {
                // TODO: deal with insert/update
                m_items[index] = value; 
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        //internal void AddRange(IEnumerable<object> items)
        //{
        //    foreach (var item in items)
        //    {
        //        Add((T)item);
        //    }
        //}

        public void Add(T item)
        {
            Add(item, false);
        }

        internal void Add(T item, bool addedFromStore)
        {
            m_items.Add(item);
            if (!addedFromStore)
            {
                m_newItems.Add(item);
            }
        }

        public object[] GetNewItems()
        {
            return m_newItems.ToArray();
        }

        public void ClearNewItems()
        {
            m_newItems.Clear();
        }
    }
}
