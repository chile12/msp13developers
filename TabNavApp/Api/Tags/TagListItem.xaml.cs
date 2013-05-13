using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using TabNavApp.Api.Common;
using TabNavApp.Views;

namespace TabNavApp.Api.Tags
{
    /// <summary>
    /// this User-Control is used as ListBoxItem in the TagSearchListBox
    /// </summary>
    public partial class TagListItem : UserControl
    {
        private MainView MainView;
        private DependencyObject currentDpendencyObject;
        private Item Item;
        /// <summary>
        /// ...
        /// </summary>
        public TagListItem()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ListItem_DataContextChanged);
        }

        /// <summary>
        /// fires after initializeComponent(), registers this UI-Control at it's parents UI-Control list (since there is no inherited methode for getting Control-list-items of ListBox)
        /// </summary>
        /// <param name="sender">this</param>
        /// <param name="e">not needed</param>
        void ListItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Initialize(sender, e);
            this.currentDpendencyObject = MainView;
            Tag tag = Item as Tag;
            if (tag != null && tag.Description != null && tag.Description.Length > 50)
            {
                tag.Description = tag.Description.Substring(0, 50) + "...";
                this.descriptionTB.Text = tag.Description;
            }

            if (Item.Background != null)
                this.LayoutRoot.Background = Item.Background;
            else
                this.LayoutRoot.Background = Constants.brush_default;
        }


        private void Initialize(object sender, DependencyPropertyChangedEventArgs e)
        {
            getMainViewFromSender(sender);
            Item = this.DataContext as Item;
            Item.Control = this;
        }

        private void getMainViewFromSender(object sender)
        {
            DependencyObject mainView = sender as DependencyObject;
            while (mainView != null)
            {
                if (mainView is TabNavApp.Views.MainView)
                    break;

                mainView = VisualTreeHelper.GetParent(mainView);
            }
            this.MainView = mainView as MainView;
        }

        /// <summary>
        /// internal click-methode fot the tag-button, calls external click-methode in MainView
        /// </summary>
        /// <param name="sender">the button</param>
        /// <param name="e">not needed</param>
        private void tagButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainView != null)       //call click-methode
                MainView.tagButtonClick(this, e);
        }

        private void LayoutRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MainView.tagController.ToggleStickied((this.DataContext as Tag));
                MainView.tagController.Update(true);
            }
        }

        private void graphButton_Click(object sender, RoutedEventArgs e)
        {
            //get the MainPage
            var mainPage = currentDpendencyObject;
            while (mainPage != null)
            {
                if (mainPage.GetType() == typeof(MainPage))
                    break;

                mainPage = VisualTreeHelper.GetParent(mainPage);
            }
            if (mainPage != null)
            {
                (mainPage as MainPage).ContentFrame.Navigate(new Uri("/SearchGraph", UriKind.Relative));
                (mainPage as MainPage).loadGraphConceptUri = (this.DataContext as Tag).Uri;
                (mainPage as MainPage).loadGraphConceptName = (this.DataContext as Tag).Name;
            }
        }

        private void LayoutRoot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MainView.LoadTagListBoxContextMenu(this);
        }

    }
}
