using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using System.Text;
using SQLitePCL;
using Windows.UI.Popups;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Net.Http;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace MyMovieReview
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            var viewTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.BackgroundColor = Windows.UI.Colors.CornflowerBlue;
            viewTitleBar.ButtonBackgroundColor = Windows.UI.Colors.CornflowerBlue;
            this.ViewModel = new ViewModels.MovieItemViewModel();
        }

        ViewModels.MovieItemViewModel ViewModel { get; set; }

        //当“＋”按钮被按下时，跳转至Newpage（窄页面时）
        private void AddAppBarBtn_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewPage), ViewModel);
        }

        private void Movie_ItemClicked(object sender, ItemClickEventArgs e)
        {
            //获取被选中的电影项
            ViewModel.SelectItem = (Models.MovieItem)(e.ClickedItem);
            if (AddAppBarBtn.IsEnabled)
            {
                //如果是窄页面时，跳转至对应电影项的NewPage
                Frame.Navigate(typeof(NewPage), ViewModel);
            }
            else
            {
                //若是宽页面时
                if (ViewModel.SelectItem == null)
                {
                    //若没有电影项被选中，右侧按钮显示为"Create"
                    SideCreateBtn.Content = "Create";

                }
                else
                {
                    //若没有电影项被选中，右侧按钮显示为"Update"并更新其中的数据条目
                    SideCreateBtn.Content = "Update";
                    SideTitle.Text = ViewModel.SelectItem.title;
                    SideDescription.Text = ViewModel.SelectItem.description;
                    SideRank.SelectedValue = ViewModel.SelectItem.rank;
                    SideReview.Text = ViewModel.SelectItem.review;
                }
            }
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            //viewModels.AddTodoItem(mTitle.Text, mDescription.Text);
            if (ViewModel.SelectItem == null)
            {
                //没有电影项被选中，即需要添加新的电影项
                //往数据库内插入新电影项
                var db = App.conn;
                using (var movieItem = db.Prepare("INSERT INTO movieLists(title, description, rank, review) VALUES(?, ?, ?, ?)"))
                {
                    movieItem.Bind(1, SideTitle.Text);
                    movieItem.Bind(2, SideDescription.Text);
                    movieItem.Bind(3, SideRank.SelectedIndex);
                    movieItem.Bind(4, SideReview.Text);
                    movieItem.Step();
                }
                //添加新的电影项
                ViewModel.AddMovieItem(SideTitle.Text, SideDescription.Text, SideRank.SelectedIndex, SideReview.Text);
            }
            else
            {
                //如果有电影项被选中，即为要更新对应电影项的信息
                //更新数据库里的信息
                var db = App.conn;
                using (var movieItem = db.Prepare("UPDATE movieLists SET title = ?,description = ?, rank = ?, review = ? WHERE id = ?"))
                {
                    movieItem.Bind(1, SideTitle.Text);
                    movieItem.Bind(2, SideDescription.Text);
                    movieItem.Bind(3, SideRank.SelectedIndex);
                    movieItem.Bind(4, SideReview.Text);
                    movieItem.Bind(5, ViewModel.SelectItem.getId());
                    movieItem.Step();
                }
                // 更新ViewModel中的相关数据
                ViewModel.UpdateMovieItem(SideTitle.Text, SideDescription.Text, SideRank.SelectedIndex, SideReview.Text);
            }
            //  Live Tile
            //更新动态磁贴
            XmlDocument tile = new XmlDocument();
            tile.LoadXml(File.ReadAllText("tile.xml"));

            XmlNodeList elemList = tile.GetElementsByTagName("text");
            elemList[0].InnerText = SideTitle.Text;
            elemList[1].InnerText = SideTitle.Text;
            elemList[2].InnerText = "Rank:" + SideRank.SelectedIndex.ToString();
            elemList[3].InnerText = SideTitle.Text;
            elemList[4].InnerText = "Rank:" + SideRank.SelectedIndex.ToString();
            elemList[5].InnerText = "Description:" + SideDescription.Text;
            elemList[6].InnerText = SideTitle.Text;
            elemList[7].InnerText = "Rank:" + SideRank.SelectedIndex.ToString();
            elemList[8].InnerText = "Description:" + SideDescription.Text;
            elemList[9].InnerText = "Review:" + SideReview.Text;

            var updator = TileUpdateManager.CreateTileUpdaterForApplication();
            var notification = new TileNotification(tile);
            updator.Update(notification);

            //刷新页面
            Frame.Navigate(typeof(MainPage), ViewModel);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //将各数据条目内信息清空
            SideTitle.Text = "";
            SideDescription.Text = "";
            SideReview.Text = "";
            SideRank.SelectedValue = "5";
            //mTitle.Text = "";
            //mDescription.Text = "";
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DataTransferManager.GetForCurrentView().DataRequested -= OnShareDataRequested;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;

            if (rootFrame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Collapsed;
            }

            if (e.Parameter.GetType() == typeof(ViewModels.MovieItemViewModel))
            {
                this.ViewModel = (ViewModels.MovieItemViewModel)(e.Parameter);
            }
        }

        private async void MyMovieSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            //通过数据库查实现电影项模糊查找
            StringBuilder sb = new StringBuilder();
            var db = App.conn;
            using(var statement = db.Prepare("SELECT * FROM movieLists WHERE title like ? OR description like ? OR rank like ? OR review like ?"))
            {
                statement.Bind(1, '%' + MyMovieSearchBar.Text + '%');
                statement.Bind(2, '%' + MyMovieSearchBar.Text + '%');
                statement.Bind(3, '%' + MyMovieSearchBar.Text + '%');
                statement.Bind(4, '%' + MyMovieSearchBar.Text + '%');
                while (SQLiteResult.ROW == statement.Step())
                {
                    sb.AppendLine($"id: {statement[0]}; title: {statement[1]}; description: {statement[2]}; rank: {statement[3]}; review: {statement[4]}");
                }
                var messageDialog = new MessageDialog(sb.ToString());
                messageDialog.Commands.Add(new UICommand("OK"));
                await messageDialog.ShowAsync();
            }
        }

        private async void WebMovieSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            WebMovieTitle.Text = WebMovieSearchBar.Text;
            SideTitle.Text = WebMovieSearchBar.Text;

            Uri uri = new Uri("http://v.juhe.cn/movie/index?key=2c9349b8af4d9d07c10a371e7a0cf0b0&title=" + WebMovieSearchBar.Text);
            HttpClient client = new HttpClient();
            string result = await client.GetStringAsync(uri);

            //  the following strings can be used as json text in order to save some api call
            //string result = "{\"resultcode\":\"200\",\"reason\":\"成功的返回\",\"result\":[{\"movieid\":\"232603\",\"rating\":\" - 1\",\"genres\":\"动作\\/ 悬疑\\/ 犯罪\",\"runtime\":null,\"language\":\"汉语普通话\\/ 粤语\",\"title\":\"拆弹专家\",\"poster\":\"http:\\/\\/ img31.mtime.cn\\/ mg\\/ 2016\\/ 06\\/ 06\\/ 113814.34913831_270X405X4.jpg\",\"writers\":\"\",\"film_locations\":\"中国 | 中国香港\",\"directors\":\"邱礼涛\",\"rating_count\":\"6\",\"actors\":\"刘德华 Andy Lau,姜武 Wu Jiang,宋佳 Jia Song\",\"plot_simple\":\"电影讲述了发生在香港红磡隧道里的一次拆弹任务，刘德华扮演一名警队的拆弹专家，要解除姜武饰演的通缉犯制造的炸弹恐怖袭击事件危机，并试图将其抓获。\",\"year\":\"2017\",\"country\":\"中国 | 中国香港\",\"type\":null,\"release_date\":\"20170101\",\"also_known_as\":\"\"}],\"error_code\":0}";
            //string result = "{\"resultcode\":\"200\",\"reason\":\"成功的返回\",\"result\":[{\"movieid\":\"147502\",\"rating\":\"8.3\",\"genres\":\"历史\\/剧情\",\"runtime\":null,\"language\":\"英语\",\"title\":\"泰坦尼克号\",\"poster\":\"http:\\/\\/img21.mtime.cn\\/mt\\/2012\\/03\\/27\\/105223.21161030_270X405X4.jpg\",\"writers\":\"朱利安·费罗斯\",\"film_locations\":\"美国|加拿大|匈牙利|英国\",\"directors\":\"\",\"rating_count\":\"413\",\"actors\":\"Ben Bishop,杰拉丁·萨莫维尔 Geraldine Somerville,李·罗斯 Lee Ross,佩尔迪达·维克斯 Perdita Weeks\",\"plot_simple\":\"总剧情 ITV版《泰坦尼克号》聚焦不同的阶层的乘客、船员，通过多视角来展现泰坦尼克号当时的方方面面。观众们将和角色们一起经历泰坦尼克号沉船前的几个小时，经历那从愉悦到悲痛的落差感以及生离死别。主人公们地位的差别，政治的倾向也许直接影响到他们的生死，还有男女之间的一见钟情和夫妻之间的相濡以沫，可以说，泰坦尼克号的上人物，完全可以 展开 ITV版《泰坦尼克号》聚焦不同的阶层的乘客、船员，通过多视角来展现泰坦尼克号当时的方方面面。观众们将和角色们一起经历泰坦尼克号沉船前的几个小时，经历那从愉悦到悲痛的落差感以及生离死别。主人公们地位的差别，政治的倾向也许直接影响到他们的生死，还有男女之间的一见钟情和夫妻之间的相濡以沫，可以说，泰坦尼克号的上人物，完全可以展现当时英国资产阶级的众生相。\",\"year\":\"2012\",\"country\":\"美国|加拿大|匈牙利|英国\",\"type\":null,\"release_date\":\"20120412\",\"also_known_as\":\"\"},{\"movieid\":\"92572\",\"rating\":\"7.8\",\"genres\":\"动作\\/历史\\/剧情\",\"runtime\":\"85 min\",\"language\":\"德语\",\"title\":\"泰坦尼克号\",\"poster\":\"http:\\/\\/img31.mtime.cn\\/mt\\/2014\\/02\\/23\\/064712.94207588_270X405X4.jpg\",\"writers\":\"Harald Bratt,Hansi Köck,...\",\"film_locations\":\"德国\",\"directors\":\"Herbert Selpin,Werner Klingler\",\"rating_count\":\"19\",\"actors\":\"Sybille Schmitz,Hans Nielsen,Kirsten Heiberg,Ernst Fritz Fürbringer\",\"plot_simple\":null,\"year\":\"1943\",\"country\":\"德国\",\"type\":null,\"release_date\":\"19431110\",\"also_known_as\":\"\"},{\"movieid\":\"40591\",\"rating\":null,\"genres\":\"爱情\\/动作\\/剧情\",\"runtime\":\"173 min \\/ Finland:163 min (DVD)\",\"language\":\"英语\",\"title\":\"泰坦尼克号\",\"poster\":\"http:\\/\\/img31.mtime.cn\\/mt\\/2014\\/02\\/23\\/032605.90912094_270X405X4.jpg\",\"writers\":\"Ross LaManna,Joyce Eliason\",\"film_locations\":\"美国|加拿大\",\"directors\":\"罗伯特·里伯曼\",\"rating_count\":null,\"actors\":\"Eric Schneider,Ron Halder,Byron Lucas,Peter Haworth\",\"plot_simple\":null,\"year\":\"1996\",\"country\":\"美国|加拿大\",\"type\":null,\"release_date\":\"19961117\",\"also_known_as\":\"\"},{\"movieid\":\"28986\",\"rating\":\"8.1\",\"genres\":\"\",\"runtime\":null,\"language\":null,\"title\":\"泰坦尼克号\",\"poster\":\"http:\\/\\/img31.mtime.cn\\/mt\\/986\\/28986\\/28986_270X405X4.jpg\",\"writers\":\"\",\"film_locations\":\"意大利\",\"directors\":\"Pier Angelo Mazzolotti\",\"rating_count\":\"33\",\"actors\":\"Luigi Duse,Giovanni Casaleggio,马里奥·伯纳德 Mario Bonnard\",\"plot_simple\":null,\"year\":\"1915\",\"country\":\"意大利\",\"type\":null,\"release_date\":\"0\",\"also_known_as\":\"\"},{\"movieid\":\"11925\",\"rating\":\"8.9\",\"genres\":\"剧情\\/爱情\",\"runtime\":\"194分钟\",\"language\":\"英语\",\"title\":\"泰坦尼克号\",\"poster\":\"http:\\/\\/img21.mtime.cn\\/mt\\/2012\\/04\\/06\\/101417.97070113_270X405X4.jpg\",\"writers\":\"詹姆斯·卡梅隆\",\"film_locations\":\"美国\",\"directors\":\"詹姆斯·卡梅隆\",\"rating_count\":\"61108\",\"actors\":\"莱昂纳多·迪卡普里奥 Leonardo DiCaprio,凯特·温丝莱特 Kate Winslet,比利·赞恩 Billy Zane,格劳瑞亚·斯图尔特 Gloria Stuart\",\"plot_simple\":\"影片以1912年泰坦尼克号邮轮在其处女启航时触礁冰山而沉没的事件为背景，描述了处于不同阶层的两个人——穷画家杰克和贵族女露丝抛弃世俗的偏见坠入爱河，最终杰克把生命的机会让给了露丝的感人故事。\",\"year\":\"1997\",\"country\":\"美国\",\"type\":null,\"release_date\":\"20120410\",\"also_known_as\":\"铁达尼号\"}],\"error_code\":0}";
            //string result = "{\"resultcode\":\"200\",\"reason\":\"成功的返回\",\"result\":[],\"error_code\":0}";
            JsonTextReader json = new JsonTextReader(new StringReader(result));
            json.Read();
            json.Read();
            json.Read();
            json.Read();
            json.Read();
            json.Read();
            json.Read();
            json.Read();
            json.Read();

            //  if the "result" in JSON is empty
            if (json.Value.ToString() == "error_code")
            {
                WebMovieInfo.Text = "Unsuccessful search. Please try another name.";
                return;
            }
            //  if there are more than one results, show the first one
            bool isSetInfo = false;
            bool isSetPoster = false;
            while (json.Read())
            {
                if (json.Value != null && json.Value.Equals("plot_simple"))
                {
                    json.Read();
                    if (json.Value != null && !isSetInfo) {
                        WebMovieInfo.Text = json.Value.ToString();
                        SideReview.Text = WebMovieInfo.Text;
                        isSetInfo = true;
                    }
                }
                if (json.Value != null && json.Value.Equals("poster"))
                {
                    json.Read();
                    if (json.Value != null && !isSetPoster)
                    {
                        string rawUri = json.Value.ToString();
                        rawUri = rawUri.Replace(" ", "");
                        Uri imageUri = new Uri(rawUri);
                        Windows.Web.Http.HttpClient http = new Windows.Web.Http.HttpClient();
                        IBuffer buffer = await http.GetBufferAsync(imageUri);
                        BitmapImage img = new BitmapImage();
                        using (IRandomAccessStream stream = new InMemoryRandomAccessStream())
                        {
                            await stream.WriteAsync(buffer);
                            stream.Seek(0);
                            await img.SetSourceAsync(stream);
                        }
                        WebMoviePoster.Source = img;
                        isSetPoster = true;
                    }
                }
            }
        }
        //影评分享
        dynamic itemSelected;
        private void ShareBtn_Click(object sender, RoutedEventArgs e)
        {
            itemSelected = ((AppBarButton)sender).DataContext;
            DataTransferManager.ShowShareUI();
        }

        private void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.Properties.Title = itemSelected.title;
            request.Data.Properties.Description = "Share movie name, description, rank and review.";
            request.Data.SetText("Description:" + itemSelected.description + "\nRank:" + itemSelected.rank + "\nReview:" + itemSelected.review);
        }

        private async void ChangeImgAppBarBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(stream);
                WebMoviePoster.Source = bmp;
            }
        }
    }
}