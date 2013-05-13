using System;
using System.Collections;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TabNavApp.Api.Documents;
using System.Collections.Generic;
using TabNavApp.Api.Tags;

namespace TabNavApp.Api.Common
{
    public class ListController<T>
    {
        public static Stack<ListState<T>> lastSearchStack = new Stack<ListState<T>>(30);
        public static Stack<ListState<T>> nextSearchStack = new Stack<ListState<T>>(30);

        private ListBox list;
        private List<T> stickiedList;
        private List<T> itemList;
        private T[] stickiedListLast;
        private T[] itemListLast;

        public ListController(ListBox list)
        {
            this.list = list;
            itemList = new List<T>();
            stickiedList = new List<T>();
        }

        public ListBox List
        {
            set
            {
                this.list = value;
            }
        }

        public List<T> Items
        {
            get { 
                //List<T> zw = itemList;
                //zw.InsertRange(0, stickiedList);
                return itemList;
            }
        }

        public List<T> StickedItems
        {
            get { return stickiedList; }
            set { stickiedList = value; }
        }

        public List<DependencyObject> Childs
        {
            get
            {
                List<DependencyObject> childs = new List<DependencyObject>();
                foreach (Item it in list.Items)
                    childs.Add(it.Control);
                return childs;
            }
        }

        public void Add(T[] items)
        {
            foreach (T t in items)
            {
                if (t != null)
                {
                    (t as Item).Background = null;
                }
            }
            itemList.AddRange(items);
        }

        public void Add(T item)
        {
            Add(new T[] { item });
        }

        public void Remove(T[] items)
        {
            foreach (T document in items)
                if (itemList.Contains(document))
                    itemList.Remove(document);
        }

        public void Remove(T item)
        {
            Remove(new T[] { item });
        }

        public void Unstick(T[] items)
        {
            foreach (T item in items)
            {
                Unstick(item);
            }
        }

        public void Unstick(T item)
        {
            if (stickiedList.Contains(item))
            {
                removeStickied(item);
                Add(item);
            }
        }

        public void Stick(T[] items)
        {
            foreach (T item in items)
            {
                Stick(item);
            }
        }

        public void Stick(T item)
        {
            if (itemList.Contains(item))
            {
                Remove(item);
                addStickied(item);
            }
        }

        public void ToggleStickied(T item)
        {
            if (stickiedList.Contains(item))
            {
                Unstick(item);
            }
            else if (itemList.Contains(item))
            {
                Stick(item);
            }
        }

        public void ToggleStickied(T[] items)
        {
            foreach (T item in items)
                ToggleStickied(item);
        }

        public void Update(bool changeHistoryStacks)
        {
            if (changeHistoryStacks)
            {
                if (stickiedListLast != null && itemListLast != null)
                {
                    if (stickiedList.Count > 0 || itemList.Count > 0)
                    {
                        lastSearchStack.Push(new ListState<T>(stickiedListLast, itemListLast));
                        nextSearchStack.Clear();
                    }
                }
                //else
                //{
                //    if (stickiedList.Count > 0 || itemList.Count > 0)
                //    {
                //        lastSearchStack.Push(new ListState<T>(stickiedList, itemList));
                //        nextSearchStack.Clear();
                //    }
                //}
                if (stickiedList.Count > 0 || itemList.Count > 0)
                {
                    stickiedListLast = new T[stickiedList.Count];
                    itemListLast = new T[itemList.Count];
                    stickiedList.CopyTo(stickiedListLast);
                    itemList.CopyTo(itemListLast);
                }
            }
            List<T> fullList = new List<T>();
            fullList.AddRange(stickiedList);
            fullList.AddRange(itemList);
            list.ItemsSource = fullList;
        }

        public void ClearAll()
        {
            itemList.Clear();
            stickiedList.Clear();
        }

        public void Clear()
        {
            itemList.Clear();
        }

        private void addStickied(T[] items)
        {
            foreach (T item in items)
                addStickied(item);
        }

        private void addStickied(T item)
        {
            (item as Item).Background = Constants.brush_stickied;
            stickiedList.Add(item);
        }

        private void removeStickied(T item)
        {
            stickiedList.Remove(item);
        }
    }
}
