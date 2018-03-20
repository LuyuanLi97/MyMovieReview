using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace MyMovieReview
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewPage : Page
    {
        public NewPage()
        {
            InitializeComponent();
            var viewTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.BackgroundColor = Windows.UI.Colors.CornflowerBlue;
            viewTitleBar.ButtonBackgroundColor = Windows.UI.Colors.CornflowerBlue;
        }

        private ViewModels.MovieItemViewModel ViewModel;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            //查看是否可以返回，若可以返回，返回按钮视为可见，否则折叠
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = rootFrame.CanGoBack ?
            AppViewBackButtonVisibility.Visible :
            AppViewBackButtonVisibility.Collapsed;
            ViewModel = ((ViewModels.MovieItemViewModel)e.Parameter);
            if (ViewModel.SelectItem == null)
            {
                //当没有电影项被选中时，按钮显示为create
                CreateBtn.Content = "Create";
                //var i = new MessageDialog("Welcome!").ShowAsync();
            }
            else
            {
                //当电影项被选中时，按钮显示为update
                CreateBtn.Content = "Update";
                //将相关数据赋值给对应条目，使被选中的电影项的数据显示出来
                Description.Text = ViewModel.SelectItem.description;
                Title.Text = ViewModel.SelectItem.title;
                Rank.SelectedValue = ViewModel.SelectItem.rank;
                Review.Text = ViewModel.SelectItem.review;
                // ...
            }
        }

        private void CreateButton_Clicked(object sender, RoutedEventArgs e)
        {
            // check the textbox and datapicker
            // if ok
            if (ViewModel.SelectItem != null)
            {
                //如果有电影项被选中，即为要更新对应电影项的信息
                //更新数据库里的信息
                var db = App.conn;
                using (var movieItem = db.Prepare("UPDATE movieLists SET title = ?,description = ?, rank = ?, review = ? WHERE id = ?"))
                {
                    movieItem.Bind(1, Title.Text);
                    movieItem.Bind(2, Description.Text);
                    movieItem.Bind(3, Rank.SelectedIndex);
                    movieItem.Bind(4, Review.Text);
                    movieItem.Bind(5, ViewModel.SelectItem.getId());
                    movieItem.Step();
                }
                // 更新ViewModel中的相关数据
                ViewModel.UpdateMovieItem(Title.Text, Description.Text, Rank.SelectedIndex, Review.Text);
            }
            else
            {
                //没有电影项被选中，即需要添加新的电影项
                //往数据库内插入新电影项
                var db = App.conn;
                using (var movieItem = db.Prepare("INSERT INTO movieLists(title, description, rank, review) VALUES(?, ?, ?, ?)"))
                {
                    movieItem.Bind(1, Title.Text);
                    movieItem.Bind(2, Description.Text);
                    movieItem.Bind(3, Rank.SelectedIndex);
                    movieItem.Bind(4, Review.Text);
                    movieItem.Step();
                }
                //添加新的电影项
                ViewModel.AddMovieItem(Title.Text, Description.Text, Rank.SelectedIndex, Review.Text);
            }

            //更新动态磁贴
            XmlDocument tile = new XmlDocument();
            tile.LoadXml(File.ReadAllText("tile.xml"));

            XmlNodeList elemList = tile.GetElementsByTagName("text");
            elemList[0].InnerText = Title.Text;
            elemList[1].InnerText = Title.Text;
            elemList[2].InnerText = "Rank:" + Rank.SelectedIndex.ToString();
            elemList[3].InnerText = Title.Text;
            elemList[4].InnerText = "Rank:" + Rank.SelectedIndex.ToString();
            elemList[5].InnerText = "Description:" + Description.Text;
            elemList[6].InnerText = Title.Text;
            elemList[7].InnerText = "Rank:" + Rank.SelectedIndex.ToString();
            elemList[8].InnerText = "Description:" + Description.Text;
            elemList[9].InnerText = "Review:" + Review.Text;

            var updator = TileUpdateManager.CreateTileUpdaterForApplication();
            var notification = new TileNotification(tile);
            updator.Update(notification);

            //返回MainPage
            Frame.Navigate(typeof(MainPage), ViewModel);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //将各数据条目内信息清空
            Title.Text = "";
            Description.Text = "";
            Review.Text = "";
            Rank.SelectedValue = 5;
        }

        private void DeleteAppBarBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectItem != null)
            {
                //如果有电影项被选中
                //将数据库中对应信息删除
                var db = App.conn;
                using (var statement = db.Prepare("DELETE FROM movieLists WHERE id = ?"))
                {
                    statement.Bind(1, ViewModel.SelectItem.getId());
                    statement.Step();
                }
                //将ViewModel中的对应电影项删除
                ViewModel.RemoveItem();
            }
            //返回MainPage
            Frame.Navigate(typeof(MainPage), ViewModel);
        }
    }
}
