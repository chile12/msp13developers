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
using TabNavApp.Api.Common;
using TabNavApp.Views;

namespace TabNavApp.Api.Documents
{
    public partial class DocumentListItem : UserControl
    {
        private DependencyObject mainView;
        private MainView MainView;
        private Item Item;

        public DocumentListItem()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ListItem_DataContextChanged);
        }

        void ListItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Initialize(sender, e);

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

        private void AddOrDeleteButtonClick_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MainView.documentController.ToggleStickied((this.DataContext as Document));
                MainView.documentController.Update(true);
            }
        }

        private void LayoutRoot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MainView.LoadDocumentListBoxContextMenu(this);
        }
    }
}
