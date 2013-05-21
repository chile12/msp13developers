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
    /// <summary>
    /// Class saves the current state of a ListController, so it can be recalled later on
    /// </summary>
    /// <typeparam name="T">in this scenarion T will be Tag or Document</typeparam>
    public class ListState<T>
    {
        private T[] stickiedList;       //the list of sticked Items
        private T[] itemList;           //all non-sticked Items
        /// <summary>
        /// takes and returns the sticked-Items_lsit
        /// </summary>
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
        /// <summary>
        /// gets and returns normal item list
        /// </summary>
        public T[] ItemList
        {
            get
            {
                if (itemList != null && itemList.Length >0)
                {
                    foreach (T t in itemList)
                    {
                        if (t != null)
                            (t as Item).Background = Constants.brush_default;
                    }
                }
                return itemList;
            }
            set
            {
                itemList = value;
            }
        }
        /// <summary>
        /// adds a new sticked Item
        /// </summary>
        /// <param name="t"></param>
        public void AddStickedItem(T t){
            T[] zw = new T[stickiedList.Length + 1];
            zw[0] = t;
            Array.ConstrainedCopy(stickiedList, 0, zw, 1, stickiedList.Length);
            stickiedList = zw;
        }
        /// <summary>
        /// adds a new Item to the ItemList
        /// </summary>
        /// <param name="t"></param>
        public void AddItem(T t)
        {
            T[] zw = new T[itemList.Length + 1];
            zw[0] = t;
            Array.ConstrainedCopy(stickiedList, 0, zw, 1, itemList.Length);
            itemList = zw;
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="stickiedList"></param>
        /// <param name="itemList"></param>
        public ListState(T[] stickiedList, T[] itemList)
        {
            this.stickiedList = stickiedList;
            this.itemList = itemList;
        }

    }
}
