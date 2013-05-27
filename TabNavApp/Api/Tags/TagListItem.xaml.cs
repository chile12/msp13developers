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
        private const int INT_StringlengthForSubLabels = 60;
        private MainView MainView;
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
            Tag tag = Item as Tag;
            if (tag != null && tag.Description != null && tag.Description.Length > INT_StringlengthForSubLabels)
            {
                tag.Description = tag.Description.Substring(0, INT_StringlengthForSubLabels) + "...";
                this.descriptionTB.Text = tag.Description;
            }
            if (tag != null && tag.AltLabels != null && tag.AltLabels.Length > INT_StringlengthForSubLabels)
            {
                tag.AltLabels = tag.AltLabels.Substring(0, INT_StringlengthForSubLabels) + "...";
                this.altLabelsTB.Text = tag.AltLabels;
            }
            if (Item.Background != null)
                this.LayoutRoot.Background = Item.Background;
            else
                this.LayoutRoot.Background = Constants.brush_default;
        }

        /// <summary>
        /// initializes Item and gets the parent of the parent-container
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Initialize(object sender, DependencyPropertyChangedEventArgs e)
        {
            getMainViewFromSender(sender);
            Item = this.DataContext as Item;    //DataContext is a Tag
            Item.Control = this;                
        }
        /// <summary>
        /// since the parent of this control is not been set, the corresponding MainView is somewhere up the visual tree
        /// this methode seeks for the MainView object
        /// </summary>
        /// <param name="sender"></param>
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
        /// <summary>
        /// mouse event: catches double-clicks and toggles sticky-state
        /// </summary>
        /// <param name="sender">this</param>
        /// <param name="e"></param>
        private void LayoutRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MainView.tagController.ToggleStickied((this.DataContext as Tag));
                MainView.tagController.Update(true);
            }
        }
        /// <summary>
        /// event: catches the click on the graph-view button, navigates to the graph-vie-tab and to the clicked concept
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void graphButton_Click(object sender, RoutedEventArgs e)
        {
            //get the MainPage
            var mainPage = sender as DependencyObject;
            //get the MainPage somewhere up the visual-tree
            while (mainPage != null)
            {
                if (mainPage.GetType() == typeof(MainView)){     //save current tag-list
                    (mainPage as MainView).tagController.Update(true);
                    (mainPage as MainView).documentController.Update(true);
                }
                if (mainPage.GetType() == typeof(MainPage))
                    break;

                mainPage = VisualTreeHelper.GetParent(mainPage);
            }
            if (mainPage != null)
            {
                (mainPage as MainPage).ContentFrame.Navigate(new Uri("/SearchGraph", UriKind.Relative));
                //save the selected tag with corresponding graph uri
                (mainPage as MainPage).loadGraphConceptUri = (this.DataContext as Tag).Uri;
                (mainPage as MainPage).loadGraphConceptName = (this.DataContext as Tag).Name;
            }
        }
        /// <summary>
        /// mouse event: catches right-clicks -> calles the contextmenu-load methode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutRoot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MainView.LoadTagListBoxContextMenu(this);
        }

    }
}
