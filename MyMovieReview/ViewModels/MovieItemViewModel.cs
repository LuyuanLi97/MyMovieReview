using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using SQLitePCL;

namespace MyMovieReview.ViewModels
{
    class MovieItemViewModel
    {
        private ObservableCollection<Models.MovieItem> allItems = new ObservableCollection<Models.MovieItem>();
        public ObservableCollection<Models.MovieItem> AllItems { get { return this.allItems; } }

        private Models.MovieItem selectitem = default(Models.MovieItem);
        public Models.MovieItem SelectItem { get { return selectitem; } set { this.selectitem = value; } }

        public MovieItemViewModel()
        {
            //this.allItems.Add(new Models.MovieItem("test_title", "test_description", 5, "test_review"));
            //this.allItems.Add(new Models.MovieItem("test_title2", "test_description2", 2, "test_review"));

            // load data in sql
            var db = App.conn;
            using (var statement = db.Prepare("SELECT * FROM movieLists"))
            {
                while(SQLiteResult.ROW == statement.Step())
                {
                    int id = Convert.ToInt32(statement[0]);
                    string SideTitle = (string)statement[1];
                    string SideDescription = (string)statement[2];
                    double SideRank = (double)statement[3];
                    string SideReview = (string)statement[4];
                    this.allItems.Add(new Models.MovieItem(id, SideTitle, SideDescription, SideRank, SideReview));
                }
            }

        }

        //添加新电影项
        public void AddMovieItem(string title, string description, double rank, string review)
        {
            this.allItems.Add(new Models.MovieItem(title, description, rank, review));
        }

        //删除数据信息
        public void RemoveItem()
        {
            this.allItems.Remove(selectitem);
            this.selectitem = null;
        }

        //更新数据信息
        public void UpdateMovieItem(string title, string description, double rank, string review)
        {
            selectitem.title = title;
            selectitem.description = description;
            selectitem.rank = rank;
            selectitem.review = review;
            this.selectitem = null;
        }
    }
}
