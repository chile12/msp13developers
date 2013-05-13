using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace TabNavApp.Api.Common
{
    public class ListState<T>
    {
        private T[] stickiedList;
        private T[] itemList;

        public T[] StickiedList
        {
            get
            {
                if (stickiedList != null)
                {
                    foreach (T t in stickiedList)
                    {
                        if (t != null)
                        {
                            (t as Item).Background = Constants.brush_stickied;
                        }
                    }
                }
                return stickiedList;
            }
            set
            {
                stickiedList = value;
            }
        }

        public T[] ItemList
        {
            get
            {
                if (itemList != null && itemList[0] != null)
                {
                    foreach (T t in itemList)
                        (t as Item).Background = Constants.brush_default; 
                }
                return itemList;
            }
            set
            {
                itemList = value;
            }
        }

        public void AddStickedItem(T t){
            T[] zw = new T[stickiedList.Length + 1];
            zw[0] = t;
            Array.ConstrainedCopy(stickiedList, 0, zw, 1, stickiedList.Length);
            stickiedList = zw;
        }

        public void AddItem(T t)
        {
            T[] zw = new T[itemList.Length + 1];
            zw[0] = t;
            Array.ConstrainedCopy(stickiedList, 0, zw, 1, itemList.Length);
            itemList = zw;
        }

        public ListState(T[] stickiedList, T[] itemList)
        {
            this.stickiedList = stickiedList;
            this.itemList = itemList;
        }

    }
}
